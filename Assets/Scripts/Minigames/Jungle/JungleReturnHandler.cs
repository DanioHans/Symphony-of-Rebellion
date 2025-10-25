using UnityEngine;

public class JungleReturnHandler : MonoBehaviour {
    public DialogueUI dialogueUI;
    public DialogueSequence drummerPostWin;
    public DialogueSequence drummerPostLose; // optional

    void Start() {
        if (TaikoRouter.TryPopResult(out bool win)) {
            if (dialogueUI) {
                var seq = win ? drummerPostWin : (drummerPostLose ? drummerPostLose : drummerPostWin);
                dialogueUI.Play(seq, () => { /* no-op after post dialogue */ });
            }
        }
    }
}
