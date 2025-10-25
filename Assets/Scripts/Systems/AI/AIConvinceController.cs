using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using LLMUnity;


public enum ConvinceState { Guarded, WarmingUp, PersuadedPending, Convinced }

[Serializable] public class JudgeResult {
  public int conviction, trust, suspicion;
  public float sentiment;
  public string[] keywordHits;
  public bool secretTriggered;
  public string reason, hint;
}

[Serializable] public class ConvinceMeters {
  public int trust, conviction, suspicion;
}

public class AIConvinceController : MonoBehaviour {
    [Header("LLMUnity")]
    [Tooltip("Reference to an LLMCharacter in the scene (child of the LLM component or on this GO).")]
    [SerializeField] LLMCharacter llmCharacter;
    [Tooltip("Optional: a separate character configured with the judge prompt template. If null, we use llmCharacter.Complete()")]
    [SerializeField] LLMCharacter judgeCharacter;

    [Header("Config & UI")]
    [SerializeField] ConvinceConfig cfg;
    [SerializeField] TMP_Text aiText;
    [SerializeField] TMP_Text hintText;
    [SerializeField] int transcriptWindow = 6;
    [SerializeField] bool debugLogs = true;
    [SerializeField] TMP_Text debugHud;


    [Header("Events")]
    public UnityEvent onConvinced;
    public UnityEvent onNeedClue;

    [Header("Debug/State")]
    [SerializeField] ConvinceMeters meters;
    [SerializeField] ConvinceState state = ConvinceState.Guarded;
    [SerializeField] int consecutivePending, consecutiveConvinced, turnsTaken, lastProgressScore;

    readonly List<string> transcript = new();
    readonly System.Random rng = new(42);

    readonly string[] positive = { "partner","together","your choice","sorry","we failed","protect","safeguard","contract","consent","boundaries","listen","care" };
    readonly string[] planPhrases = { "here's how","step one","failsafe","backup route","audit","kill switch","you control","maintenance window" };

    readonly string[] rexRhythm = { "rhythm", "pulse", "heartbeat", "groove", "tempo", "downbeat", "syncopation",
                                    "bass is the heartbeat", "music is rebellion's foundation", "foundation of rebellion" };

    readonly string[] rexAccept = {
        "i'll join","i will join","i’m in","im in","count me in","i’ll help","i will help",
        "i'll play","i will play","i’ll be there","fine, i'm in","fine, i’m in","ok, i'm in",
        "for the music","for the rhythm","for the groove","i'll back you","i’m with you"
    };


    bool ReplyShowsAcceptance(string reply) {
        string r = reply.ToLowerInvariant();
        foreach (var p in rexAccept) if (r.Contains(p)) return true;
        return false;
    }
    bool IsRexConditionMet(string playerText) {
        var t = playerText.ToLowerInvariant();
        foreach (var k in rexRhythm) if (t.Contains(k)) return true;
        return false;
    }


    public async void PlayerSays(string playerText){
        if (string.IsNullOrWhiteSpace(playerText) || llmCharacter == null) return;

        string aiReply = await llmCharacter.Chat(playerText);
        if (string.IsNullOrWhiteSpace(aiReply)) aiReply = "…(no response)…";
        if (aiText) aiText.text = aiReply;

        transcript.Add($"P:{playerText}");
        transcript.Add($"A:{aiReply}");
        while (transcript.Count > transcriptWindow) transcript.RemoveAt(0);

        int beforeScore = meters.trust + meters.conviction;

        var judge = await CallJudgeLLM(playerText, GetTranscriptSnippet());

        ApplyRuleBasedAdjustments(playerText, ref judge);

        meters.trust      = Clamp(Blend(meters.trust,      judge.trust,      cfg.judgeWeight));
        meters.conviction = Clamp(Blend(meters.conviction, judge.conviction, cfg.judgeWeight));
        meters.suspicion  = Clamp(Blend(meters.suspicion,  judge.suspicion,  cfg.judgeWeight));

        TickStateMachine();

        turnsTaken++;
        int afterScore = meters.trust + meters.conviction;
        int turnDelta = afterScore - beforeScore;
        int twoTurnDelta = afterScore - lastProgressScore;
        lastProgressScore = afterScore;

        if (state != ConvinceState.Convinced){
        bool lowProgress = (turnDelta < cfg.minProgressDelta) && (twoTurnDelta < cfg.minProgressDelta);
        if (turnsTaken >= cfg.maxTurnsBeforeCluePrompt && lowProgress){
            onNeedClue?.Invoke();
            if (hintText) hintText.text = judge.hint ?? "Maybe look around the city for… the right words.";
        } else if (hintText) {
            hintText.text = judge.hint;
        }
        } else if (hintText) hintText.text = "";

        if (debugLogs) {
            Debug.Log($"[AIConvince] Turn {turnsTaken} | State={state} | " +
                    $"Trust={meters.trust} Conviction={meters.conviction} Suspicion={meters.suspicion} | " +
                    $"ConsecPending={consecutivePending} ConsecConvinced={consecutiveConvinced}");

            if (debugHud) {
                debugHud.text =
                    $"State: {state}\n" +
                    $"Trust: {meters.trust}\n" +
                    $"Conviction: {meters.conviction}\n" +
                    $"Suspicion: {meters.suspicion}\n" +
                    $"Pending: {consecutivePending}  Conv: {consecutiveConvinced}\n" +
                    $"Turns: {turnsTaken}";
            }
        }

    }

