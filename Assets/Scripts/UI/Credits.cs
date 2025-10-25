using UnityEngine;

public class Credits : MonoBehaviour
{
    public void Quit()
    {
        Application.Quit();
    }

    public void RestartGame()
    {
        GameState.I.ResetGameState();
        SceneLoader.I.LoadSceneWithFade("StarMap");
    }
}
