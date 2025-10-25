using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour {
    [Header("Refs")]
    public Image portrait;
    public TMP_Text nameText, bodyText;
    public Button continueBtn;
    public Transform choicesRoot;
    public Button choiceButtonPrefab;
    public System.Action<string> onCommand;

    [Header("Settings")]
    [SerializeField] float charDelay = 0.02f;

    DialogueSequence seq;
    int idx;
    System.Action onFinished;

    public void Play(DialogueSequence s, System.Action finished) {
        onFinished = finished;
        if (s == null || s.lines == null || s.lines.Length == 0) {
            gameObject.SetActive(false);
            onFinished?.Invoke();
            return;
        }
        seq = s; idx = 0;
        gameObject.SetActive(true);
        ShowLine();
    }

    void ShowLine() {
        if (seq == null || idx < 0 || idx >= seq.lines.Length) { End(); return; }
        var L = seq.lines[idx];

        if (portrait) portrait.sprite = L.portrait;
        if (!L.portrait) portrait.color = new Color32(1,1,1,0);
        else portrait.color = new Color32(255,255,255,255);
        if (nameText) nameText.text = string.IsNullOrEmpty(L.speaker) ? "" : L.speaker;

        StopAllCoroutines();
        StartCoroutine(TypeText(L.text ?? ""));

        if (choicesRoot) foreach (Transform c in choicesRoot) Destroy(c.gameObject);

        bool hasChoices = L.choices != null && L.choices.Length > 0;

        if (continueBtn) {
            continueBtn.gameObject.SetActive(!hasChoices);
            if (!hasChoices) {
                continueBtn.onClick.RemoveAllListeners();
                continueBtn.onClick.AddListener(() => { idx++; ShowLine(); });
            }
        }

        if (hasChoices && choicesRoot && choiceButtonPrefab) {
            foreach (var ch in L.choices) {
                var b = Instantiate(choiceButtonPrefab, choicesRoot);
                var t = b.GetComponentInChildren<TMPro.TMP_Text>();
                if (t) t.text = ch.label;

                var choice = ch;

                b.onClick.AddListener(() => {
                    if (!string.IsNullOrEmpty(choice.command)) {
                        onCommand?.Invoke(choice.command);
                        return;
                    }

                    idx = (choice.gotoIndex < 0) ? idx + 1 : choice.gotoIndex;
                    ShowLine();
                });
            }
        }
    }

    IEnumerator TypeText(string s) {
        bodyText.text = "";
        for (int i = 0; i < s.Length; i++) {
            bodyText.text = s.Substring(0, i + 1);
            yield return new WaitForSeconds(charDelay);
        }
    }

    void End() {
        gameObject.SetActive(false);
        onFinished?.Invoke();
    }
}
