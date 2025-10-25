using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public bool IsHolding { get; private set; }
    public Action<bool> onHoldChanged;

    public void OnPointerDown(PointerEventData eventData)
    {
        IsHolding = true;
        onHoldChanged?.Invoke(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsHolding = false;
        onHoldChanged?.Invoke(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsHolding)
        {
            IsHolding = false;
            onHoldChanged?.Invoke(false);
        }
    }
}
