using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StealthRunner : MonoBehaviour {
    public RectTransform player;
    public RectTransform[] lanes;     // size 3
    public RectTransform spawnRoot;   // top Y
    public GameObject guardPrefab;    // a UI Image with collider (or just Rect)

    public float laneSwitchSpeed = 12f;
    public float guardSpeed = 150f;   // px/sec downward
    public float spawnEvery = 0.9f;
    public float durationToWin = 20f;

    int lane = 1;
    float tSpawn, tElapsed;
    List<RectTransform> guards = new();

    MinigameController mc;

    void Start() {
        mc = FindObjectOfType<MinigameController>();
        SnapPlayer();
    }

    void Update() {
        if (!mc.playing) return;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) lane = Mathf.Max(0, lane-1);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) lane = Mathf.Min(2, lane+1);

        // smooth move to lane
        player.anchoredPosition = Vector2.Lerp(player.anchoredPosition, LanePos(lane), Time.deltaTime * laneSwitchSpeed);

        // spawn guards
        tSpawn += Time.deltaTime;
        if (tSpawn >= spawnEvery) { tSpawn = 0; SpawnGuard(Random.Range(0,3)); }

        // move guards
        for (int i = guards.Count-1; i >= 0; i--) {
            var g = guards[i];
            g.anchoredPosition += Vector2.down * guardSpeed * Time.deltaTime;
            if (g.anchoredPosition.y < -700) { Destroy(g.gameObject); guards.RemoveAt(i); }
            else if (Collides(player, g)) { mc.OnLose(); enabled = false; }
        }

        // win timer
        tElapsed += Time.deltaTime;
        if (tElapsed >= durationToWin) {
            GameState.I.SetFlag("carceris_completed");
            mc.OnWin(); 
            enabled = false;
        }
    }

    Vector2 LanePos(int i) => new Vector2(lanes[i].anchoredPosition.x, player.anchoredPosition.y);

    void SnapPlayer() { player.anchoredPosition = LanePos(lane); }

    void SpawnGuard(int laneIndex) {
        var go = Instantiate(guardPrefab, spawnRoot.parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f,0.5f);
        rt.sizeDelta = new Vector2(100, 140); // tweak
        rt.anchoredPosition = new Vector2(lanes[laneIndex].anchoredPosition.x, spawnRoot.anchoredPosition.y);
        guards.Add(rt);
    }

    bool Collides(RectTransform a, RectTransform b) {
        var ra = RectWorld(a); var rb = RectWorld(b);
        return ra.Overlaps(rb);
    }
    Rect RectWorld(RectTransform rt) {
        Vector3[] w = new Vector3[4]; rt.GetWorldCorners(w);
        return new Rect(w[0], w[2] - w[0]);
    }
}
