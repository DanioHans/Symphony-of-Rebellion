using UnityEngine;

public class Asteroid : MonoBehaviour
{
    [SerializeField] float minSpeed = 2f, maxSpeed = 5f, maxSpin = 180f;
    Vector2 dir; float spd; float spin; Vector2 minB, maxB;

    public void Launch(Vector2 boundsMin, Vector2 boundsMax)
    {
        minB = boundsMin; maxB = boundsMax;
        spd = Random.Range(minSpeed, maxSpeed);
        dir = new Vector2(Random.Range(-0.25f, 0.25f), -1f).normalized;
        spin = Random.Range(-maxSpin, maxSpin);
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
    }

    void Update()
    {
        transform.position += (Vector3)(dir * spd * Time.deltaTime);
        transform.Rotate(0, 0, spin * Time.deltaTime);

        if (transform.position.y < minB.y - 1.5f)
            Projectile.Despawn(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Projectile>(out var proj) && proj.TeamId == Projectile.Team.Player)
        {
            Projectile.Despawn(other.gameObject);
            Projectile.Despawn(gameObject);
        }
        if (other.GetComponent<PlayerShip>())
        {
            Projectile.Despawn(gameObject);
        }
    }
}
