using UnityEngine;
using UnityEngine.SceneManagement;

public class MinigameExit : MonoBehaviour {
    public void WinAndExit() {
        var gs = GameState.I;
        if (gs != null) {
            gs.MarkAreaComplete(gs.currentPlanetId, gs.currentAreaId);
        }
        ReturnToStarMap();
    }

    public void LoseAndRetry() {
        SceneLoader.I.LoadSceneWithFade(SceneManager.GetActiveScene().name);
    }

    public void ReturnToStarMap() {
        SceneLoader.I.LoadSceneWithFade("StarMap");
    }

    void Start() {
        if (SceneLoader.I) StartCoroutine(SceneLoader.I.FadeOutOnSceneStart());
    }
}
