using UnityEngine;
using TMPro;
using System;

public class RefuelTask : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text statusText;

    [Tooltip("Full width bar (background).")]
    [SerializeField] private RectTransform barContainer;

    [Tooltip("Fill segment that grows left->right with pressure.")]
    [SerializeField] private RectTransform barFill;

    [Tooltip("Visual overlay that shows the allowed zone (the green rect).")]
    [SerializeField] private RectTransform zoneOverlay;

    [Header("Fill Dynamics")]
    [SerializeField] private float fillSpeed = 0.25f;
    [SerializeField] private float drainSpeed = 0.18f;

    [Tooltip("When holding, we also gently steer currentFill toward the safe zone center. 0 = off.")]
    [SerializeField] private float assistTowardsZone = 0.5f;

    [Header("Zone Movement")]
    [Tooltip("How wide the safe zone is, as fraction of bar width (0-1).")]
    [SerializeField] private float zoneWidth = 0.18f;

    [Tooltip("Speed that the zone center moves (0-1 per second). Higher = faster band.")]
    [SerializeField] private float zoneMoveSpeed = 0.35f;

    [Header("Stability Requirement")]
    [Tooltip("Seconds you must keep pressure in band WHILE holding.")]
    [SerializeField] private float requiredStableTime = 1.2f;

    [Tooltip("How fast stability decays when you're not qualifying.")]
    [SerializeField] private float stabilityDecayRate = 0.4f;

    [Header("Hold Control")]
    [SerializeField] private HoldButton holdButton;

    [HideInInspector] public Action onTaskComplete;

    private float currentFill = 0f; // 0..1
    private bool completed = false;

    // zone center in [0..1]
    private float zoneCenter01 = 0.5f;

    private float stableTimer = 0f;

    void Start()
    {
        if (holdButton) holdButton.onHoldChanged += OnHoldChanged;
        ResetTask();
    }

    public void BeginRefuel()
    {
        ResetTask();
        UpdateStatus("Track the flow. Keep holding in the green band.");
    }

    void ResetTask()
    {
        completed = false;
        stableTimer = 0f;
        currentFill = 0f;
        zoneCenter01 = 0.5f;
        UpdateBarVisual();
        UpdateZoneOverlayVisual();
        UpdateStatus("Refuel and stabilize pressure.");
    }

    void Update()
    {
        if (completed) return;

        bool holding = (holdButton && holdButton.IsHolding);

        if (holding)
        {
            currentFill += fillSpeed * Time.deltaTime;
        }
        else
        {
            currentFill -= drainSpeed * Time.deltaTime;
        }

        currentFill = Mathf.Clamp01(currentFill);

        zoneCenter01 += zoneMoveSpeed * Time.deltaTime;

        if (zoneCenter01 > 0.8f || zoneCenter01 < 0.2f)
        {
            zoneMoveSpeed *= -1f;
            zoneCenter01 = Mathf.Clamp(zoneCenter01, 0.2f, 0.8f);
        }

        float halfW = zoneWidth * 0.5f;
        float minTarget = Mathf.Clamp01(zoneCenter01 - halfW);
        float maxTarget = Mathf.Clamp01(zoneCenter01 + halfW);
        float zoneMid   = (minTarget + maxTarget) * 0.5f;

        if (holding && assistTowardsZone > 0f)
        {
            currentFill = Mathf.Lerp(
                currentFill,
                zoneMid,
                assistTowardsZone * Time.deltaTime
            );
        }

        currentFill = Mathf.Clamp01(currentFill);

        bool inBand = (currentFill >= minTarget && currentFill <= maxTarget);

        if (holding && inBand)
        {
            stableTimer += Time.deltaTime;
            UpdateStatus($"Stabilizing... {stableTimer:0.0}/{requiredStableTime:0.0}");

            if (stableTimer >= requiredStableTime)
            {
                CompleteTask();
            }
        }
        else
        {
            stableTimer = Mathf.Max(0f, stableTimer - stabilityDecayRate * Time.deltaTime);

            if (!completed)
            {
                UpdateStatus("Keep pressure inside the green band...");
            }
        }

        UpdateBarVisual();
        UpdateZoneOverlayVisual(minTarget, maxTarget);
    }

    void CompleteTask()
    {
        completed = true;
        UpdateStatus("Fuel flow nominal. We're good.");
        onTaskComplete?.Invoke();
    }

    void OnHoldChanged(bool isHolding)
    {
        if (completed) return;

        if (isHolding)
        {
            UpdateStatus("Pressurizing...");
        }
        else
        {
            UpdateStatus("Hold pressure inside the green band...");
        }
    }

    void UpdateBarVisual()
    {
        if (!barContainer || !barFill) return;

        float maxWidth = barContainer.rect.width;
        float newWidth = maxWidth * currentFill;

        barFill.anchorMin = new Vector2(0f, 0.5f);
        barFill.anchorMax = new Vector2(0f, 0.5f);
        barFill.pivot     = new Vector2(0f, 0.5f);

        barFill.anchoredPosition = new Vector2(0f, 0f);
        barFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
        barFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barContainer.rect.height);
    }

    void UpdateZoneOverlayVisual()
    {
        float halfW = zoneWidth * 0.5f;
        float minTarget = Mathf.Clamp01(zoneCenter01 - halfW);
        float maxTarget = Mathf.Clamp01(zoneCenter01 + halfW);
        UpdateZoneOverlayVisual(minTarget, maxTarget);
    }

    void UpdateZoneOverlayVisual(float minTarget, float maxTarget)
    {
        if (!barContainer || !zoneOverlay) return;

        float barW = barContainer.rect.width;
        float barH = barContainer.rect.height;

        float xMinPx = barW * minTarget;
        float xMaxPx = barW * maxTarget;
        float zoneWidthPx = Mathf.Max(2f, xMaxPx - xMinPx);

        zoneOverlay.anchorMin = new Vector2(0f, 0.5f);
        zoneOverlay.anchorMax = new Vector2(0f, 0.5f);
        zoneOverlay.pivot     = new Vector2(0f, 0.5f);

        zoneOverlay.anchoredPosition = new Vector2(xMinPx, 0f);
        zoneOverlay.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, zoneWidthPx);
        zoneOverlay.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barH);
    }

    void UpdateStatus(string s)
    {
        if (statusText) statusText.text = s;
    }
}
