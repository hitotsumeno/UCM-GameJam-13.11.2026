using UnityEngine;

// A homing mine. It sleeps until the player comes close, then slowly swims toward them.
//  - Shot by the player: it blows up at a distance, leaves oxygen crystals and counts for the combo.
//  - Touched by the player: it blows up in their face, costs oxygen and throws them away.
// The blast pushes everything around, sets off other mines nearby and cracks rocks,
// so a mine lured into rocks does the player's work.
public class Mine : MonoBehaviour
{
    Rigidbody2D rb;
    SpriteRenderer lamp, glow;
    bool awake;
    bool exploded;

    public static Mine Create(Vector2 position, Transform parent)
    {
        var go = new GameObject("Mine");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        float s = Tuning.MineSize;

        // Red glow: everything dangerous glows red or orange.
        var glow = SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
            new Color(1f, 0.2f, 0.15f, 0.45f), Vector2.one * s * 2.6f, 2);

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
        // Blinking red lamp in the middle (drawn over the picture too).
        var lamp = SpriteFactory.MakeSprite("Lamp", go.transform, SpriteFactory.Circle(),
            new Color(1f, 0.25f, 0.2f), Vector2.one * s * 0.3f, 5);

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
        m.rb = rb;
        m.lamp = lamp;
        m.glow = glow;
        return m;
    }

    void Update()
    {
        // The lamp blinks slowly while the mine sleeps and fast when it hunts.
        float k = 0.5f + 0.5f * Mathf.Sin(Time.time * (awake ? 14f : 3f));
        lamp.color = new Color(1f, 0.25f, 0.2f, 0.35f + 0.65f * k);
        glow.color = new Color(1f, 0.2f, 0.15f, (awake ? 0.35f : 0.2f) + 0.35f * k);
    }

    void FixedUpdate()
    {
        var game = DraftGame.Instance;
        Transform player = game != null && game.State == GameState.Playing ? game.PlayerTransform : null;

        Vector2 wanted = Vector2.zero;
        if (player != null)
        {
            Vector2 to = (Vector2)player.position - rb.position;
            float dist = to.magnitude;
            // Wakes up when the player is close; gives up only when they are far away.
            awake = dist < (awake ? Tuning.MineLoseRadius : Tuning.MineWakeRadius);
            if (awake && dist > 0.01f) wanted = to / dist * Tuning.MineSpeed;

            // Close enough to touch: go off (does not rely on the physics contact alone).
            if (!exploded && dist < Tuning.MineSize * 0.45f + Tuning.PlayerDiameter * 0.5f + 0.08f)
            {
                Explode(true, false);
                return;
            }
        }
        else awake = false;

        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, wanted, Tuning.MineAccel * Time.fixedDeltaTime);
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
