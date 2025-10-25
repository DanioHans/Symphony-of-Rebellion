using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour {
    public static GameState I { get; private set; }

    public HashSet<string> flags = new();
    public int storyStep = 0;

    public string currentPlanetId;
    public string currentAreaId;
    public bool reopenPlanetViewAfterReturn;   // open planet view automatically after minigame
    public HashSet<string> completedAreas = new();

    public HashSet<string> storyboardsSeen = new();

    public string pendingStoryboardId;


    void Awake() {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);
        Load();
    }

    public bool HasFlag(string key) => !string.IsNullOrEmpty(key) && flags.Contains(key);

    public void SetFlag(string key) {
        if (string.IsNullOrEmpty(key)) return;
        if (flags.Add(key)) Save();
    }

    public void AdvanceStoryToAtLeast(int step) {
        if (storyStep < step) { storyStep = step; Save(); }
    }

    const string KFlags = "gs_flags";
    const string KStory = "gs_story";
    const string KCompleted = "gs_completed";

    void Save() {
        PlayerPrefs.SetString(KFlags, string.Join(",", flags));
        PlayerPrefs.SetInt(KStory, storyStep);
        PlayerPrefs.SetString(KCompleted, string.Join(",", completedAreas));

    }
    void Load() {
        flags.Clear();
        var s = PlayerPrefs.GetString(KFlags, "");
        if (!string.IsNullOrEmpty(s)) foreach (var f in s.Split(',')) if (!string.IsNullOrEmpty(f)) flags.Add(f);

        storyStep = PlayerPrefs.GetInt(KStory, 0);

        completedAreas.Clear();
        var c = PlayerPrefs.GetString(KCompleted, "");
        if (!string.IsNullOrEmpty(c)) foreach (var k in c.Split(',')) if (!string.IsNullOrEmpty(k)) completedAreas.Add(k);
    }

    public void MarkAreaComplete(string planetId, string areaId) {
        completedAreas.Add($"{planetId}:{areaId}");
        Save();
    }
    public bool IsAreaComplete(string planetId, string areaId) => completedAreas.Contains($"{planetId}:{areaId}");

    public void ResetGameState()
    {
        flags.Clear();
        completedAreas.Clear();
        storyStep = 0;
        currentPlanetId = currentAreaId = "";
        reopenPlanetViewAfterReturn = false;

        // delete only our keys
        PlayerPrefs.DeleteKey("gs_flags");
        PlayerPrefs.DeleteKey("gs_story");
        PlayerPrefs.DeleteKey("gs_completed");
        PlayerPrefs.Save();
    }

    public bool HasSeenStoryboard(string id) => storyboardsSeen.Contains(id);

    public void MarkStoryboardSeen(string id) {
        if (storyboardsSeen.Add(id)) Save();
    }

    public void SetPendingStoryboard(string id) {
        pendingStoryboardId = id;
    }

    public string ConsumePendingStoryboard() {
        var id = pendingStoryboardId;
        pendingStoryboardId = null;
        return id;
    }
}
