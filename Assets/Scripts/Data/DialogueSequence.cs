using UnityEngine;

[CreateAssetMenu(menuName="Game/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject {
    [System.Serializable] public class Line {
        public string speaker;
        [TextArea(2,5)] public string text;
        public Sprite portrait;
        public Choice[] choices; // optional; if empty, auto-continue
    }
    [System.Serializable] public class Choice {
        public string label;
        public int gotoIndex = -1;  // -1 = next
        public string command; // optional
    }
    public Line[] lines;
}