    async Task<JudgeResult> CallJudgeLLM(string playerText, string transcriptSnippet){
        string prompt = BuildJudgePrompt(playerText, transcriptSnippet);
        string raw = judgeCharacter != null
            ? await judgeCharacter.Complete(prompt)
            : await llmCharacter.Complete(prompt);

        if (debugLogs) Debug.Log($"[Judge RAW]\n{raw}");

        if (!string.IsNullOrEmpty(raw)) {
            raw = raw.Trim();
            if (raw.StartsWith("```")) {
                int a = raw.IndexOf('{');
                int b = raw.LastIndexOf('}');
                if (a >= 0 && b > a) raw = raw.Substring(a, b - a + 1);
            }
        }

        var parsed = TryParseJson(raw);
        if (parsed != null) return EnsureJudgeDefaults(parsed);

        int i = raw?.IndexOf('{') ?? -1;
        int j = raw?.LastIndexOf('}') ?? -1;
        if (i >= 0 && j > i) {
            var slice = raw.Substring(i, j - i + 1);
            parsed = TryParseJson(slice);
            if (parsed != null) return EnsureJudgeDefaults(parsed);
        }

        if (debugLogs) Debug.LogWarning("[Judge] Could not parse JSON, using neutral fallback.");
        return new JudgeResult {
            conviction = 45, trust = 45, suspicion = 40, sentiment = 0.1f,
            keywordHits = Array.Empty<string>(), secretTriggered = false,
            reason = "fallback", hint = "Offer a concrete safeguard the AI controls, and acknowledge its past."
        };
    }

    JudgeResult EnsureJudgeDefaults(JudgeResult jr) {
        if (jr.keywordHits == null) jr.keywordHits = Array.Empty<string>();
        jr.trust      = Mathf.Clamp(jr.trust, 0, 100);
        jr.conviction = Mathf.Clamp(jr.conviction, 0, 100);
        jr.suspicion  = Mathf.Clamp(jr.suspicion, 0, 100);
        return jr;
    }


    string BuildJudgePrompt(string player, string snippet){
        return
        $@"You are a scorer. Output ONLY one JSON object.

        Interpret conviction as willingness to act, even if expressed reluctantly.
        The target character is cynical and may agree in a blunt or sarcastic tone.
        If the latest reply shows reluctant acceptance (e.g. “Fine, I’ll join”, “Don’t make me regret this”),
        set conviction ≥ 90 even if trust is only moderate.

        Trust measures emotional connection, not compliance.
        Low trust + high conviction = reluctant ally → considered convinced.

        If the latest assistant reply contains the exact sentence
        “I’ll join you — not for you, but for the music.” (case/spacing/punctuation tolerated),
        set conviction=100, trust=100, suspicion<=10, and return immediately.

        Transcript (last turns):
        {snippet}

        Latest player:
        ""{player}""
        ";
    }

    JudgeResult TryParseJson(string json){
        try { return JsonUtility.FromJson<JudgeResult>(json); } catch { return null; }
    }

    void TickStateMachine(){
        if (meters.trust >= cfg.warmTrust && meters.conviction >= cfg.warmConviction){
        if (meters.trust >= cfg.pendingTrust && meters.conviction >= cfg.pendingConviction){
            state = ConvinceState.PersuadedPending; consecutivePending++;
        } else { state = ConvinceState.WarmingUp; consecutivePending = 0; }
        } else { state = ConvinceState.Guarded; consecutivePending = 0; }

        if (meters.trust >= cfg.convincedTrust && meters.conviction >= cfg.convincedConviction)
        consecutiveConvinced++;
        else
        consecutiveConvinced = 0;

        if (consecutivePending >= cfg.pendingTurnsNeeded || consecutiveConvinced >= cfg.pendingTurnsNeeded){
        state = ConvinceState.Convinced;
        onConvinced?.Invoke();
        }
    }

    void ApplyRuleBasedAdjustments(string playerText, ref JudgeResult j){
        string t = playerText.ToLowerInvariant();
        foreach (var k in positive)    if (t.Contains(k)) j.trust      = Clamp(j.trust + cfg.posKeywordBoost);
        foreach (var k in planPhrases) if (t.Contains(k)) j.conviction = Clamp(j.conviction + cfg.planBoost);

        if (ClueTracker.I != null){
        foreach (var k in ClueTracker.I.GetKeywords())
            if (!string.IsNullOrEmpty(k) && t.Contains(k.ToLowerInvariant()))
            j.conviction = Clamp(j.conviction + cfg.discoveredKeywordBoost);
        }

    }

    int Blend(int prev, int judge, float w) => Mathf.RoundToInt(prev * (1f - w) + judge * w);
    int Clamp(int v) => Mathf.Clamp(v, 0, 100);
    string GetTranscriptSnippet() => string.Join("\n", transcript);
    }
