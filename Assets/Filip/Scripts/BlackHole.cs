using System.Collections.Generic;
using UnityEngine;

// A black hole. It pulls the player, the bullets and the oxygen crystals; whatever
// reaches its dark middle is gone. Big holes stand still with rocks circling them,
// small ones can drift back and forth, so the safe path keeps changing.
public class BlackHole : MonoBehaviour
{
    // The dark disc is this share of the picture's side (see Tools~/BackdropGen.cs).
    const float DiscShare = 0.16f;

    // All holes of the current level; oxygen crystals ask them for their pull.
    public static readonly List<BlackHole> All = new List<BlackHole>();

    float bodyRadius;    // the dark middle: touching it is fatal
    float fieldRadius;   // the pull reaches this far (dotted circle)
    float strength;
    Vector2 home, axis;  // drifting holes glide along axis around home
    bool drifts;
    float phase;
    Transform picture;

    public static void ResetStatics()
    {
        All.Clear();
    }

    // big: a large still hole with rocks circling it.
    public static BlackHole Create(Vector2 position, Transform parent, bool big)
    {
        var hole = Build(position, parent,
            big ? Tuning.HoleBigRadius : Tuning.HoleSmallRadius,
            big ? Tuning.HoleBigField : Tuning.HoleSmallField,
            big ? Tuning.HoleBigStrength : Tuning.HoleSmallStrength);

        if (big)
        {
            // Rocks on two circles, turning in opposite directions.
            for (int i = 0; i < Tuning.HoleOrbitRocks; i++)
            {
                bool outer = i % 2 == 1;
                float radius = hole.bodyRadius + (outer ? 8.5f : 5f);
                float angle = Mathf.PI * 2f * i / Tuning.HoleOrbitRocks + Random.Range(-0.3f, 0.3f);
                Vector2 pos = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                var rock = Asteroid.Create(outer ? AsteroidSize.Medium : AsteroidSize.Small, pos, Vector2.zero, parent, false);
                Orbiter.Attach(rock.gameObject, hole.transform, radius, angle, outer ? -1f : 1f);
            }
        }
        return hole;
    }

    // A small hole that glides back and forth along `axis`.
    public static BlackHole CreateDrifting(Vector2 position, Vector2 axis, Transform parent)
    {
        var hole = Build(position, parent, Tuning.HoleSmallRadius, Tuning.HoleSmallField, Tuning.HoleSmallStrength);
        hole.drifts = true;
        hole.home = position;
        hole.axis = axis.normalized;
        hole.phase = Random.Range(0f, Mathf.PI * 2f);
        return hole;
    }

    static BlackHole Build(Vector2 position, Transform parent, float bodyRadius, float fieldRadius, float strength)
    {
        var go = new GameObject("BlackHole");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var hole = go.AddComponent<BlackHole>();
        hole.bodyRadius = bodyRadius;
        hole.fieldRadius = fieldRadius;
        hole.strength = strength;

        // Picture "blackhole" (dark disc with a glowing ring) or a placeholder.
        var art = Art.TryMake("blackhole", go.transform, bodyRadius / DiscShare, 3);
        if (art != null) hole.picture = art.transform;
        else
        {
            SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
                new Color(1f, 0.6f, 0.25f, 0.6f), Vector2.one * bodyRadius * 5f, 2);
            SpriteFactory.MakeSprite("Ring", go.transform, SpriteFactory.Ring(),
                new Color(1f, 0.8f, 0.5f), Vector2.one * bodyRadius * 2.8f, 3);
            SpriteFactory.MakeSprite("Disc", go.transform, SpriteFactory.Circle(),
                Color.black, Vector2.one * bodyRadius * 2f, 4);
        }

        // Dotted circle: the edge of the pull.
        var edge = new GameObject("FieldEdge").transform;
        edge.SetParent(go.transform, false);
        int dots = Mathf.RoundToInt(fieldRadius * 4f);
        for (int i = 0; i < dots; i++)
        {
            float a = Mathf.PI * 2f * i / dots;
            var dot = SpriteFactory.MakeSprite("Dot", edge, SpriteFactory.Circle(),
                new Color(0.75f, 0.6f, 1f, 0.35f), Vector2.one * 0.2f, -40);
            dot.transform.localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * fieldRadius;
        }
        return hole;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Update()
    {
        if (picture != null) picture.Rotate(0f, 0f, -Tuning.HoleSpinSpeed * Time.deltaTime);
    }

    // Acceleration this hole gives to a thing at `position`. Zero outside its field.
    public Vector2 PullAt(Vector2 position)
    {
        Vector2 to = (Vector2)transform.position - position;
        float dist = to.magnitude;
        if (dist > fieldRadius || dist < 0.01f) return Vector2.zero;
        float d = Mathf.Max(dist, bodyRadius * 0.5f);
        return to / dist * Mathf.Min(strength / (d * d), Tuning.HoleMaxAccel);
    }

    // True if the point is inside the dark middle.
    public bool Swallows(Vector2 position, float margin)
    {
        return Vector2.Distance(transform.position, position) < bodyRadius + margin;
    }

    void FixedUpdate()
    {
        if (drifts)
        {
            float t = Time.time * Tuning.HoleDriftSpeed / Tuning.HoleDriftRange + phase;
            transform.position = home + axis * Mathf.Sin(t) * Tuning.HoleDriftRange;
        }

        Vector2 center = transform.position;
        foreach (var hit in Physics2D.OverlapCircleAll(center, fieldRadius))
        {
            var body = hit.attachedRigidbody;
            if (body == null) continue;
            var player = hit.GetComponent<PlayerController>();
            bool bullet = hit.GetComponent<Bullet>() != null;
            if (player == null && !bullet) continue; // rocks and mines do not feel the pull

            if (Swallows(body.position, player != null ? Tuning.PlayerDiameter * 0.2f : 0f))
            {
                if (player != null)
                {
                    if (DraftGame.Instance != null) DraftGame.Instance.Lose("SWALLOWED BY A BLACK HOLE");
                }
                else Destroy(hit.gameObject);
                continue;
            }
            body.AddForce(PullAt(body.position) * body.mass);
        }
    }
}
