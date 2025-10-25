using UnityEngine;

public class EnemyPathMover : MonoBehaviour
{
    public enum PathType { Straight, Sine, ZigZag }

    [SerializeField] PathType path = PathType.Straight;
    [SerializeField] float speed = 3f;
    [SerializeField] float sineAmp = 1.2f, sineFreq = 2.5f;
    [SerializeField] float zigWidth = 1.5f, zigPeriod = 1.0f;

    Vector3 startPos;
    float t;

    public void Setup(PathType p, float baseSpeed, float amp = 1.2f, float freq = 2.5f)
    {
        path = p; speed = baseSpeed; sineAmp = amp; sineFreq = freq; zigWidth = amp; zigPeriod = 1f / Mathf.Max(0.1f, freq);
        startPos = transform.position; t = 0f;
    }

    void Start() { startPos = transform.position; }
    void Update()
    {
        t += Time.deltaTime;
        Vector3 pos = transform.position;
        pos += Vector3.down * speed * Time.deltaTime;

        switch (path)
        {
            case PathType.Straight: break;
            case PathType.Sine:
                pos.x = startPos.x + Mathf.Sin(t * sineFreq) * sineAmp;
                break;
            case PathType.ZigZag:
                float phase = Mathf.PingPong(t / zigPeriod, 1f) * 2f - 1f; // -1..1
                pos.x = startPos.x + phase * zigWidth;
                break;
        }
        transform.position = pos;

        if (pos.y < -10f) Destroy(gameObject);
    }
}
