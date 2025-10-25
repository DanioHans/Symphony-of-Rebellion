using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MinigameController : MonoBehaviour {
    public DialogueUI dialogueUI;
    public DialogueSequence introDialogue;
    public DialogueSequence winDialogue;
    public DialogueSequence loseDialogue;
    public GameObject gameplayRoot;

    public bool playing;

    void Start() {
        if (SceneLoader.I) StartCoroutine(SceneLoader.I.FadeOutOnSceneStart());
        if (dialogueUI) dialogueUI.Play(introDialogue, StartGameplay);
        else StartGameplay();
    }

    void StartGameplay() {
        playing = true;
    }

    public void OnWin() {
        if (!playing) return;
        playing = false;

        ShowEndDialogue(winDialogue);
    }

    public void OnLose() {
        if (!playing) return;
        playing = false;

        ShowEndDialogue(loseDialogue);
    }

    void ShowEndDialogue(DialogueSequence seq) {
        dialogueUI.onCommand = HandleDialogueCommand;
        dialogueUI.Play(seq, () => HandleDialogueCommand("exit"));
    }

    void HandleDialogueCommand(string cmd) {
        switch ((cmd ?? "").ToLowerInvariant()) {
            case "retry":
                FindObjectOfType<MinigameExit>()?.LoseAndRetry();
                break;
            case "win":
                FindObjectOfType<MinigameExit>()?.WinAndExit();
                break;
            case "easy":
                TaikoLaunch.Launch(TaikoDifficulty.Easy);
                break;
            case "medium":
                TaikoLaunch.Launch(TaikoDifficulty.Medium);
                break;
            case "hard":
                TaikoLaunch.Launch(TaikoDifficulty.Hard);
                break;
            case "exit":
                MinigameUtils.FinishMinigame(true);
                break;
            default:
                FindObjectOfType<MinigameExit>()?.ReturnToStarMap();
                break;
        }
    }
}

public static class MinigameUtils
{
    public static void FinishMinigame(bool success)
    {
        var gs = GameState.I;
        string planet = gs.currentPlanetId;
        string area   = gs.currentAreaId;

        if (success && !string.IsNullOrEmpty(planet) && !string.IsNullOrEmpty(area))
            gs.MarkAreaComplete(planet, area);

        string storyboardId = planet + "_after";
        Debug.Log("storyboardId=" + storyboardId);
        gs.SetPendingStoryboard(storyboardId);
        
        gs.reopenPlanetViewAfterReturn = true;

        if (SceneLoader.I != null)
            SceneLoader.I.LoadSceneWithFade("StarMap");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("StarMap");
    }
}

