using UnityEngine;

[CreateAssetMenu(menuName="Game/ConvinceConfig")]
public class ConvinceConfig : ScriptableObject {
  [Header("Conversation thresholds")]
  public int warmTrust = 50, warmConviction = 50;
  public int pendingTrust = 65, pendingConviction = 70;
  public int convincedTrust = 75, convincedConviction = 80;
  public int pendingTurnsNeeded = 2;

  [Header("Clue flow (no fail)")]
  public int maxTurnsBeforeCluePrompt = 6;
  public int minProgressDelta = 12;

  [Header("Rule-based blending")]
  public int posKeywordBoost = 5;
  public int planBoost = 8;
  public int discoveredKeywordBoost = 8;

  [Range(0f,1f)] public float judgeWeight = 0.4f;
}
