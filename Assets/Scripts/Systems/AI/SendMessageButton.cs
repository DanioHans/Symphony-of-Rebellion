using TMPro;
using UnityEngine;

public class SendMessageButton : MonoBehaviour {
    [SerializeField] TMP_InputField input;
    [SerializeField] AIConvinceController controller;

    public void OnSend() {
        if (!controller || !input) return;
        controller.PlayerSays(input.text);
        input.text = "";
        input.ActivateInputField();
    }
}
