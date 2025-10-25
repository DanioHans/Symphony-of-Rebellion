using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaikoMinigame : MonoBehaviour {
    [Header("Wiring")]
    public MinigameController minigame;
    public TaikoChart chart;
    public AudioSource music;
    public RectTransform playArea;
    public RectTransform hitLine;
    public GameObject notePrefab;
    public TMP_Text scoreText;
    public TMP_Text comboText;
    public Slider healthBar;

    [Header("Keys")]
    public KeyCode leftKey  = KeyCode.J;
    public KeyCode rightKey = KeyCode.K;

    [Header("Judgement Windows (ms)")]
    [Tooltip("Presses earlier than this are IGNORED (no miss, no consume).")]
    public float earlyIgnoreMs = 150f;

    [Tooltip("± window for Perfect.")]
    public float perfectWindowMs = 35f;

    [Tooltip("± window for Good (wider than Perfect).")]
    public float goodWindowMs = 80f;

    [Tooltip("After this late amount, the note auto-misses (no longer hittable).")]
    public float lateMissMs = 120f;


    [Header("Speed Tuning")]
    public float lanesYSpacing = 120f;
    public float baseY;

    [Header("Spawn & Lane Visuals")]
    [Tooltip("How many pixels before the left edge a note spawns (negative = offscreen)")]
    public float spawnOffsetX = -80f;

    [Tooltip("Per-lane glyphs shown on the note (set null/empty to hide)")]
    public string leftGlyph = "J";
    public string rightGlyph = "K";
    
    [Header("Lane Colors")]
    public Color leftColor = Color.red;
    public Color rightColor = Color.blue;


    [Header("Sync / Calibration")]
    [Tooltip("Countdown before music starts, in seconds.")]
    public float countdownSec = 2.0f;

    [Tooltip("Shifts judgement in ms (positive = you must hit later). Use to compensate input/audio latency.")]
    public float inputOffsetMs = 0f;

    [Tooltip("Shifts note travel earlier/later in beats (positive = notes arrive later).")]
    public float visualOffsetBeats = 0f;

    [Header("Countdown UI")]
    public TMP_Text countdownText;  // optional; leave null if not needed



    float spb;
    double songStartDsp;
    int spawnIndex;
    int judgeIndex;
    readonly List<TNote> live = new();
    int score, combo, maxCombo, missCount;
    bool started, finished;

    class TNote {
        public TaikoLane lane;
        public float beat;
        public double hitTimeDsp;
        public RectTransform rt;
        public TMP_Text label;
        public bool judged;
    }

    public void Begin() {
        if (!chart || chart.bpm <= 0f) { Debug.LogError("Taiko: invalid chart"); return; }

        spb = 60f / chart.bpm;

        foreach (var n in live) if (n.rt) Destroy(n.rt.gameObject);
        live.Clear();
        spawnIndex = 0; judgeIndex = 0;
        score = combo = maxCombo = missCount = 0;
        UpdateHUD();

        double now = AudioSettings.dspTime;
        songStartDsp = now + Mathf.Max(0.05f, countdownSec);
        started = true; finished = false;

        if (music) {
            music.clip = chart.music;
            music.playOnAwake = false;
            music.loop = false;
            music.spatialBlend = 0f;
            if (music.clip) {
                music.PlayScheduled(songStartDsp + chart.songOffsetSec);
            }
        }

        if (countdownText) countdownText.gameObject.SetActive(true);
    }


    void Update() {
        if (!minigame.playing) return;
        if (!started) Begin();
        if (finished) return;

        double now = AudioSettings.dspTime;
        double zero = songStartDsp + chart.songOffsetSec;
        float songBeat = (float)((now - zero) / spb);
        float approach = chart.approachBeats + visualOffsetBeats;

        if (countdownText) {
            double toGo = songStartDsp - now;
            if (toGo > 1.5)      countdownText.text = "3";
            else if (toGo > 0.5) countdownText.text = "2";
            else if (toGo > 0.0) countdownText.text = "1";
            else if (toGo > -0.3) countdownText.text = "GO!";
            else countdownText.gameObject.SetActive(false);
        }

        // 1) Spawn when current time passes each note's spawn time
        // spawnBeat = noteBeat - approach
        while (spawnIndex < chart.notes.Count) {
            var def = chart.notes[spawnIndex];
            float spawnBeat = def.beat - approach;

            if (songBeat < spawnBeat) break;

            var go = Instantiate(notePrefab, playArea);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);

            float laneYOffset = (def.lane == TaikoLane.Left ? -0.5f : +0.5f) * lanesYSpacing + baseY;
            rt.anchoredPosition = new Vector2(spawnOffsetX, laneYOffset);

            var circle = go.transform.Find("Circle")?.GetComponent<UnityEngine.UI.Image>();
            if (circle) {
                circle.color = (def.lane == TaikoLane.Left ? leftColor : rightColor);
            }

            // Label (J/K)
            TMP_Text lab = go.GetComponentInChildren<TMP_Text>(true);
            if (!lab) {
                var t = new GameObject("Glyph", typeof(RectTransform));
                t.transform.SetParent(go.transform, false);
                var tr = (RectTransform)t.transform;
                tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
                tr.sizeDelta = new Vector2(60, 60);
                lab = t.AddComponent<TMP_Text>();
                lab.alignment = TextAlignmentOptions.Center;
                lab.fontSize = 36f;
                lab.color = Color.black;
            }
            lab.text = (def.lane == TaikoLane.Left ? leftGlyph : rightGlyph);

            // Absolute DSP times for spawn & hit
            double hitTime  = zero + def.beat * spb;
            var tn = new TNote {
                lane = def.lane,
                beat = def.beat,
                hitTimeDsp = hitTime,
                rt = rt,
                label = lab,
                judged = false
            };
            live.Add(tn);
            spawnIndex++;
        }

        // 2) Animate notes: t = (songBeat - (beat-approach)) / approach
        float hitX = HitLineLocalX();
        for (int i = 0; i < live.Count; i++) {
            var n = live[i];
            if (!n.rt) continue;

            float spawnBeat = n.beat - approach;
            float t = Mathf.InverseLerp(spawnBeat, n.beat, songBeat);
            t = Mathf.Clamp01(t);

            float x = Mathf.Lerp(spawnOffsetX, hitX, t);
            var p = n.rt.anchoredPosition; p.x = x; n.rt.anchoredPosition = p;
        }

        // 3) Auto-miss if we passed beyond Good window
        while (judgeIndex < live.Count) {
            var n = live[judgeIndex];
            if (n.judged) { judgeIndex++; continue; }

            double ms = (AudioSettings.dspTime - n.hitTimeDsp) * 1000.0 - inputOffsetMs;
            if (ms < lateMissMs) break;          // still hittable or in grace
            n.judged = true; Miss(n);
            judgeIndex++;
        }

        // 4) Input
        if (Input.GetKeyDown(leftKey))  TryHit(TaikoLane.Left,  now);
        if (Input.GetKeyDown(rightKey)) TryHit(TaikoLane.Right, now);

        // 5) End when music stops (or no clip)
        if (!music || (music.clip && !music.isPlaying && now > songStartDsp + music.clip.length + 0.5)) {
            Finish();
        }
    }



    float HitLineLocalX() {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
        playArea,
        RectTransformUtility.WorldToScreenPoint(null, hitLine.position),
        null,
        out var localCenterSpace);

        float halfWidth = playArea.rect.width * 0.5f;
        return localCenterSpace.x + halfWidth;
    }

    void TryHit(TaikoLane lane, double nowDsp) {
        TNote best = null; 
        double bestAbs = double.MaxValue;
        double bestMs = 0.0;

        for (int i = 0; i < live.Count; i++) {
            var n = live[i];
            if (n.judged || n.lane != lane) continue;

            double ms = (nowDsp - n.hitTimeDsp) * 1000.0 - inputOffsetMs;
            double abs = System.Math.Abs(ms);

            if (abs < bestAbs) { bestAbs = abs; best = n; bestMs = ms; }

            if (ms < -(earlyIgnoreMs + 10.0)) break;
        }

        if (best == null) return;

        if (bestMs < -earlyIgnoreMs) return;

        double absMs = System.Math.Abs(bestMs);
        if (absMs <= perfectWindowMs) {
            best.judged = true; Hit(best, true);
        }
        else if (absMs <= goodWindowMs) {
            best.judged = true; Hit(best, false);
        }
        else {
            if (bestMs > goodWindowMs && bestMs <= lateMissMs) {
                best.judged = true; Miss(best);
            }
        }
    }

    void Hit(TNote n, bool perfect) {
        n.judged = true;
        combo++;
        maxCombo = Mathf.Max(maxCombo, combo);
        score += perfect ? 1000 + combo * 5 : 500 + combo * 3;
        Pop(n, perfect ? 1.05f : 1.0f);
        UpdateHUD();
    }

    void Miss(TNote n) {
        n.judged = true;
        combo = 0;
        missCount++;
        Pop(n, 0.9f);
        UpdateHUD();

        if (chart.allowedMisses > 0 && missCount >= chart.allowedMisses) {
            ImmediateLose();
        }
    }

    void Pop(TNote n, float scale) {
        if (n.rt) {
            Destroy(n.rt.gameObject);
            n.rt = null;
        }
    }

    void UpdateHUD() {
        if (scoreText) scoreText.text = score.ToString();
        if (comboText) comboText.text = (combo > 0 ? $"{combo}x" : "");
        if (healthBar) {
            if (chart.allowedMisses > 0) {
                float left = Mathf.Clamp01(1f - (float)missCount / chart.allowedMisses);
                healthBar.value = left;
            }
        }
    }

    void ImmediateLose() {
        if (finished) return;
        finished = true;
        if (music && music.isPlaying) music.Stop();
        minigame?.OnLose();
    }

    void Finish() {
        if (finished) return;
        finished = true;
        bool win = (chart.winScore <= 0) || (score >= chart.winScore);
        GameState.I.SetFlag("verdantia_completed");
        if (win) minigame?.OnWin(); else minigame?.OnLose();
    }
}
