using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class HoverTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public TMP_Text tip;

    public void OnPointerEnter(PointerEventData e) {
        if (tip) tip.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData e) {
        if (tip) tip.gameObject.SetActive(false);
    }
}
