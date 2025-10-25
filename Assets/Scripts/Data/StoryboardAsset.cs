using UnityEngine;

[CreateAssetMenu(menuName = "Game/Storyboard")]
public class StoryboardAsset : ScriptableObject
{
    public Slide[] slides;

    [System.Serializable]
    public class Slide
    {
        public Sprite image;

        [Header("Caption")]
        [TextArea(2, 6)] public string text;     // single block
        [TextArea(1, 4)] public string[] lines;  // multiple lines
        public bool autoAdvanceLines = false;    // auto page each line?
        public float holdSecondsPerLine = 3.5f;  // when autoAdvanceLines is true

        [Header("Voice Over (optional)")]
        public AudioClip voiceOver;              // whole slide VO (keeps playing)
        public bool autoAdvance = true;          // auto-advance SLIDE after last line
        public float holdSeconds = 4f;           // fallback if no lines

        [Header("Ken Burns (optional)")]
        public bool kenBurns = false;
        public Vector2 panStart01 = new(0.5f, 0.5f);
        public Vector2 panEnd01   = new(0.5f, 0.5f);
        public float zoomStart = 1.0f;
        public float zoomEnd   = 1.08f;
    }

}
