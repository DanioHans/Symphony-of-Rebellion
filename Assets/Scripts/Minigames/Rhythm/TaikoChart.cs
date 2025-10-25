using System;
using System.Collections.Generic;
using UnityEngine;

public enum TaikoLane { Left, Right }

[CreateAssetMenu(fileName = "NewTaikoChart", menuName = "Minigames/Taiko Chart")]
public class TaikoChart : ScriptableObject {
    [Header("Audio")]
    public AudioClip music;
    [Tooltip("Song BPM used for beat->time conversion")]
    public float bpm = 120f;
    [Tooltip("Seconds to delay music start so notes align to hit-line (positive = music later)")]
    public float songOffsetSec = 0.0f;

    [Header("Gameplay")]
    [Tooltip("How many beats early a note appears before its hit time")]
    public float approachBeats = 2.0f;
    [Tooltip("How many misses allowed before immediate fail (<=0 = never auto-fail)")]
    public int allowedMisses = 10;
    [Tooltip("Score needed to win (0 = always win on song end)")]
    public int winScore = 30000;

    [Serializable] public struct Note {
        [Tooltip("On which beat this note should be hit (0 = song start)")]
        public float beat;
        public TaikoLane lane;
    }

    [Header("Notes (sorted by beat)")]
    public List<Note> notes = new();
}
