using UnityEngine;
using UnityEngine.UI;

public class BackgroundScroller : MonoBehaviour {
    public RawImage bg;
    public float scrollSpeed = 0.1f;
    public MinigameController mc;

    void Update() {
        if (!mc.playing) return;
        Rect uv = bg.uvRect;
        uv.y += scrollSpeed * Time.deltaTime;
        bg.uvRect = uv;
    }
}
