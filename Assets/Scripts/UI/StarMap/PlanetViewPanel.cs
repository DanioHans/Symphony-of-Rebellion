using UnityEngine;
using UnityEngine.UI;

public class PlanetViewPanel : MonoBehaviour {
    [SerializeField] CanvasGroup cg;
    [SerializeField] Image bg;
    [SerializeField] RectTransform hotspotsRoot;
    [SerializeField] Button backButton;

    System.Action _onClosed;
    PlanetData _data;

    void Awake() {
        if (backButton) backButton.onClick.AddListener(Close);
        gameObject.SetActive(false);
        cg.alpha = 0; cg.interactable = false; cg.blocksRaycasts = false;
    }

    public void Open(PlanetData data, System.Action onClosed = null) {
        gameObject.SetActive(true);
        _data = data; _onClosed = onClosed;
        bg.sprite = data.planetViewBG ? data.planetViewBG : data.icon;
        RebuildHotspots(data);

        gameObject.SetActive(true);
        StartCoroutine(Fade(0, 1, 0.2f, true));
    }

    public void Close() {
        StartCoroutine(Fade(1, 0, 0.15f, false, () => {
            gameObject.SetActive(false);
            _onClosed?.Invoke();
        }));
    }

    void RebuildHotspots(PlanetData data) {
        foreach (Transform c in hotspotsRoot) Destroy(c.gameObject);
        if (data.areas == null) return;
        foreach (var a in data.areas) {
            var hs = Instantiate(hotspotButtonPrefab, hotspotsRoot);
            var rt = (RectTransform)hs.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(a.x01, a.y01);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(120,120);
            hs.button.onClick.AddListener(() => {});
        }
    }

    System.Collections.IEnumerator Fade(float a, float b, float dur, bool enable, System.Action then = null) {
        cg.interactable = enable;
        cg.blocksRaycasts = enable;
        float t=0; cg.alpha = a;
        while (t<1f){ t+=Time.deltaTime/dur; cg.alpha=Mathf.SmoothStep(a,b,t); yield return null; }
        cg.alpha = b; then?.Invoke();
    }

    public HotspotButton hotspotButtonPrefab;
}
