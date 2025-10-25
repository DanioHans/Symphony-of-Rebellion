using UnityEngine;
using System;

public class StoryboardService : MonoBehaviour {
    public static StoryboardService I { get; private set; }

    [SerializeField] private StoryboardPlayer player;

    void Awake() {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    public void Play(StoryboardAsset asset, Action onComplete = null) {
        if (!player) { Debug.LogWarning("StoryboardService: no StoryboardPlayer assigned"); onComplete?.Invoke(); return; }
        player.Play(asset, () => onComplete?.Invoke());
    }
}
