using UnityEngine;

public enum MineMode { Hunter, Wall, Patrol, Orbit }

// A mine. Four behaviours, so a minefield is never just targets standing still:
//  - Hunter (red): sleeps until the player is close, then swims toward them.
//  - Patrol (yellow): glides back and forth along a line; the gap between patrols keeps moving.
//  - Orbit (yellow): circles around a point (usually an oxygen tank) together with others.
//  - Wall (yellow): holds its place in a dense row; one shot sets the whole row off.
// Shot by the player: blows up, leaves crystals, counts for the combo.
// Touching the player or exploding next to them: breaches the hull and throws them away.
// A blast pushes everything around, sets off other mines nearby and cracks rocks.
public class Mine : MonoBehaviour
{
    static readonly Color Red = new Color(1f, 0.25f, 0.2f);
    static readonly Color Yellow = new Color(1f, 0.8f, 0.2f);

    MineMode mode;
    Rigidbody2D rb;
    SpriteRenderer lamp, glow;
    Color color;
    bool awake;
    bool exploded;

    Vector2 home;          // Wall: its place. Patrol: middle of its line. Orbit: center of the circle.
    Vector2 axis;          // Patrol: direction of the line
    float phase;           // Patrol: where on the line it starts. Orbit: starting angle.
    float orbitRadius;

    public static Mine CreateHunter(Vector2 position, Transform parent)
    {
        return Create(position, parent, MineMode.Hunter);
    }

    public static Mine CreateWall(Vector2 position, Transform parent)
    {
        var m = Create(position, parent, MineMode.Wall);
        m.home = position;
        return m;
    }

    // Glides along `axis` around its starting place.
    public static Mine CreatePatrol(Vector2 position, Vector2 axis, Transform parent)
    {
        var m = Create(position, parent, MineMode.Patrol);
        m.home = position;
        m.axis = axis.normalized;
        m.phase = Random.Range(0f, Mathf.PI * 2f);
        return m;
    }

    // A ring of mines circling a point.
    public static void CreateRing(Vector2 center, Transform parent)
    {
        float turn = Random.value < 0.5f ? 1f : -1f;
        for (int i = 0; i < Tuning.MineRingCount; i++)
        {
            float angle = Mathf.PI * 2f * i / Tuning.MineRingCount;
            Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Tuning.MineRingRadius;
            var m = Create(pos, parent, MineMode.Orbit);
            m.home = center;
            m.phase = angle;
            m.orbitRadius = Tuning.MineRingRadius;
            m.axis = new Vector2(turn, 0f); // x keeps the direction of rotation
        }
    }

    static Mine Create(Vector2 position, Transform parent, MineMode mode)
    {
        var go = new GameObject("Mine_" + mode);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        float s = Tuning.MineSize;
        Color color = mode == MineMode.Hunter ? Red : Yellow;

        // Glow in the mine's colour: everything dangerous glows.
        var glow = SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
            new Color(color.r, color.g, color.b, 0.45f), Vector2.one * s * 2.6f, 2);

        // Picture "mine" or a placeholder: a dark ball with spikes.
        if (Art.TryMake("mine", go.transform, s, 4) == null)
        {
            for (int i = 0; i < 4; i++)
            {
                var spike = SpriteFactory.MakeSprite("Spike", go.transform, SpriteFactory.Square(),
                    new Color(0.2f, 0.21f, 0.26f), new Vector2(s * 1.25f, s * 0.16f), 4);
                spike.transform.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
            }
            SpriteFactory.MakeSprite("Body", go.transform, SpriteFactory.Circle(),
                new Color(0.13f, 0.14f, 0.18f), Vector2.one * s * 0.82f, 4);
        }
        // Blinking lamp in the middle (drawn over the picture too).
        var lamp = SpriteFactory.MakeSprite("Lamp", go.transform, SpriteFactory.Circle(),
            color, Vector2.one * s * 0.3f, 5);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.mass = Tuning.MineMass;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.angularVelocity = Random.Range(-25f, 25f);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = s * 0.45f;
        col.sharedMaterial = new PhysicsMaterial2D("Mine") { friction = 0f, bounciness = 0.3f };

