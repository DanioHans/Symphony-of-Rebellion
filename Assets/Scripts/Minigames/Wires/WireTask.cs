using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class WireTask : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private int totalPairs = 3;

    [Header("Visual Lines")]
    [SerializeField] private RectTransform lineParent;
    [SerializeField] private Image lineTemplate;

    [HideInInspector] public Action onTaskComplete;

    private WireNode pendingLeft;
    private int pairsSolved = 0;

    void Start()
    {
        UpdateStatus("Connect the wires.");
    }

    public void SelectNode(WireNode node)
    {
        if (node.IsLocked) return;

        if (node.isLeft)
        {
            pendingLeft = node;
            UpdateStatus("Select matching wire on the right...");
        }
        else
        {
            if (pendingLeft == null)
            {
                UpdateStatus("Pick a left wire first.");
                return;
            }

            if (pendingLeft.colorId == node.colorId && !node.IsLocked)
            {
                pendingLeft.Lock();
                node.Lock();

                CreateConnectionLine(pendingLeft, node);

                pairsSolved++;
                UpdateStatus("Connected!");
                pendingLeft = null;

                if (pairsSolved >= totalPairs)
                    CompleteTask();
            }
            else
            {
                UpdateStatus("Wrong match. Try again.");
            }
        }
    }

    void CompleteTask()
    {
        UpdateStatus("Wires stable.");
        onTaskComplete?.Invoke();
    }

    void UpdateStatus(string msg)
    {
        if (statusText) statusText.text = msg;
    }

    void CreateConnectionLine(WireNode left, WireNode right)
    {
        if (!lineParent || !lineTemplate) return;

        Image lineImg = Instantiate(lineTemplate, lineParent);
        lineImg.gameObject.SetActive(true);

        var srcImg = left.GetButtonImage();
        if (srcImg) lineImg.color = srcImg.color;

        PositionLineBetween(
            lineImg.rectTransform,
            (RectTransform)left.transform,
            (RectTransform)right.transform
        );
    }

    void PositionLineBetween(RectTransform lineRect, RectTransform a, RectTransform b)
    {
        if (!lineRect || !a || !b) return;

        Vector2 aLocal, bLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            lineParent,
            RectTransformUtility.WorldToScreenPoint(null, a.position),
            null,
            out aLocal
        );
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            lineParent,
            RectTransformUtility.WorldToScreenPoint(null, b.position),
            null,
            out bLocal
        );

        Vector2 dir = bLocal - aLocal;
        float dist = dir.magnitude;
        float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        lineRect.anchorMin = new Vector2(0f, 0.5f);
        lineRect.anchorMax = new Vector2(0f, 0.5f);
        lineRect.pivot     = new Vector2(0f, 0.5f);

        lineRect.anchoredPosition = aLocal;
        lineRect.sizeDelta = new Vector2(dist, 6f);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
    }
}
