using UnityEngine;

public class Bullet : MonoBehaviour
{
    public static Bullet Spawn(Vector2 position, Vector2 direction, Collider2D ignore, Transform parent)
    {
        var go = new GameObject("Bullet");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
            new Color(1f, 0.8f, 0.25f, Tuning.BulletGlowOpacity), Vector2.one * Tuning.BulletGlowSize, 19);
        SpriteFactory.MakeSprite("Visual", go.transform, SpriteFactory.Circle(),
            new Color(1f, 0.95f, 0.6f), Vector2.one * Tuning.BulletDiameter, 20);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.mass = Tuning.BulletMass;
        rb.linearDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.linearVelocity = direction.normalized * Tuning.BulletSpeed;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = Tuning.BulletDiameter * 0.5f;
        if (ignore != null) Physics2D.IgnoreCollision(col, ignore);

        var b = go.AddComponent<Bullet>();
        Destroy(go, Tuning.BulletLifetime);
        return b;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        var asteroid = collision.collider.GetComponent<Asteroid>();
        if (asteroid != null) asteroid.Hit();
        var mine = collision.collider.GetComponent<Mine>();
        if (mine != null) mine.Shot();
        Destroy(gameObject);
    }
}
