using UnityEngine;

// A planet. It pulls the player and the bullets: the closer, the stronger (up to a limit).
// The edge of its gravity field is drawn as a dotted circle. Touching the planet is fatal.
// Flying past it bends the path, so the player can turn around it without shooting.
public class Planet : MonoBehaviour
{
    public static Planet Create(Vector2 position, Transform parent)
    {
        var go = new GameObject("Planet");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        float size = Tuning.PlanetSize;                       // side of the picture
        float bodyRadius = size * Tuning.PlanetBodyShare;     // the solid ball inside it

        // Picture "planet1" (the ball takes 0.44 of it) or a placeholder ball with a glow.
        if (Art.TryMake("planet", go.transform, size, 3) == null)
        {
            SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
                new Color(1f, 0.7f, 0.4f, 0.5f), Vector2.one * bodyRadius * 3f, 2);
            SpriteFactory.MakeSprite("Ball", go.transform, SpriteFactory.Circle(),
                new Color(0.8f, 0.5f, 0.3f), Vector2.one * bodyRadius * 2f, 3);
        }

        // Dotted circle: the edge of the gravity field.
        var edge = new GameObject("FieldEdge").transform;
        edge.SetParent(go.transform, false);
        const int dots = 90;
        for (int i = 0; i < dots; i++)
        {
            float a = Mathf.PI * 2f * i / dots;
            var dot = SpriteFactory.MakeSprite("Dot", edge, SpriteFactory.Circle(),
                new Color(1f, 0.8f, 0.55f, 0.4f), Vector2.one * 0.22f, -40);
            dot.transform.localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Tuning.GravityRadius;
        }

        // Solid body: rocks bounce off it, bullets vanish on it, the player dies on it.
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = bodyRadius;

        return go.AddComponent<Planet>();
    }

    void FixedUpdate()
    {
        Vector2 center = transform.position;
        foreach (var hit in Physics2D.OverlapCircleAll(center, Tuning.GravityRadius))
        {
            var body = hit.attachedRigidbody;
            if (body == null) continue;
            // Only the player and the bullets feel the pull.
            if (hit.GetComponent<PlayerController>() == null && hit.GetComponent<Bullet>() == null) continue;

            Vector2 to = center - body.position;
            float dist = Mathf.Max(to.magnitude, 0.5f);
            float accel = Mathf.Min(Tuning.GravityStrength / (dist * dist), Tuning.GravityMaxAccel);
            body.AddForce(to / dist * accel * body.mass);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<PlayerController>() == null) return;
        if (DraftGame.Instance != null) DraftGame.Instance.Lose("CRASHED INTO THE PLANET");
    }
}
