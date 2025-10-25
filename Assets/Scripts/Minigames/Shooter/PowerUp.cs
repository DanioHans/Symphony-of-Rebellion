using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum Kind { Spread, Rapid, Shield, Health }

    [Header("Config")]
    [SerializeField] Kind kind = Kind.Spread;
    [SerializeField] bool randomizeOnDrop = true;
    [SerializeField] float fallSpeed = 2.5f;

    Vector2 minB, maxB;

    public void Drop(Vector2 pos, Vector2 boundsMin, Vector2 boundsMax)
    {
        transform.position = pos;
        minB = boundsMin;
        maxB = boundsMax;

        if (randomizeOnDrop)
        {
            int values = System.Enum.GetValues(typeof(Kind)).Length;
            kind = (Kind)Random.Range(0, values);
        }

        gameObject.SetActive(true);
    }

    void Update()
    {
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        if (transform.position.y < minB.y - 1f)
            Projectile.Despawn(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Pickup by player
        if (other.TryGetComponent<PlayerShip>(out var player))
        {
            Debug.Log($"Player picked up power-up: {kind}");
            player.CollectPowerUp(kind);
            Projectile.Despawn(gameObject);
            return;
        }
    }

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb) { rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0; }
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }
}
