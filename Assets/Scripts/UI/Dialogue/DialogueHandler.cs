using UnityEngine;
using UnityEngine.UI;

public class DialogueHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private DialogueSequence dialogueSequence;
    [SerializeField] private DialogueSequence postDialogue;

    public void PlayDialogue()
    {
        if (dialogueUI == null)
        {
            Debug.LogWarning($"CityDialogueHandler on {name} has no DialogueUI assigned!");
            return;
        }

        // Call your DialogueUI’s play function
        dialogueUI.gameObject.SetActive(true);
        dialogueUI.onCommand = Handler;
        dialogueUI.Play(dialogueSequence, Finish);
    }

    void Handler(string cmd)
    {
        switch ((cmd ?? "").ToLowerInvariant())
        {
            case "workshop":
                var cryovale = FindObjectOfType<CryoController>();
                if (cryovale != null)
                    cryovale.EnterWorkshop();
                    Finish();
                break;
            default:
                Debug.Log($"Unhandled dialogue command: {cmd}");
                break;
        }
    }

    void Finish()
    {
        dialogueSequence = postDialogue;
        dialogueUI.gameObject.SetActive(false);
    }
}
