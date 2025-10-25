using UnityEngine;
using UnityEngine.SceneManagement;

public class TaikoRouter : MonoBehaviour {
    public static TaikoRouter I { get; private set; }

    public string returnScene;            // where to go back
    public string taikoScene = "TaikoScene";
    public TaikoDifficulty difficulty;
    public bool? lastResult;              // null = none, true = win, false = lose

    void Awake() {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);
    }

    public static void Ensure() {
        if (I == null) new GameObject("~TaikoRouter").AddComponent<TaikoRouter>();
    }

    public static void Launch(TaikoDifficulty diff, string returnToScene) {
        Ensure();
        I.difficulty = diff;
        I.returnScene = returnToScene;
        I.lastResult = null;
        UnityEngine.SceneManagement.SceneManager.LoadScene(I.taikoScene);
    }

    public static void CompleteAndReturn(bool win) {
        if (I == null) return;
        I.lastResult = win;
        if (!string.IsNullOrEmpty(I.returnScene))
            UnityEngine.SceneManagement.SceneManager.LoadScene(I.returnScene);
    }

    // One-time read & clear
    public static bool TryPopResult(out bool win) {
        win = false;
        if (I == null || I.lastResult == null) return false;
        win = I.lastResult.Value;
        I.lastResult = null;
        return true;
    }
}
