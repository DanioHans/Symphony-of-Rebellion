using UnityEngine;
using UnityEngine.SceneManagement;

public enum TaikoDifficulty { Easy, Medium, Hard }

public class TaikoLaunch : MonoBehaviour
{
    public static TaikoLaunch I { get; private set; }
    public TaikoDifficulty difficulty;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);
    }

    public static void Ensure() {
        if (I == null) new GameObject("~TaikoLaunch").AddComponent<TaikoLaunch>();
    }

    public static void Launch(TaikoDifficulty diff) {
        Ensure();
        I.difficulty = diff;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Mini_Taiko");
    }
}
