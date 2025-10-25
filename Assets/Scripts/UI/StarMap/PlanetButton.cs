using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlanetButton : MonoBehaviour {
    [Header("Refs")]
    public Image iconImg;
    public Image lockOverlay;
    public TMP_Text nameText;
    public Button button;

    [Header("Optional (auto if missing)")]
    public CanvasGroup lockGroup;

    [HideInInspector] public PlanetData data;

    void Awake() {
        EnsureLockGroup();
    }

    void OnValidate() {
        EnsureLockGroup();
    }

    void EnsureLockGroup() {
        if (!lockOverlay) return;
        if (!lockGroup) lockGroup = lockOverlay.GetComponent<CanvasGroup>();
        if (!lockGroup) lockGroup = lockOverlay.gameObject.AddComponent<CanvasGroup>();
        lockGroup.interactable = false;
        lockGroup.blocksRaycasts = true;
    }

    public void Bind(PlanetData d, bool unlocked) {
        data = d;
        if (iconImg) iconImg.sprite = d.icon;
        if (nameText) nameText.text = d.displayName;

        if (lockGroup) {
            lockGroup.alpha = unlocked ? 0f : 1f;
            lockGroup.blocksRaycasts = !unlocked;
            if (lockOverlay) lockOverlay.enabled = !unlocked;
        } else if (lockOverlay) {
            lockOverlay.gameObject.SetActive(!unlocked);
        }

        if (button) button.interactable = unlocked;
    }
}
