using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class OsuTaikoImporter
{
    // Bit flags for osu! hitSound
    const int HS_NORMAL  = 1 << 0; // 1 (unused for lane)
    const int HS_WHISTLE = 1 << 1; // 2 -> rim
    const int HS_FINISH  = 1 << 2; // 4 -> big (ignored here)
    const int HS_CLAP    = 1 << 3; // 8 -> rim

    struct TimingPoint
    {
        public double timeMs;     // when this timing point starts
        public double beatLength; // ms per beat; negative = inherited (SV), positive = uninherited (real BPM)
        public bool uninherited;  // true if "red line" (BPM)
    }

    /// <summary>
    /// Parse a Taiko .osu file into a runtime TaikoChart (ScriptableObject, not yet saved).
    /// </summary>
    public static TaikoChart ParseFile(string osuPath, float defaultApproachBeats = 2f, int defaultAllowedMisses = 8, int defaultWinScore = 5000)
    {
        if (!File.Exists(osuPath))
        {
            Debug.LogError("[OsuTaikoImporter] File not found: " + osuPath);
            return null;
        }

        var lines = File.ReadAllLines(osuPath);
        var notes = new List<TaikoChart.Note>(256);
        var timing = new List<TimingPoint>(8);

        string audioFilename = null;
        int mode = -1;

        // Sections
        bool inTiming = false, inHitObjects = false;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("//")) continue;

            // Section switches
            if (line.StartsWith("["))
            {
                inTiming = line.Equals("[TimingPoints]", StringComparison.OrdinalIgnoreCase);
                inHitObjects = line.Equals("[HitObjects]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            // General/meta
            if (!inTiming && !inHitObjects)
            {
                if (line.StartsWith("AudioFilename:", StringComparison.OrdinalIgnoreCase))
                    audioFilename = line.Split(new[] {':'}, 2)[1].Trim();
                else if (line.StartsWith("Mode:", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(line.Split(':')[1].Trim(), out mode);

                continue;
            }

            // Timing points
            if (inTiming)
            {
                // time,beatLength,meter,sampleSet,sampleIndex,volume,uninherited,effects
                var parts = line.Split(',');
                if (parts.Length >= 2 &&
                    double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var t) &&
                    double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var beatLen))
                {
                    bool uninherited = true;
                    if (parts.Length >= 7)
                    {
                        // 1 = uninherited (red), 0 = inherited (green)
                        if (int.TryParse(parts[6], out var ui)) uninherited = ui != 0;
                    }
                    timing.Add(new TimingPoint { timeMs = t, beatLength = beatLen, uninherited = uninherited });
                }
                continue;
            }

            // HitObjects (only circles are expected for Taiko; sliders/spinners would be type flags but we ignore)
            if (inHitObjects)
            {
                // x,y,time,type,hitSound,objectParams,hitSample
                var parts = line.Split(',');
                if (parts.Length < 5) continue;

                // time (ms)
                if (!float.TryParse(parts[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var timeMsF))
                    continue;
                float timeMs = timeMsF;

                // hitSound
                if (!int.TryParse(parts[4], out var hitSound)) hitSound = 0;

                // Lane mapping: Kat (right) if whistle or clap is present; otherwise Don (left)
                bool isRim = ( (hitSound & HS_WHISTLE) != 0 ) || ( (hitSound & HS_CLAP) != 0 );
                var lane = isRim ? TaikoLane.Right : TaikoLane.Left;

                // convert times to beats after we know BPM & offset.
                // Temporarily stash timeMs; convert below.
                notes.Add(new TaikoChart.Note { beat = timeMs, lane = lane }); // beat field temporarily holds timeMs
            }
        }

        if (mode != -1 && mode != 1)
            Debug.LogWarning($"[OsuTaikoImporter] .osu Mode={mode} (expected 1 for Taiko). Proceeding anyway.");

        // Find first uninherited (red) timing point — base BPM/offset
        TimingPoint? baseTp = null;
        foreach (var tp in timing)
        {
            if (tp.uninherited && tp.beatLength > 0)
            {
                baseTp = tp;
                break;
            }
        }
        if (baseTp == null)
        {
            Debug.LogWarning("[OsuTaikoImporter] No uninherited timing point found. Defaulting to 120 BPM, offset 0.");
            baseTp = new TimingPoint { timeMs = 0, beatLength = 500.0, uninherited = true }; // 120 BPM
        }

        double offsetMs = baseTp.Value.timeMs;
        double spbMs = baseTp.Value.beatLength; // ms per beat
        float bpm = (float)(60000.0 / spbMs);

        // Convert stored timeMs → beat using base BPM/offset
        for (int i = 0; i < notes.Count; i++)
        {
            var n = notes[i];
            double beat = (n.beat - (float)offsetMs) / spbMs; // (timeMs - offset) / (ms/beat)
            n.beat = (float)beat;
            notes[i] = n;
        }

        // Build chart ScriptableObject
        var chart = ScriptableObject.CreateInstance<TaikoChart>();
        chart.bpm = bpm;
        chart.songOffsetSec = (float)(offsetMs / 1000.0);
        chart.approachBeats = defaultApproachBeats;
        chart.allowedMisses = defaultAllowedMisses;
        chart.winScore = defaultWinScore;
        chart.notes = notes;

        return chart;
    }

#if UNITY_EDITOR
    [MenuItem("Assets/Import osu! Taiko (.osu) to TaikoChart", true)]
    static bool CanImportOsu() {
        var obj = Selection.activeObject as TextAsset;
        return obj != null && obj.name.EndsWith(".osu", StringComparison.OrdinalIgnoreCase);
    }

    [MenuItem("Assets/Import osu! Taiko (.osu) to TaikoChart")]
    static void ImportSelectedOsu()
    {
        var txt = Selection.activeObject as TextAsset;
        if (txt == null)
        {
            EditorUtility.DisplayDialog("Import osu! Taiko", "Select a .osu TextAsset in the Project first.", "OK");
            return;
        }

        // Write the TextAsset to a temp file (ParseFile expects a filesystem path)
        string tempPath = Path.Combine(Application.dataPath, "../Temp_Import osu.osu");
        File.WriteAllText(tempPath, txt.text);

        var chart = ParseFile(tempPath);
        try { File.Delete(tempPath); } catch {}

        if (chart == null)
        {
            EditorUtility.DisplayDialog("Import osu! Taiko", "Failed to parse .osu file.", "OK");
            return;
        }

        string defaultName = Path.GetFileNameWithoutExtension(txt.name) + "_TaikoChart";
        string path = EditorUtility.SaveFilePanelInProject("Save TaikoChart", defaultName, "asset", "Choose location for the TaikoChart asset.");
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(chart, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(chart);
            Debug.Log($"[OsuTaikoImporter] Imported {txt.name} → {path}\nBPM={chart.bpm:F2}, Offset={chart.songOffsetSec:F3}s, Notes={chart.notes.Count}");
        }
    }
#endif
}
