using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu I { get; private set; }

    [Header("Wiring")]
    [SerializeField] CanvasGroup menuGroup;
    [SerializeField] Button resumeButton;
    [SerializeField] Button exitButton;
    [SerializeField] Button quitButton;

    bool isOpen;
    float previousTimeScale = 1f;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        resumeButton.onClick.AddListener(Resume);
        exitButton.onClick.AddListener(ReturnToStarmap);
        quitButton.onClick.AddListener(QuitGame);

        HideImmediate();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOpen) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        if (isOpen) return;
        isOpen = true;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        menuGroup.alpha = 1f;
        menuGroup.interactable = true;
        menuGroup.blocksRaycasts = true;
    }

    public void Resume()
    {
        if (!isOpen) return;

        Time.timeScale = previousTimeScale;
        ForceHideMenu();
    }

    void HideImmediate()
    {
        isOpen = false;
        menuGroup.alpha = 0f;
        menuGroup.interactable = false;
        menuGroup.blocksRaycasts = false;
    }

    public void ReturnToStarmap()
    {
        Time.timeScale = 1f;

        ForceHideMenu();

        SceneManager.LoadScene("StarMap");
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

        ForceHideMenu();

        Debug.Log("Quitting game...");
        Application.Quit();
    }

    void ForceHideMenu()
    {
        isOpen = false;

        if (menuGroup != null)
        {
            menuGroup.alpha = 0f;
            menuGroup.interactable = false;
            menuGroup.blocksRaycasts = false;
        }
    }
}

