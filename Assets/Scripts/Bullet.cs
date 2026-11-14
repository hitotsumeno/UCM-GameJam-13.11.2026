using UnityEngine;

// Set up in Assets/Prefabs/Bullet.prefab.
public class Bullet : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [SerializeField] Collider2D col;

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public static Bullet Spawn(Bullet prefab, Vector2 position, Vector2 direction, Collider2D ignore, Transform parent)
    {
        if (prefab == null)
        {
            Debug.LogError("Bullet.Spawn: no bullet prefab assigned.");
            return null;
        }
        var b = Instantiate(prefab, position, Quaternion.identity, parent);
        b.rb.linearVelocity = direction.normalized * Tuning.BulletSpeed;
        if (ignore != null) Physics2D.IgnoreCollision(b.col, ignore);
        Destroy(b.gameObject, Tuning.BulletLifetime);
        return b;
    }

    // Uses the bullet prefab assigned on DraftGame.
    public static Bullet Spawn(Vector2 position, Vector2 direction, Collider2D ignore, Transform parent)
    {
        var game = DraftGame.Instance;
        return Spawn(game != null ? game.BulletPrefab : null, position, direction, ignore, parent);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        var asteroid = collision.collider.GetComponent<Asteroid>();
        if (asteroid != null) asteroid.Hit();
        Destroy(gameObject);
    }
}
