using UnityEngine;

public class PlayerShip : MonoBehaviour
{
    [SerializeField] float moveSpeed = 9f;
    [SerializeField] float baseFireCooldown = 0.2f;
    [SerializeField] int maxBombs = 2;

    // NEW: i-frames
    [Header("Invincibility")]
    [SerializeField] float invincibleSeconds = 1.5f;
    [SerializeField] float flickerHz = 12f;
    bool invincible;
    float invTimer;
    SpriteRenderer[] rends;

    SpaceShooterBase ctrl;
    Projectile projectilePrefab;
    Vector2 minB, maxB;
    float cd;
    bool alive = true;

    // upgrades
    int spreadLevel = 0;    // 0..2 → 1/3/5 bullets
    float rapidMult = 1f;   // 1..2
    int shieldHP = 0;       // 0..3

    public void Init(SpaceShooterBase controller, Projectile proj, Vector2 boundsMin, Vector2 boundsMax)
    {
        ctrl = controller; projectilePrefab = proj; minB = boundsMin; maxB = boundsMax;
    }

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb) { rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0; }
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        rends = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
    }

    void Update()
    {
        if (!alive) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 d = new Vector3(h, v, 0f).normalized;
        transform.position += d * moveSpeed * Time.deltaTime;

        var p = transform.position;
        p.x = Mathf.Clamp(p.x, minB.x, maxB.x);
        p.y = Mathf.Clamp(p.y, minB.y, maxB.y);
        transform.position = p;

        cd -= Time.deltaTime;
        float currentCD = baseFireCooldown * (1f / Mathf.Max(0.01f, rapidMult));
        if (Input.GetKey(KeyCode.Space) && cd <= 0f) { cd = currentCD; Fire(); }

        if (invincible)
        {
            invTimer -= Time.deltaTime;
            float t = Mathf.PingPong(Time.time * flickerHz, 1f);
            bool on = t > 0.5f;
            SetRenderersAlpha(on ? 1f : 0.25f);

            if (invTimer <= 0f)
            {
                invincible = false;
                SetRenderersAlpha(1f);
            }
        }
    }

    void Fire()
    {
        int bullets = spreadLevel switch { 0 => 1, 1 => 3, _ => 5 };
        float spread = spreadLevel switch { 0 => 0f, 1 => 12f, _ => 20f };
        int mid = bullets / 2;

        for (int i = 0; i < bullets; i++)
        {
            float off = (bullets == 1) ? 0f : Mathf.Lerp(-spread * 0.5f, spread * 0.5f, i / (float)(bullets - 1));
            var proj = SimplePool.Spawn(projectilePrefab, transform.position + Vector3.up * 0.5f, Quaternion.Euler(0, 0, off));
            Vector2 dir = Quaternion.Euler(0, 0, off) * Vector2.up;
            proj.Setup(Projectile.Team.Player, dir, 18f, minB, maxB);
        }
    }


    public void Respawn(Vector3 pos)
    {
        alive = true;
        gameObject.SetActive(true);

        BeginInvincibility(invincibleSeconds);
    }

    public void CollectPowerUp(PowerUp.Kind kind)
    {
        switch (kind)
        {
            case PowerUp.Kind.Spread: spreadLevel = Mathf.Clamp(spreadLevel + 1, 0, 2); break;
            case PowerUp.Kind.Rapid:  rapidMult  = Mathf.Clamp(rapidMult + 0.25f, 1f, 2f); break;
            case PowerUp.Kind.Shield: shieldHP   = Mathf.Clamp(shieldHP + 1, 0, 3); break;
            case PowerUp.Kind.Health: break;
        }
        ctrl?.OnPickup(kind);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!alive) return;

        if (invincible) return;

        if (other.TryGetComponent<Projectile>(out var proj) && proj.TeamId == Projectile.Team.Enemy)
        {
            Projectile.Despawn(other.gameObject);
            TakeHit();
            return;
        }
        if (other.GetComponent<EnemyUnit>() || other.GetComponent<Asteroid>())
        {
            TakeHit();
            return;
        }
    }

    void TakeHit()
    {
        if (shieldHP > 0) { shieldHP--; ctrl?.OnShieldHit(shieldHP); return; }
        alive = false;
        gameObject.SetActive(false);
        ctrl?.PlayerHit();
    }

    public void BeginInvincibility(float seconds)
    {
        invincible = true;
        invTimer = seconds;
        SetRenderersAlpha(0.25f);
    }

    void SetRenderersAlpha(float a)
    {
        if (rends == null) return;
        foreach (var r in rends)
        {
            if (!r) continue;
            var c = r.color; c.a = a; r.color = c;
        }
    }
}
