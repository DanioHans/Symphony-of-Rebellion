using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StarMapController : MonoBehaviour {
    [Header("Star Map")]
    public RectTransform buttonsRoot;
    public PlanetData[] planets;

    [Header("Planet View")]
    [SerializeField] RectTransform planetViewPanel;
    [SerializeField] CanvasGroup planetViewCg;
    [SerializeField] Image planetViewBG;
    [SerializeField] RectTransform hotspotsRoot;
    [SerializeField] Button backButton;

    [Header("Prefabs")]
    [SerializeField] PlanetButton planetButtonPrefab;
    [SerializeField] Image zoomProxyPrefab;
    [SerializeField] HotspotButton hotspotButtonPrefab;

    [SerializeField] private StoryboardRegistry storyboardRegistry;

    [SerializeField] private AudioSource backgroundMusic;
    [SerializeField] private AudioClip starMapMusicClip;

    void Start() {
        if (GameState.I == null) new GameObject("GameState").AddComponent<GameState>();
        BuildButtons();

        if (SceneLoader.I) StartCoroutine(SceneLoader.I.FadeOutOnSceneStart());

        if (GameState.I.reopenPlanetViewAfterReturn && !string.IsNullOrEmpty(GameState.I.currentPlanetId)) {
            var pd = System.Array.Find(planets, p => p.planetId == GameState.I.currentPlanetId);
            if (pd != null) OpenPlanetInstant(pd);
            GameState.I.reopenPlanetViewAfterReturn = false;
        }

        if (!GameState.I.HasSeenStoryboard("intro")) { GameState.I.SetPendingStoryboard("intro"); GameState.I.MarkStoryboardSeen("intro"); }
        if (GameState.I.HasFlag("melodia_completed")) { 
            backgroundMusic.clip = starMapMusicClip;
            backgroundMusic.Play();
            GameState.I.SetPendingStoryboard("end");
            GameState.I.MarkStoryboardSeen("end");
        }

        var sbId = GameState.I?.ConsumePendingStoryboard();
        if (!string.IsNullOrEmpty(sbId) && storyboardRegistry && StoryboardService.I) {
            if (storyboardRegistry.TryGet(sbId, out var entry) && entry.asset) {
                SetStarMapInteractable(false);

                Debug.Log($"StarMapController: playing pending storyboard {sbId}");
                StoryboardService.I.Play(entry.asset, () => {
                    if (!string.IsNullOrEmpty(entry.postEffectId)) {
                        switch (entry.postEffectId) {
                            case "end":
                                Credits();
                                break;
                        }
                    }
                    SetStarMapInteractable(true);
                });
            }
        }
    }

    private void SetStarMapInteractable(bool enabled) {
        if (buttonsRoot) {
            var cg = buttonsRoot.GetComponent<CanvasGroup>();
            if (!cg) cg = buttonsRoot.gameObject.AddComponent<CanvasGroup>();
            cg.interactable = enabled;
            cg.blocksRaycasts = enabled;
            cg.alpha = enabled ? 1f : 0.8f;
        }
    }

    void BuildButtons() {
        foreach (Transform c in buttonsRoot) Destroy(c.gameObject);
        foreach (var p in planets) {
            var pb = Instantiate(planetButtonPrefab, buttonsRoot);
            var rt = (RectTransform)pb.transform;
            var icon = pb.transform.GetChild(0);
            rt.anchorMin = rt.anchorMax = p.anchor01;
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one * p.scale;
            icon.localScale = Vector3.one * p.iconScale;

            bool unlocked = EvaluateUnlock(p);
            pb.Bind(p, unlocked);
            if (unlocked) pb.button.onClick.AddListener(() => OnPlanetClicked(pb, p));
        }
    }

    void OpenPlanetInstant(PlanetData data) {
        planetViewPanel.gameObject.SetActive(true);
        planetViewCg.alpha = 1f;
        planetViewCg.interactable = true;
        planetViewCg.blocksRaycasts = true;

        planetViewBG.sprite = data.planetViewBG ? data.planetViewBG : data.icon;
        RebuildHotspots(data);
    }

    bool EvaluateUnlock(PlanetData p) {
        switch (p.lockCondition) {
            case PlanetLockCondition.AlwaysUnlocked: return true;
            case PlanetLockCondition.FlagRequired:   return GameState.I.HasFlag(p.requiredFlag);
            case PlanetLockCondition.StoryStepReached: return GameState.I.storyStep >= p.requiredStoryStep;
            default: return false;
        }
    }

    void OnPlanetClicked(PlanetButton pb, PlanetData data) {
        StartCoroutine(ZoomAndOpen(pb, data));
    }

    IEnumerator ZoomAndOpen(PlanetButton pb, PlanetData data) {
        var proxy = Instantiate(zoomProxyPrefab, planetViewPanel.parent);
        proxy.raycastTarget = false;
        proxy.sprite = pb.iconImg.sprite;

        CopyRect((RectTransform)pb.iconImg.transform, (RectTransform)proxy.transform);

        Vector2 targetPos = Vector2.zero;
        Vector2 targetSize = new Vector2(1080, 1080);
        float t = 0f, dur = 0.35f;
        var rt = (RectTransform)proxy.transform;
        Vector2 startPos = rt.anchoredPosition;
        Vector2 startSize = rt.sizeDelta;

        while (t < 1f) {
            t += Time.deltaTime / dur;
            float e = EaseInOut(t);
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, e);
            rt.sizeDelta = Vector2.Lerp(startSize, targetSize, e);
            yield return null;
        }

        planetViewPanel.gameObject.SetActive(true);
        planetViewCg.alpha = 0f;
        planetViewCg.interactable = false;
        planetViewCg.blocksRaycasts = false;

        planetViewBG.sprite = data.planetViewBG ? data.planetViewBG : data.icon;

        foreach (Transform c in hotspotsRoot) Destroy(c.gameObject);
        for (int i = 0; i < data.areas.Length; i++) {
            var a = data.areas[i];
            var hs = Instantiate(hotspotButtonPrefab, hotspotsRoot);
            hs.iconImg.sprite = a.background ? a.background : data.icon;
            var hrt = (RectTransform)hs.transform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(a.x01, a.y01);
            hrt.anchoredPosition = Vector2.zero;
            hrt.sizeDelta = new Vector2(120,120);
            hs.button.onClick.AddListener(() => OpenArea(data, a));

        }

        Destroy(proxy.gameObject);
        yield return FadeCanvasGroup(planetViewCg, 0f, 1f, 0.01f, enableOnEnd:true);
    }

    void OpenArea(PlanetData data, AreaDef a) {
        if (string.IsNullOrEmpty(a.sceneToLoad)) {
        Debug.LogWarning($"Area {a.id} has no sceneToLoad");
        return;
        }
        GameState.I.currentPlanetId = data.planetId;
        GameState.I.currentAreaId = a.id;
        GameState.I.reopenPlanetViewAfterReturn = true;
        SceneLoader.I.LoadSceneWithFade(a.sceneToLoad);
        }

    public void ClosePlanetView() {
        planetViewPanel.gameObject.SetActive(false);
    }

    IEnumerator ClosePlanetCoroutine() {
        yield return FadeCanvasGroup(planetViewCg, 1f, 0f, 0.2f, enableOnEnd:false);
        planetViewPanel.gameObject.SetActive(false);
    }   

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float dur, bool enableOnEnd) {
        cg.alpha = from;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        float t = 0f;
        while (t < 1f) {
            t += Time.deltaTime / dur;
            cg.alpha = Mathf.SmoothStep(from, to, t);
            yield return null;
        }
        cg.alpha = to;
        cg.interactable = enableOnEnd;
        cg.blocksRaycasts = enableOnEnd;
    }

    static void CopyRect(RectTransform from, RectTransform to) {
        to.anchorMin = to.anchorMax = new Vector2(0.5f, 0.5f);
        Vector3[] w = new Vector3[4];
        from.GetWorldCorners(w);
        var parent = (RectTransform)to.parent;
        Vector2 bl, tr;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, RectTransformUtility.WorldToScreenPoint(null, w[0]), null, out bl);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, RectTransformUtility.WorldToScreenPoint(null, w[2]), null, out tr);
        to.anchoredPosition = (bl + tr) * 0.5f;
        to.sizeDelta = (tr - bl);
        to.localRotation = Quaternion.identity;
        to.localScale = Vector3.one;
    }
    static float EaseInOut(float t) => t <= 0 ? 0 : (t >= 1 ? 1 : t*t*(3-2*t));

    PlanetButton FindButtonFor(PlanetData pd) {
        foreach (Transform t in buttonsRoot) {
            var pb = t.GetComponent<PlanetButton>();
            if (pb && pb.data == pd) return pb;
        }
        return null;
    }

    void RebuildHotspots(PlanetData data) {
        foreach (Transform c in hotspotsRoot) Destroy(c.gameObject);
        for (int i = 0; i < data.areas.Length; i++) {
            var a = data.areas[i];
            var hs = Instantiate(hotspotButtonPrefab, hotspotsRoot);
            hs.iconImg.sprite = a.background ? a.background : data.icon;

            var rt = (RectTransform)hs.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(a.x01, a.y01);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(120,120);

            if (GameState.I.IsAreaComplete(data.planetId, a.id)) {
                hs.iconImg.color = new Color(1,1,1,0.5f);
            }

            hs.button.onClick.AddListener(() => OpenArea(data, a));
        }
    }

    void UnlockPlanetVisual(string planetId)
    {
        PlanetButton target = null;
        foreach (Transform t in buttonsRoot)
        {
            var pb = t.GetComponent<PlanetButton>();
            if (pb && pb.data && pb.data.planetId == planetId) { target = pb; break; }
        }
        if (!target)
        {
            Debug.LogWarning($"No PlanetButton for {planetId}");
            return;
        }

        if (target.button) target.button.interactable = true;

        StartCoroutine(FadeLockAndPulse(target));
    }

    IEnumerator FadeLockAndPulse(PlanetButton pb)
    {
        if (pb == null) yield break;

        var cg = pb.lockGroup;
        var img = pb.lockOverlay;

        if (cg != null)
        {
            float t = 0f, dur = 0.35f;
            float start = cg.alpha;
            float end   = 0f;

            cg.blocksRaycasts = true;

            while (t < 1f)
            {
                if (pb == null || cg == null) yield break;
                t += Time.deltaTime / Mathf.Max(0.01f, dur);
                cg.alpha = Mathf.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            if (cg != null) {
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
            }
            if (img != null) img.enabled = false;
        }
        else
        {
            var lockImg = pb.lockOverlay;
            if (lockImg && lockImg.gameObject.activeInHierarchy)
            {
                float t = 0f, dur = 0.35f; var c = lockImg.color;
                while (t < 1f)
                {
                    if (pb == null || lockImg == null) yield break;
                    t += Time.deltaTime / Mathf.Max(0.01f, dur);
                    c.a = 1f - t;
                    lockImg.color = c;
                    yield return null;
                }
                if (lockImg) lockImg.enabled = false;
            }
        }

        if (pb == null || pb.iconImg == null) yield break;
        var rt = pb.iconImg.rectTransform;
        if (rt == null) yield break;

        Vector3 baseScale = rt.localScale;
        float u = 0f, pulseDur = 0.25f;

        while (u < 1f)
        {
            if (pb == null || rt == null) yield break;
            u += Time.deltaTime / Mathf.Max(0.01f, pulseDur);
            float s = 1f + 0.1f * Mathf.Sin(u * Mathf.PI);
            rt.localScale = baseScale * s;
            yield return null;
        }

        if (rt != null) rt.localScale = baseScale;
    }

    void Credits() {
        SceneLoader.I.LoadSceneWithFade("CreditsScene");
    }



}