        var m = go.AddComponent<Mine>();
        m.mode = mode;
        m.rb = rb;
        m.lamp = lamp;
        m.glow = glow;
        m.color = color;
        return m;
    }

    void Update()
    {
        // The lamp blinks slowly while a hunter sleeps and fast when it hunts.
        bool fast = mode == MineMode.Hunter && awake;
        float k = 0.5f + 0.5f * Mathf.Sin(Time.time * (fast ? 14f : 3f));
        lamp.color = new Color(color.r, color.g, color.b, 0.35f + 0.65f * k);
        glow.color = new Color(color.r, color.g, color.b, (fast ? 0.35f : 0.2f) + 0.35f * k);
    }

    void FixedUpdate()
    {
        var game = DraftGame.Instance;
        Transform player = game != null && game.State == GameState.Playing ? game.PlayerTransform : null;
        float dist = player != null ? Vector2.Distance(player.position, rb.position) : float.MaxValue;

        // Close enough to touch: go off (does not rely on the physics contact alone).
        if (!exploded && dist < Tuning.MineSize * 0.45f + Tuning.PlayerDiameter * 0.5f + 0.08f)
        {
            Explode(true, false);
            return;
        }

        Vector2 wanted = Vector2.zero;
        float accel = Tuning.MineAccel;
        switch (mode)
        {
            case MineMode.Hunter:
                // Wakes up when the player is close; gives up only when they are far away.
                awake = dist < (awake ? Tuning.MineLoseRadius : Tuning.MineWakeRadius);
                if (awake && dist > 0.01f)
                    wanted = ((Vector2)player.position - rb.position) / dist * Tuning.MineSpeed;
                break;

            case MineMode.Wall:
                wanted = Toward(home);
                break;

            case MineMode.Patrol:
            {
                float t = Time.time * Tuning.MinePatrolSpeed / Tuning.MinePatrolRange + phase;
                wanted = Toward(home + axis * Mathf.Sin(t) * Tuning.MinePatrolRange);
                accel *= 3f;
                break;
            }
            case MineMode.Orbit:
            {
                float angle = phase + axis.x * Time.time * Tuning.MineRingSpeed / orbitRadius;
                wanted = Toward(home + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * orbitRadius);
                accel *= 3f;
                break;
            }
        }
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, wanted, accel * Time.fixedDeltaTime);
    }

    // Velocity that brings the mine to a moving target and keeps it there (also after a blast pushed it away).
    Vector2 Toward(Vector2 target)
    {
        return Vector2.ClampMagnitude((target - rb.position) * 4f, Tuning.MineSpeed * 1.5f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (exploded) return;
        if (collision.collider.GetComponent<PlayerController>() != null)
        {
            Explode(true, false);
            return;
        }
        // A rock or a meteor hitting it hard sets it off too.
        if (collision.collider.GetComponent<Asteroid>() != null
            && collision.relativeVelocity.magnitude > Tuning.MineTriggerSpeed)
            Explode(false, false);
    }

    // Hit by the player's bullet.
    public void Shot()
    {
        if (!exploded) Explode(false, true);
    }

    // Set off by another mine's blast, a moment later.
    void Chain()
    {
        if (!exploded) Explode(false, false);
    }

    void Explode(bool touchedPlayer, bool shotByPlayer)
    {
        exploded = true;
        Vector2 pos = rb.position;
        var game = DraftGame.Instance;

        // A blast right next to the player breaches the hull; farther away it only pushes.
        Transform player = game != null ? game.PlayerTransform : null;
        if (player != null && (touchedPlayer
            || Vector2.Distance(player.position, pos) < Tuning.MineBreachRadius))
            game.BreachHull(pos);

        if (game != null && shotByPlayer)
        {
            int combo = game.RegisterKill();
            float worth = Tuning.CrystalOxygen * (1f + Tuning.ComboBonusPerStep * (combo - 1));
            OxygenCrystal.Burst(pos, Tuning.CrystalsFromMine, worth, transform.parent);
        }
        else if (game != null && !touchedPlayer)
        {
            // Set off by a chain or a rock: still leaves a crystal, so a big chain pays off.
            OxygenCrystal.Burst(pos, 1, Tuning.CrystalOxygen, transform.parent);
        }

        Blast.Create(pos, Tuning.MineBlastRadius, transform.parent);
        CameraRig.Shake();

        foreach (var hit in Physics2D.OverlapCircleAll(pos, Tuning.MineBlastRadius))
        {
            if (hit.gameObject == gameObject) continue;
            Vector2 to = (Vector2)hit.transform.position - pos;
            float dist = to.magnitude;

            // Push everything away; heavy rocks move less.
            var body = hit.attachedRigidbody;
            if (body != null)
            {
                float falloff = Mathf.Clamp01(1f - dist / Tuning.MineBlastRadius);
                Vector2 dir = dist > 0.01f ? to / dist : Vector2.up;
                body.AddForce(dir * Tuning.MineBlastImpulse * falloff * Mathf.Min(body.mass, 3f), ForceMode2D.Impulse);
            }

            var other = hit.GetComponent<Mine>();
            if (other != null && !other.exploded) other.Invoke("Chain", Tuning.MineChainDelay);

            var rock = hit.GetComponent<Asteroid>();
            if (rock != null && dist < Tuning.MineRockRadius + rock.Size * 0.5f) rock.Hit();
        }

        Destroy(gameObject);
    }
}
