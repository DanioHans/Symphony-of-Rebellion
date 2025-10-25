using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName="Game/StoryboardRegistry")]
public class StoryboardRegistry : ScriptableObject {
    [System.Serializable]
    public struct Entry {
        public string id;
        public StoryboardAsset asset;
        public string postEffectId;
    }

    public Entry[] entries;

    private Dictionary<string, Entry> _map;

    void OnEnable() { Build(); }

    Dictionary<string, Entry> Build() {
        _map = new Dictionary<string, Entry>();
        if (entries == null) return _map;
        foreach (var e in entries) {
            if (!string.IsNullOrEmpty(e.id) && e.asset)
                _map[e.id] = e;
        }
        return _map;
    }

    public bool TryGet(string id, out Entry entry)
    {
        if (_map == null) Build();

        if (_map != null && _map.TryGetValue(id, out entry))
            return true;

        entry = default;
        return false;
    }

}
