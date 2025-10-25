using UnityEngine;

public class StarfieldScroller : MonoBehaviour
{
    [SerializeField] float scrollSpeed = 0.2f;
    [SerializeField] Renderer targetRenderer;
    [SerializeField] Vector2 direction = new(0f, -1f);

    void Reset() { targetRenderer = GetComponent<Renderer>(); }
    void Update()
    {
        if (!targetRenderer) return;
        var o = targetRenderer.material.mainTextureOffset;
        o += direction.normalized * scrollSpeed * Time.deltaTime;
        targetRenderer.material.mainTextureOffset = o;
    }
}
