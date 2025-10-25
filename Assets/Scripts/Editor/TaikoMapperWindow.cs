#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TaikoMapperWindow : EditorWindow
{
    // Audio + timing
    AudioClip clip;
    float bpm = 120f;
    float songOffsetSec = 0f;

    // Record config
    float approachBeats = 2f;
    int   allowedMisses = 8;
    int   winScore = 5000;

    // Quantize
    string[] gridNames = { "No quantize", "1/2 beat", "1/4 beat", "1/8 beat" };
    float[]  gridSizes = { 0f, 0.5f, 0.25f, 0.125f };
    int gridIndex = 2; // 1/4 beat by default
    float dedupeWindowBeats = 0.05f;

    // Input keys
    KeyCode leftKey = KeyCode.J;
    KeyCode rightKey = KeyCode.K;

    // Runtime
    GameObject audioGO;
    AudioSource src;
    double startDsp = 0;
    bool armed = false;
    bool playing = false;

    struct RawNote { public float beat; public TaikoLane lane; }
    readonly List<RawNote> recorded = new();

    [MenuItem("Tools/Taiko Mapper")]
    static void Open() => GetWindow<TaikoMapperWindow>("Taiko Mapper");

    void OnGUI()
    {
        GUILayout.Label("Audio & Timing", EditorStyles.boldLabel);
        clip = (AudioClip)EditorGUILayout.ObjectField("Music", clip, typeof(AudioClip), false);
        bpm = EditorGUILayout.FloatField("BPM", bpm);
        songOffsetSec = EditorGUILayout.FloatField("Song Offset (sec)", songOffsetSec);

        EditorGUILayout.Space();
        GUILayout.Label("Gameplay Defaults (saved to chart)", EditorStyles.boldLabel);
        approachBeats = EditorGUILayout.FloatField("Approach Beats", approachBeats);
        allowedMisses = EditorGUILayout.IntField("Allowed Misses", allowedMisses);
        winScore = EditorGUILayout.IntField("Win Score", winScore);

        EditorGUILayout.Space();
        GUILayout.Label("Quantize", EditorStyles.boldLabel);
        gridIndex = EditorGUILayout.Popup("Grid", gridIndex, gridNames);
        dedupeWindowBeats = EditorGUILayout.Slider("Merge dupes (beats)", dedupeWindowBeats, 0f, 0.2f);

        EditorGUILayout.Space();
        GUILayout.Label("Controls", EditorStyles.boldLabel);
        leftKey  = (KeyCode)EditorGUILayout.EnumPopup("Left Key", leftKey);
        rightKey = (KeyCode)EditorGUILayout.EnumPopup("Right Key", rightKey);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = !armed && clip && bpm > 0f;
            if (GUILayout.Button("Arm & Play", GUILayout.Height(28))) ArmAndPlay();
            GUI.enabled = armed;
            if (GUILayout.Button("Stop", GUILayout.Height(28))) StopPlayback();
            GUI.enabled = true;
        }

        EditorGUILayout.Space();
        GUILayout.Label($"Recorded: {recorded.Count} notes", EditorStyles.miniBoldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = recorded.Count > 0;
            if (GUILayout.Button("Quantize + Save HARD chart…"))
            {
                var notes = BuildNotes(recorded, bpm, gridSizes[gridIndex], dedupeWindowBeats);
                SaveChartAsset("Hard", notes);
            }
            if (GUILayout.Button("Make EASY from HARD…"))
            {
            }
            GUI.enabled = true;
        }

        GUILayout.Space(8);
        EditorGUILayout.HelpBox("Tap J/K while playing to lay notes. Use quantize to clean timing.\nEasy chart helper keeps downbeats & enforces min spacing.", MessageType.Info);

        // Focus to capture key events
        if (armed) Focus();

        // Key capture
        var e = Event.current;
        if (armed && e != null && e.type == EventType.KeyDown)
        {
            if (e.keyCode == leftKey)  AddTap(TaikoLane.Left);
            if (e.keyCode == rightKey) AddTap(TaikoLane.Right);
        }
    }

    void OnDisable() => StopPlayback();

    void ArmAndPlay()
    {
        StopPlayback();

        audioGO = new GameObject("~TaikoMapper_AudioPreview");
        audioGO.hideFlags = HideFlags.HideAndDontSave;
        src = audioGO.AddComponent<AudioSource>();
        src.clip = clip;
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;

        recorded.Clear();
        startDsp = AudioSettings.dspTime + 0.05;
        src.PlayScheduled(startDsp + songOffsetSec);

        playing = true;
        armed = true;
        EditorApplication.update += OnEditorUpdate;
    }

    void StopPlayback()
    {
        if (src) { try { src.Stop(); } catch {} }
        if (audioGO) DestroyImmediate(audioGO);
        src = null; audioGO = null;
        armed = false; playing = false;
        EditorApplication.update -= OnEditorUpdate;
        Repaint();
    }

    void OnEditorUpdate()
    {
        if (!playing || src == null) { StopPlayback(); return; }
        if (!src.isPlaying && AudioSettings.dspTime > startDsp + (clip ? clip.length : 0f) + 0.5f)
            StopPlayback();
        Repaint();
    }

    void AddTap(TaikoLane lane)
    {
        if (!armed || bpm <= 0f) return;
        double now = AudioSettings.dspTime;
        double zero = startDsp + songOffsetSec;
        float spb = 60f / bpm;
        float beat = (float)((now - zero) / spb);
        recorded.Add(new RawNote { beat = beat, lane = lane });
    }

    static List<TaikoChart.Note> BuildNotes(List<RawNote> raw, float bpm, float grid, float mergeWindowBeats)
    {
        var list = new List<TaikoChart.Note>(raw.Count);
        foreach (var r in raw)
        {
            float b = r.beat;
            if (grid > 0f) b = Mathf.Round(b / grid) * grid;
            list.Add(new TaikoChart.Note { beat = b, lane = r.lane });
        }
        list = list.OrderBy(n => n.beat).ToList();
        if (mergeWindowBeats > 0f)
        {
            var merged = new List<TaikoChart.Note>();
            foreach (var n in list)
            {
                if (merged.Count == 0) { merged.Add(n); continue; }
                var last = merged[merged.Count - 1];
            }
            list = merged;
        }
        return list;
    }

    void SaveChartAsset(string difficultySuffix, List<TaikoChart.Note> notes)
    {
        var asset = ScriptableObject.CreateInstance<TaikoChart>();
        asset.music = clip;
        asset.bpm = bpm;
        asset.songOffsetSec = songOffsetSec;
        asset.approachBeats = approachBeats;
        asset.allowedMisses = allowedMisses;
        asset.winScore = winScore;
        asset.notes = notes;

        string baseName = clip ? clip.name : "Taiko";
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Taiko Chart",
            $"{baseName}_{difficultySuffix}",
            "asset",
            "Choose a location for the TaikoChart asset."
        );
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
        }
    }

}
#endif

