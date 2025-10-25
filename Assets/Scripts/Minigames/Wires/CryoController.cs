using UnityEngine;
using UnityEngine.UI;


public class CryoController : MonoBehaviour
{
    [SerializeField] private Image Background;
    [SerializeField] private Sprite CafeteriaImage;
    [SerializeField] private Sprite WorkshopImage;

    [SerializeField] private GameObject partButton_A;
    [SerializeField] private GameObject partButton_B;
    [SerializeField] private GameObject partButton_C;

    [SerializeField] private string repairMinigameSceneName = "Mini_ShipRepair";

    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private DialogueSequence afterCollectDialogue;

    private bool inWorkshop = false;
    private bool gotA = false;
    private bool gotB = false;
    private bool gotC = false;
    private void EnterWorkshopState()
    {
        inWorkshop = true;

        if (Background && WorkshopImage)
            Background.sprite = WorkshopImage;

        if (partButton_A) partButton_A.SetActive(true);
        if (partButton_B) partButton_B.SetActive(true);
        if (partButton_C) partButton_C.SetActive(true);
    }

    public void EnterWorkshop()
    {
        if (inWorkshop) return;

        EnterWorkshopState();
    }

    public void CollectPartA()
    {
        if (!inWorkshop) return;
        if (gotA) return;
        gotA = true;

        if (partButton_A) partButton_A.SetActive(false);

        CheckAllParts();
    }

    public void CollectPartB()
    {
        if (!inWorkshop) return;
        if (gotB) return;
        gotB = true;

        if (partButton_B) partButton_B.SetActive(false);

        CheckAllParts();
    }

    public void CollectPartC()
    {
        if (!inWorkshop) return;
        if (gotC) return;
        gotC = true;

        if (partButton_C) partButton_C.SetActive(false);

        CheckAllParts();
    }

    private void CheckAllParts()
    {
        if (gotA && gotB && gotC)
        {
            Debug.Log("Cryovale: all ship parts collected.");

            if (dialogueUI && afterCollectDialogue)
            {
                dialogueUI.Play(afterCollectDialogue, LoadMinigame);
            }
            else
            {
                LoadMinigame();
            }
        }
    }

    private void LoadMinigame()
    {
        if (!string.IsNullOrEmpty(repairMinigameSceneName) && SceneLoader.I)
        {
            if (GameState.I != null)
            {
                GameState.I.currentPlanetId = "Cryovale";
                GameState.I.currentAreaId   = "ShipRepair";
                GameState.I.reopenPlanetViewAfterReturn = true;
            }

            SceneLoader.I.LoadSceneWithFade(repairMinigameSceneName);
        }
        else
        {
            Debug.LogWarning("CryovalePlanetController: repairMinigameSceneName not set or SceneLoader.I missing.");
        }
    }
}
