#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public static class DevTools
{
    [MenuItem("Dev/Reset Game State %&r")]
    public static void ResetGS()
    {
        if (GameState.I != null) GameState.I.ResetGameState();
        else {
            PlayerPrefs.DeleteKey("gs_flags");
            PlayerPrefs.DeleteKey("gs_story");
            PlayerPrefs.DeleteKey("gs_completed");
            PlayerPrefs.Save();
        }
        Debug.Log("[Dev] GameState reset.");
    }

    [MenuItem("Dev/Story/Set Step 0")] public static void S0() => SetStory(0);
    [MenuItem("Dev/Story/Set Step 1")] public static void S1() => SetStory(1);
    [MenuItem("Dev/Story/Set Step 2")] public static void S2() => SetStory(2);
    [MenuItem("Dev/Story/Set Step 3")] public static void S3() => SetStory(3);
    [MenuItem("Dev/Story/Set Step 4")] public static void S4() => SetStory(4);
    static void SetStory(int s)
    {
        var gs = GetGS();
        gs.storyStep = s;
        SaveGS(gs);
        Debug.Log($"[Dev] storyStep = {s}");
    }

    [MenuItem("Dev/Flags/Toggle hasBassPlayer")] public static void ToggleBass()
        => ToggleFlag("hasBassPlayer");

    [MenuItem("Dev/Areas/Complete All Areas")] public static void CompleteAllAreas()
    {
        var gs = GetGS();
        foreach (var pd in LoadAllPlanetData())
        {
            if (pd.areas == null) continue;
            foreach (var a in pd.areas)
                gs.completedAreas.Add($"{pd.planetId}:{a.id}");
        }
        SaveGS(gs);
        Debug.Log("[Dev] Marked ALL areas complete.");
    }

    [MenuItem("Dev/Areas/Clear Completed Areas")] public static void ClearCompleted()
    {
        var gs = GetGS();
        gs.completedAreas.Clear();
        SaveGS(gs);
        Debug.Log("[Dev] Cleared completed areas.");
    }

    [MenuItem("Dev/Open StarMap Scene")]
    public static void OpenStarMapScene()
    {
        string path = AssetDatabase.FindAssets("t:Scene StarMap")
            .Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
        if (!string.IsNullOrEmpty(path)) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
        else Debug.LogWarning("[Dev] Couldn't find a scene named 'StarMap'.");
    }

    [MenuItem("Dev/Dev Panel %#d")]
    public static void OpenPanel() => DevPanel.ShowWindow();

    static GameState GetGS()
    {
        if (GameState.I != null) return GameState.I;
        var gs = Object.FindObjectOfType<GameState>();
        if (gs != null) return gs;

        var go = new GameObject("~DevGameState");
        go.hideFlags = HideFlags.HideAndDontSave;
        return go.AddComponent<GameState>();
    }

    static void SaveGS(GameState gs)
    {
        PlayerPrefs.Save();
    }

    static void ToggleFlag(string key)
    {
        var gs = GetGS();
        if (gs.flags.Contains(key)) gs.flags.Remove(key);
        else gs.flags.Add(key);
        SaveGS(gs);
        Debug.Log($"[Dev] {(gs.flags.Contains(key) ? "Set" : "Cleared")} flag '{key}'.");
    }

    static IEnumerable<PlanetData> LoadAllPlanetData()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:PlanetData"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<PlanetData>(path);
            if (asset != null) yield return asset;
        }
    }
}

public class DevPanel : EditorWindow
{
    Vector2 _scroll;
    string _newFlag = "";
    string _planetId = "";
    string _areaId = "";

    public static void ShowWindow()
    {
        var w = GetWindow<DevPanel>("Dev Panel");
        w.minSize = new Vector2(360, 280);
        w.Show();
    }

    void OnGUI()
    {
        var gs = DevTools_GetGS();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        GUILayout.Label("Story", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        int step = EditorGUILayout.IntField("storyStep", gs.storyStep);
        if (GUILayout.Button("Set", GUILayout.Width(60))) { gs.storyStep = step; DevTools_SaveGS(gs); }
        if (GUILayout.Button("0", GUILayout.Width(28))) { gs.storyStep = 0; DevTools_SaveGS(gs); }
        if (GUILayout.Button("+1", GUILayout.Width(34))) { gs.storyStep++; DevTools_SaveGS(gs); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        GUILayout.Label("Flags", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        _newFlag = EditorGUILayout.TextField("Add/Toggle", _newFlag);
        if (GUILayout.Button("Toggle", GUILayout.Width(70)) && !string.IsNullOrEmpty(_newFlag))
        {
            if (gs.flags.Contains(_newFlag)) gs.flags.Remove(_newFlag);
            else gs.flags.Add(_newFlag);
            DevTools_SaveGS(gs);
        }
        EditorGUILayout.EndHorizontal();

        if (gs.flags.Count > 0)
        {
            EditorGUILayout.LabelField("Current Flags:");
            foreach (var f in gs.flags.ToList())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("• " + f);
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    gs.flags.Remove(f);
                    DevTools_SaveGS(gs);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else EditorGUILayout.HelpBox("No flags set.", MessageType.Info);

        EditorGUILayout.Space(6);
        GUILayout.Label("Areas", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        _planetId = EditorGUILayout.TextField("PlanetId", _planetId);
        _areaId   = EditorGUILayout.TextField("AreaId", _areaId);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Mark Complete"))
        {
            if (!string.IsNullOrEmpty(_planetId) && !string.IsNullOrEmpty(_areaId))
            {
                gs.completedAreas.Add($"{_planetId}:{_areaId}");
                DevTools_SaveGS(gs);
            }
        }
        if (GUILayout.Button("Clear All Completed"))
        {
            gs.completedAreas.Clear();
            DevTools_SaveGS(gs);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        GUILayout.Label("Quick Actions", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset GameState")) { DevTools.ResetGS(); }
        if (GUILayout.Button("Open StarMap"))   { DevTools.OpenStarMapScene(); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Tip: Shortcut Ctrl/Cmd+Alt+R resets GameState.\nCtrl/Cmd+Shift+D opens this panel.", MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    GameState DevTools_GetGS()
    {
        if (GameState.I != null) return GameState.I;
        var gs = FindObjectOfType<GameState>();
        if (gs != null) return gs;
        var go = new GameObject("~DevGameState");
        go.hideFlags = HideFlags.HideAndDontSave;
        return go.AddComponent<GameState>();
    }
    void DevTools_SaveGS(GameState gs) { PlayerPrefs.Save(); Repaint(); }
}
#endif
