using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class JungleHuntController : MonoBehaviour {
    [Header("Data")]
    public JungleNode[] nodes;
    public int startIndex = 0;

    [Header("UI")]
    public Image bg;
    public Image hintOverlay;
    public RectTransform bgRect;
    public RectTransform zonesRoot;
    public TMP_Text promptText;
    public Button backButton;
    public Image soundCursorHint;
    public MinigameController MinigameController;

    [Header("Audio")]
    public AudioSource sfx;

    public int finalIndex = -1;
    bool finishedPath;

    // runtime
    int curIndex = -1;
    readonly Stack<int> history = new();
    readonly List<Button> zonePool = new();

    [Header("Sound Hotspot Tuning")]
    [Tooltip("How far (0..1 of BG size) the hotspot can be heard. Smaller = steeper falloff.")]
    public float hotspotAudibleRadius01 = 0.30f;

    [Tooltip("Idle background volume when far away.")]
    [Range(0f, 1f)] public float hotspotMinVolume = 0.03f;

    [Tooltip("Peak volume right on top of the hotspot.")]
    [Range(0f, 1f)] public float hotspotMaxVolume = 0.80f;

    [Tooltip(">1 = sharper rise near center; 1 = linear; <1 = softer.")]
    [Range(0.25f, 8f)] public float hotspotFalloffPower = 3f;

    public AnimationCurve hotspotFalloffCurve = null;


    void Awake() {
        backButton.onClick.AddListener(GoBack);
        GoTo(startIndex, pushHistory:false);
        zonesRoot.SetAsLastSibling();
    }

    void Update() {
        var n = nodes[curIndex];
        if (n.hasSoundHotspot)
            {
                // Mouse in BG local space
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(bgRect, Input.mousePosition, null, out local);
                Rect r = bgRect.rect;

                // Normalize 0..1 (BG space)
                float x01 = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
                float y01 = Mathf.InverseLerp(r.yMin, r.yMax, local.y);

                // Distance in 0..1 BG units
                float dx = x01 - n.hotspotX01, dy = y01 - n.hotspotY01;
                float dist01 = Mathf.Sqrt(dx * dx + dy * dy);

                // Map distance to [0..1] proximity within our audible radius
                float d = Mathf.Clamp01(dist01 / Mathf.Max(0.0001f, hotspotAudibleRadius01));
                float t = 1f - d;

                // Shape the rise: use curve if provided, else power curve
                float shaped = (hotspotFalloffCurve != null && hotspotFalloffCurve.length > 0)
                    ? Mathf.Clamp01(hotspotFalloffCurve.Evaluate(t))
                    : Mathf.Pow(Mathf.Clamp01(t), hotspotFalloffPower);

                // Final volume
                float vol = Mathf.Lerp(hotspotMinVolume, hotspotMaxVolume, shaped);

                if (sfx && sfx.clip == n.hotspotLoop)
                    sfx.volume = vol;

                // Cursor hint opacity: stronger when close
                if (soundCursorHint)
                {
                    soundCursorHint.gameObject.SetActive(true);
                    soundCursorHint.rectTransform.position = Input.mousePosition;
                    var c = soundCursorHint.color;
                    c.a = Mathf.Lerp(0.01f, 0.90f, shaped);
                    soundCursorHint.color = c;
                }

                // Click near the hotspot (pixel radius as before)
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 hotspotPx = new(
                        Mathf.Lerp(r.xMin, r.xMax, n.hotspotX01),
                        Mathf.Lerp(r.yMin, r.yMax, n.hotspotY01));

                    float distPx = Vector2.Distance(local, hotspotPx);
                    if (distPx <= n.triggerRadiusPx && n.hotspotLeadsToIndex >= 0)
                        GoTo(n.hotspotLeadsToIndex);
                }
            }
            else if (soundCursorHint)
            {
                soundCursorHint.gameObject.SetActive(false);
            }
    }


    void GoBack() {
        if (history.Count == 0) return;
        int prev = history.Pop();
        GoTo(prev, pushHistory:false);
    }

    void GoTo(int index, bool pushHistory = true) {
        if (index < 0 || index >= nodes.Length) return;
        if (curIndex >= 0 && pushHistory) history.Push(curIndex);
        curIndex = index;

        var n = nodes[curIndex];

        bg.sprite = n.background;
        if (hintOverlay) { hintOverlay.enabled = n.hintOverlay; hintOverlay.sprite = n.hintOverlay; }

        if (sfx) {
            if (n.hasSoundHotspot && n.hotspotLoop) {
                sfx.clip = n.hotspotLoop; sfx.loop = true; sfx.volume = 0f; sfx.Play();
            } else {
                if (sfx.isPlaying) sfx.Stop();
                if (n.isDeadEnd && n.deadEndSfx) { sfx.PlayOneShot(n.deadEndSfx, 0.9f); }
            }
        }

        backButton.gameObject.SetActive(history.Count > 0);

        BuildZones(n);

        if (!finishedPath && (curIndex == finalIndex)) {
            MinigameController.OnWin();
        }
    }


    void BuildZones(JungleNode n) {
        // clear old
        foreach (var b in zonePool) Destroy(b.gameObject);
        zonePool.Clear();

        if (n.isDeadEnd) {
            if (promptText) promptText.text = "<i>Something growls in the dark… better head back.</i>";
            return;
        }

        if (n.hasSoundHotspot) {
            if (promptText) promptText.text = "<i>Listen… the drums are close. Click when the beat is loudest.</i>";
            return;
        }

        // build invisible rect zones from exits, or fallback to L/R thirds, etc.
        if (n.exits != null && n.exits.Length > 0) {
            // Prompt line: "Follow river | Into the jungle | Climb ridge"
            if (promptText) {
                System.Text.StringBuilder sb = new();
                for (int i = 0; i < n.exits.Length; i++) {
                    if (i > 0) sb.Append("  |  ");
                    sb.Append(n.exits[i].label);
                }
                promptText.text = sb.ToString();
            }

            foreach (var ex in n.exits) {
                var zone = CreateZoneButton();
                var rt = zone.GetComponent<RectTransform>();

                if (ex.hasRect)
                {
                    Rect r01 = To01(ex.rect01, bgRect);

                    rt.anchorMin = new Vector2(r01.xMin, r01.yMin);
                    rt.anchorMax = new Vector2(r01.xMax, r01.yMax);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
                else
                {
                    int count = n.exits.Length;
                    int idx = Mathf.Max(0, zonePool.Count - 1);
                    rt.anchorMin = new Vector2((float)idx / count, 0f);
                    rt.anchorMax = new Vector2((float)(idx + 1) / count, 1f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }


                var tip = zone.GetComponentInChildren<TMP_Text>(true);
                if (tip) {
                    tip.text = ex.label;
                    tip.gameObject.SetActive(false);
                }

                var hover = zone.gameObject.AddComponent<HoverTip>();
                hover.tip = tip;

                int target = ex.targetIndex; // capture
                zone.onClick.AddListener(() => GoTo(target));
            }
        } else {
            if (promptText) promptText.text = "";
        }
    }


    Button CreateZoneButton() {
        var go = new GameObject("Zone", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(zonesRoot, false);

        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.001f);
        img.raycastTarget = true;

        var tgo = new GameObject("Tip", typeof(RectTransform));
        tgo.transform.SetParent(go.transform, false);
        var rt = (RectTransform)tgo.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(260, 40);

        var tmp = tgo.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 22f;
        tmp.color = new Color(1f, 1f, 1f, 0.9f);
        tmp.enableWordWrapping = false;
        tgo.SetActive(false);

        var btn = go.GetComponent<Button>();
        zonePool.Add(btn);
        return btn;
    }

    Rect To01(Rect rMaybe, RectTransform bgRef)
    {
        Rect r = rMaybe;
        var bg = bgRef.rect;
        bool looksLikePixels =
            r.x > 1f || r.y > 1f || r.width > 1f || r.height > 1f;

        if (looksLikePixels)
        {
            r = new Rect(
                rMaybe.x / bg.width,
                rMaybe.y / bg.height,
                rMaybe.width / bg.width,
                rMaybe.height / bg.height
            );
        }

        float xMin = Mathf.Clamp01(r.x);
        float yMin = Mathf.Clamp01(r.y);
        float xMax = Mathf.Clamp01(r.x + r.width);
        float yMax = Mathf.Clamp01(r.y + r.height);

        if (xMax < xMin) (xMin, xMax) = (xMax, xMin);
        if (yMax < yMin) (yMin, yMax) = (yMax, yMin);

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }


}
