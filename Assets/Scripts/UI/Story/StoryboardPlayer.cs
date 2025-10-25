using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StoryboardPlayer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] CanvasGroup cg;
    [SerializeField] Image dim;
    [SerializeField] Image slideImg;
    [SerializeField] RectTransform slideRect;
    [SerializeField] TMP_Text textBox;
    [SerializeField] Button nextBtn, backBtn, skipBtn;

    [Header("Audio")]
    [SerializeField] AudioSource voSource;
    [SerializeField] AudioSource musicToDuck;
    [SerializeField] float duckDb = -8f;

    [Header("Timings")]
    [SerializeField] float fadeSeconds = 0.3f;

    StoryboardAsset asset;
    System.Action onComplete;
    int lineIdx;
    Coroutine _lineRoutine;
    int idx;
    bool playing;
    float savedMusicVol = 1f;

    void Awake()
    {
        if (!slideRect) slideRect = slideImg.rectTransform;
        cg.alpha = 0; cg.interactable = false; cg.blocksRaycasts = false;

        if (nextBtn) nextBtn.onClick.AddListener(Next);
        if (backBtn) backBtn.onClick.AddListener(Back);
        if (skipBtn) skipBtn.onClick.AddListener(Skip);
    }

    public void Play(StoryboardAsset sb, System.Action completed = null, int startIndex = 0)
    {
        asset = sb; onComplete = completed; idx = Mathf.Clamp(startIndex, 0, sb.slides.Length - 1);

        if (musicToDuck) { savedMusicVol = musicToDuck.volume; musicToDuck.volume = savedMusicVol * Db(duckDb); }

        StopAllCoroutines();
        StartCoroutine(FadeCanvas(0, 1, fadeSeconds, true, () => {
            playing = true;
            ShowSlide(idx, true);
        }));
    }

    public void Skip() => End();
    public void Next() {
        if (!playing) return;
        var s = asset.slides[idx];
        if (s.lines != null && lineIdx < s.lines.Length - 1) {
            lineIdx++;
            textBox.text = s.lines[lineIdx];
            return;
        }
        if (++idx < asset.slides.Length) ShowSlide(idx);
        else End();
    }
    public void Back() {
        if (!playing) return;
        var s = asset.slides[idx];
        if (s.lines != null && lineIdx > 0) {
            lineIdx--;
            textBox.text = s.lines[lineIdx];
            return;
        }
        if (--idx >= 0) ShowSlide(idx);
        else idx = 0;
    }
    void End()
    {
        playing = false;
        if (voSource) voSource.Stop();
        StartCoroutine(FadeCanvas(1, 0, fadeSeconds, false, () => {
            if (musicToDuck) musicToDuck.volume = savedMusicVol;
            onComplete?.Invoke();
        }));
    }

    void ShowSlide(int i, bool instant = false)
    {
        var s = asset.slides[i];
        slideImg.sprite = s.image;
        lineIdx = 0;
        textBox.text = s.lines != null && s.lines.Length > 0 ? s.lines[lineIdx] : (s.text ?? "");

        if (voSource)
        {
            voSource.Stop();
            if (s.voiceOver) { voSource.clip = s.voiceOver; voSource.loop = false; voSource.Play(); }
        }

        float startScale = s.kenBurns ? Mathf.Max(0.001f, s.zoomStart) : 1f;
        slideRect.localScale = Vector3.one * startScale;
        slideRect.pivot = s.panStart01;

        slideRect.anchorMin = Vector2.zero;
        slideRect.anchorMax = Vector2.one;
        slideRect.anchoredPosition = Vector2.zero;
        slideRect.offsetMin = slideRect.offsetMax = Vector2.zero;

        StopAllCoroutines();
        StartCoroutine(PlaySlide(s, instant));

        if (_lineRoutine != null) StopCoroutine(_lineRoutine);
        if (s.lines != null && s.lines.Length > 0 && s.autoAdvanceLines)
            _lineRoutine = StartCoroutine(AutoPageLines(s));
    }

    IEnumerator PlaySlide(StoryboardAsset.Slide s, bool instant)
    {
        float t = 0f, dur = Mathf.Max(0.01f, s.holdSeconds);
        while (playing)
        {
            if (s.kenBurns)
            {
                float e = Mathf.Clamp01(t / dur);
                slideRect.localScale = Vector3.one * Mathf.Lerp(s.zoomStart, s.zoomEnd, e);
                slideRect.pivot = Vector2.Lerp(s.panStart01, s.panEnd01, e);
            }

            if (s.autoAdvance) { t += Time.deltaTime; if (t >= dur) { Next(); yield break; } }

            if (Input.GetKeyDown(KeyCode.Space)) { Next(); yield break; }
            if (Input.GetKeyDown(KeyCode.LeftArrow)) { Back(); yield break; }
            if (Input.GetKeyDown(KeyCode.Escape)) { Skip(); yield break; }

            yield return null;
        }
    }

    IEnumerator FadeCanvas(float a, float b, float sec, bool enable, System.Action then = null)
    {
        cg.interactable = enable; cg.blocksRaycasts = enable;
        float t = 0; cg.alpha = a;
        while (t < 1f) { t += Time.deltaTime / sec; cg.alpha = Mathf.SmoothStep(a, b, t); yield return null; }
        cg.alpha = b; then?.Invoke();
    }

    IEnumerator AutoPageLines(StoryboardAsset.Slide s) {
        while (playing && s.lines != null && lineIdx < s.lines.Length - 1) {
            yield return new WaitForSeconds(Mathf.Max(0.05f, s.holdSecondsPerLine));
            if (!playing || asset.slides[idx] != s) yield break;
            lineIdx++;
            textBox.text = s.lines[lineIdx];
        }
    }

    float Db(float db) => Mathf.Pow(10f, db / 20f);
}
