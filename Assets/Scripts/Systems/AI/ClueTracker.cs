using System.Collections.Generic;
using UnityEngine;

public class ClueTracker : MonoBehaviour {
  public static ClueTracker I { get; private set; }
  readonly HashSet<string> discovered = new();

  void Awake() { if (I && I != this) { Destroy(gameObject); return; } I = this; DontDestroyOnLoad(gameObject); }
  public void MarkDiscovered(string keyword){ if (!string.IsNullOrWhiteSpace(keyword)) discovered.Add(keyword.ToLowerInvariant()); }
  public IReadOnlyCollection<string> GetKeywords() => discovered;
}
