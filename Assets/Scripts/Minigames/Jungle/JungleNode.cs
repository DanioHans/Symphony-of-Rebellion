using UnityEngine;

[CreateAssetMenu(menuName="Game/Jungle Node")]
public class JungleNode : ScriptableObject {
    [Header("Visuals")]
    public Sprite background;
    public Sprite hintOverlay;

    [Header("Behavior")]
    public bool isDeadEnd = false;
    public AudioClip deadEndSfx;

    [Header("Sound Hotspot (optional)")]
    public bool hasSoundHotspot = false;
    [Range(0f,1f)] public float hotspotX01 = 0.5f;
    [Range(0f,1f)] public float hotspotY01 = 0.5f;
    public float triggerRadiusPx = 80f;
    public AudioClip hotspotLoop;
    public int hotspotLeadsToIndex = -1;

    [Header("Exits")]
    public Exit[] exits;

    [System.Serializable]
    public struct Exit {
        public string label;
        public int targetIndex;
        public bool hasRect;
        public Rect rect01;
    }
}
