using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {
    public static SceneLoader I;
    [SerializeField] CanvasGroup overlayCg;
    [SerializeField] float fadeDur = 0.2f;

    void Awake() { I = this; }

    public void LoadSceneWithFade(string sceneName) {
        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName) {
        overlayCg.gameObject.SetActive(true);
        yield return Fade(overlayCg, 0f, 1f, fadeDur);
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!op.isDone) yield return null;
    }

    public IEnumerator FadeOutOnSceneStart() {
        overlayCg.gameObject.SetActive(true);
        overlayCg.alpha = 1f;
        yield return Fade(overlayCg, 1f, 0f, fadeDur);
        overlayCg.gameObject.SetActive(false);
    }

    static IEnumerator Fade(CanvasGroup cg, float a, float b, float d) {
        float t = 0f;
        cg.alpha = a;
        while (t < 1f) { t += Time.deltaTime / d; cg.alpha = Mathf.SmoothStep(a,b,t); yield return null; }
        cg.alpha = b;
    }
}
