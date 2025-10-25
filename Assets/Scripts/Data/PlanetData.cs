using UnityEngine;

public enum PlanetLockCondition {
    AlwaysUnlocked,
    FlagRequired,          // e.g., "carceris_completed"
    StoryStepReached       // e.g., storyIndex >= N
}


[System.Serializable] public class AreaDef {
    public string id;
    public Sprite background;
    [Range(0,1)] public float x01 = 0.5f;
    [Range(0,1)] public float y01 = 0.5f;
    public string minigameId;
    public string sceneToLoad;
    public bool completed = false;
}


[CreateAssetMenu(menuName="Game/Planet Data")]
public class PlanetData : ScriptableObject {
    public string planetId;
    public string displayName;
    public float scale = 1.0f;
    public Sprite icon;           // star-map button image (can be a temp placeholder)
    public float iconScale = 1.0f;  // scale factor for star-map button image
    public Vector2 anchor01;      // position on map in 0..1 (x,y normalized)
    public PlanetLockCondition lockCondition = PlanetLockCondition.AlwaysUnlocked;
    public string requiredFlag;   // used when lockCondition == FlagRequired
    public int requiredStoryStep; // used when lockCondition == StoryStepReached
    public string sceneToLoad;
    public Sprite planetViewBG;     // big background
    public AreaDef[] areas;
    public bool unlockedByDefault = false;

}
