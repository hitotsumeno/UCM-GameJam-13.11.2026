using UnityEngine;

public enum AsteroidSize { Small, Medium, Large }

// Drifts slowly, bounces off other asteroids and the player. Bullets break it.
public class Asteroid : MonoBehaviour
{
    static readonly Color DamagedColor = new Color(1f, 0.45f, 0.35f);

    // Level area. Confined asteroids turn back at its edges, so they never fly away.
    public static Rect Bounds;

    AsteroidSize size;
    int hp, maxHp;
    SpriteRenderer body;
    Color baseColor;
    Rigidbody2D rb;
    bool confined;

    public static float Diameter(AsteroidSize size)
    {
        switch (size)
        {
            case AsteroidSize.Small: return Tuning.AsteroidSmallSize;
            case AsteroidSize.Medium: return Tuning.AsteroidMediumSize;
            default: return Tuning.AsteroidLargeSize;
        }
    }

    static int HitPoints(AsteroidSize size)
    {
        switch (size)
        {
            case AsteroidSize.Small: return Tuning.AsteroidHpSmall;
            case AsteroidSize.Medium: return Tuning.AsteroidHpMedium;
            default: return Tuning.AsteroidHpLarge;
        }
    }

    static string ArtName(AsteroidSize size)
    {
        switch (size)
        {
            case AsteroidSize.Small: return "asteroid_s";
            case AsteroidSize.Medium: return "asteroid_m";
            default: return "asteroid_l";
        }
    }

    public static Asteroid Create(AsteroidSize size, Vector2 position, Vector2 velocity, Transform parent,
        bool confined = true)
    {
        float d = Diameter(size);
        var go = new GameObject("Asteroid_" + size);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        // Picture (asteroid_s1, asteroid_m2, ...) or a placeholder: grey circle with two craters.
        // The pictures are rendered so that a circle of 0.45 x picture size has the same area
        // as the rock (see Assets/Filip/Tools~), so this collider fits every one of them.
        float radius = d * 0.45f;
        var body = Art.TryMake(ArtName(size), go.transform, d, 3);
        if (body == null)
        {
            radius = d * 0.5f;
            float shade = Random.Range(0.42f, 0.55f);
            body = SpriteFactory.MakeSprite("Body", go.transform, SpriteFactory.Circle(),
                new Color(shade, shade * 0.95f, shade * 0.9f), Vector2.one * d, 3);
            var crater1 = SpriteFactory.MakeSprite("Crater", go.transform, SpriteFactory.Circle(),
                new Color(0f, 0f, 0f, 0.25f), Vector2.one * d * 0.3f, 4);
            crater1.transform.localPosition = new Vector3(d * 0.18f, d * 0.12f, 0f);
            var crater2 = SpriteFactory.MakeSprite("Crater", go.transform, SpriteFactory.Circle(),
                new Color(0f, 0f, 0f, 0.25f), Vector2.one * d * 0.2f, 4);
            crater2.transform.localPosition = new Vector3(-d * 0.2f, -d * 0.15f, 0f);
        }

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.mass = Tuning.AsteroidDensity * d * d;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.linearVelocity = velocity;
        rb.angularVelocity = Random.Range(-Tuning.AsteroidSpin, Tuning.AsteroidSpin);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = radius;
        col.sharedMaterial = new PhysicsMaterial2D("Asteroid") { friction = 0f, bounciness = Tuning.Bounciness };

        var a = go.AddComponent<Asteroid>();
        a.size = size;
        a.maxHp = HitPoints(size);
        a.hp = a.maxHp;
        a.body = body;
        a.baseColor = body.color;
        a.rb = rb;
        a.confined = confined;
        return a;
    }

    public void SetTint(Color tint)
    {
        baseColor *= tint;
        body.color = baseColor;
    }

    void FixedUpdate()
    {
        if (!confined) return;
        Vector2 p = rb.position;
        Vector2 v = rb.linearVelocity;
        bool turned = false;
        if ((p.x < Bounds.xMin && v.x < 0f) || (p.x > Bounds.xMax && v.x > 0f)) { v.x = -v.x; turned = true; }
        if ((p.y < Bounds.yMin && v.y < 0f) || (p.y > Bounds.yMax && v.y > 0f)) { v.y = -v.y; turned = true; }
        if (turned) rb.linearVelocity = v;
    }

    // Called by a bullet.
    public void Hit()
    {
        if (hp <= 0) return;
        hp--;
        if (hp > 0)
        {
            // Gets redder with every hit, so damage is visible.
            body.color = Color.Lerp(DamagedColor, baseColor, (float)hp / maxHp);
            return;
        }

        if (size == AsteroidSize.Large)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir == Vector2.zero) dir = Vector2.right;
            Vector2 pos = transform.position;
            float offset = Diameter(AsteroidSize.Medium) * 0.5f;
            Vector2 push = dir * Tuning.AsteroidSplitSpeed;
            Create(AsteroidSize.Medium, pos + dir * offset, rb.linearVelocity + push, transform.parent, confined);
            Create(AsteroidSize.Medium, pos - dir * offset, rb.linearVelocity - push, transform.parent, confined);
        }
        Destroy(gameObject);
    }
}
