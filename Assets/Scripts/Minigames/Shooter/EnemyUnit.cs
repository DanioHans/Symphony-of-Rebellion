using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    [SerializeField] float bulletSpeed = 8f;

    GalaxyAttackController ctrl;
    Projectile projectilePrefab;
    Vector2 bmin, bmax;

    public void Init(GalaxyAttackController c, Projectile proj, Vector2 boundsMin, Vector2 boundsMax)
    {
        ctrl = c; projectilePrefab = proj; bmin = boundsMin; bmax = boundsMax;
    }

    void Awake()
    {
        // ensure trigger-style collisions (no physics spin)
        var rb = GetComponent<Rigidbody2D>();
        if (rb) { rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0; }
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void Update()
    {
        // 👇 Leak detection: if we’re below the playfield, count as damage
        if (transform.position.y < bmin.y - 0.1f && ctrl != null)
        {
            ctrl.OnEnemyLeaked(this);
            SilentKill();
            return;
        }
    }

    public void Fire()
    {
        if (!ctrl || !projectilePrefab) return;
        var proj = SimplePool.Spawn(projectilePrefab, transform.position + Vector3.down * 0.35f, Quaternion.identity);
        proj.Setup(Projectile.Team.Enemy, Vector2.down, bulletSpeed, bmin, bmax);
    }

    public void SilentKill()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Projectile>(out var proj) && proj.TeamId == Projectile.Team.Player)
        {
            Projectile.Despawn(other.gameObject);
            ctrl.OnEnemyKilled(this); // only on kill (not on leak)
            SilentKill();
        }
        if (other.GetComponent<PlayerShip>())
        {
            ctrl.OnEnemyKilled(this);
            SilentKill();
        }
    }
}
