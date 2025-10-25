using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public enum Team { Player, Enemy }
    public Team TeamId { get; private set; }

    public static readonly HashSet<Projectile> Active = new();

    Vector2 dir;
    float speed;
    Vector2 minB, maxB;
    bool boundsSet;

    Rigidbody2D rb; Collider2D col; PoolMember poolMember;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        poolMember = GetComponent<PoolMember>();

        if (rb) { rb.gravityScale = 0f; rb.isKinematic = true; }
        if (col) col.isTrigger = true;
    }

    public void Setup(Team t, Vector2 direction, float spd, Vector2 boundsMin, Vector2 boundsMax)
    {
        TeamId = t; dir = direction.normalized; speed = spd; minB = boundsMin; maxB = boundsMax; boundsSet = true;
        Active.Add(this);
    }

    void OnEnable()
    {
        // if revived from pool
        Active.Add(this);
    }

    void OnDisable()
    {
        Active.Remove(this);
    }

    void Update()
    {
        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        if (boundsSet)
        {
            var p = transform.position;
            if (p.x < minB.x - 1f || p.x > maxB.x + 1f || p.y < minB.y - 1f || p.y > maxB.y + 1f)
                Despawn(gameObject);
        }
    }

    public static void Despawn(GameObject obj)
    {
        if (!obj) return;
        var pm = obj.GetComponent<PoolMember>();
        if (pm && pm.PrefabRef) SimplePool.Despawn(obj, pm.PrefabRef);
        else obj.SetActive(false);
    }
}
