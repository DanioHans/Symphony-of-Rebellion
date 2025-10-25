using System.Collections.Generic;
using UnityEngine;

public class SimplePool : MonoBehaviour
{
    static readonly Dictionary<GameObject, Queue<GameObject>> pools = new();

    public static T Spawn<T>(T prefab, Vector3 pos, Quaternion rot) where T : Component
    {
        var go = Spawn(prefab.gameObject, pos, rot);
        return go.GetComponent<T>();
    }

    public static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!pools.TryGetValue(prefab, out var q)) { q = new Queue<GameObject>(); pools[prefab] = q; }
        GameObject obj;
        if (q.Count == 0)
        {
            obj = Object.Instantiate(prefab, pos, rot);
            var pm = obj.GetComponent<PoolMember>() ?? obj.AddComponent<PoolMember>();
            pm.PrefabRef = prefab;
        }
        else
        {
            obj = q.Dequeue();
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.SetActive(true);
        }
        return obj;
    }

    public static void Despawn(GameObject obj, GameObject prefab)
    {
        if (!obj) return;
        obj.SetActive(false);
        if (!pools.TryGetValue(prefab, out var q)) { q = new Queue<GameObject>(); pools[prefab] = q; }
        q.Enqueue(obj);
    }
}

public class PoolMember : MonoBehaviour
{
    public GameObject PrefabRef;
}
