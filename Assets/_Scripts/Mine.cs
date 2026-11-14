using System.Collections.Generic;
using UnityEngine;

public enum MineMode { Hunter, Wall, Patrol, Orbit, Heavy }

// A mine. Several behaviours, so a minefield is never just targets standing still:
//  - Hunter (red): sleeps until the player is close, then swims toward them.
//  - Patrol (yellow): glides back and forth along Axis around the place it was put.
//  - Orbit (yellow): circles around its parent object (see Assets/Prefabs/MineRing.prefab).
//  - Wall (yellow): holds its place in a dense row; one shot sets the whole row off.
//  - Heavy (orange): big and still, takes three hits, and its blast wipes out everything around.
// Shot by the player: blows up, leaves crystals, counts for the combo.
// Touching the player or exploding next to them: breaches the hull and throws them away.
// A blast pushes everything around, sets off other mines nearby and cracks rocks.
// Prefabs: Assets/Prefabs/Mine_Hunter / _Wall / _Patrol / _Orbit / _Heavy.
public class Mine : MonoBehaviour
{
    // All mines of the current level (for the danger markers).
    public static readonly List<Mine> All = new List<Mine>();

    public static void ResetStatics()
    {
        All.Clear();
    }

    // A hunter that is awake and chasing the player.
    public bool Hunting { get { return mode == MineMode.Hunter && awake && !exploded; } }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    [SerializeField] MineMode mode;
    [SerializeField] Rigidbody2D rb;
    [Tooltip("Blinking lamp in the middle; its colour is the colour of the mine")]
    [SerializeField] SpriteRenderer lamp;
    [SerializeField] SpriteRenderer glow;
    [Tooltip("Bullets needed to set it off")]
    [SerializeField] int hits = 1;
    [Tooltip("Diameter in units (before the object scale)")]
    [SerializeField] float size = 1.1f;
    [Tooltip("Heavy mines blast wider: 2 = twice the usual radius")]
    [SerializeField] float blastScale = 1f;
    [Tooltip("Patrol: direction of the line it glides along")]
    [SerializeField] Vector2 axis = Vector2.right;
    [SerializeField] OxygenCrystal crystalPrefab;
    [SerializeField] Blast blastPrefab;

    Color color;
    bool awake;
    bool exploded;
    Vector2 home;          // Wall, Heavy: its place. Patrol: middle of its line. Orbit: center of the circle.
    float phase;           // Patrol: where on the line it starts. Orbit: starting angle.
    float orbitRadius;
    float turn = 1f;       // Orbit: +1 counter-clockwise, -1 clockwise

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        color = lamp.color;
    }

    void Start()
    {
        home = transform.position;
        axis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector2.right;
        phase = Random.Range(0f, Mathf.PI * 2f);
        rb.angularVelocity = Random.Range(-25f, 25f);

        if (mode == MineMode.Orbit && transform.parent != null)
        {
            // Circles around the parent, starting from where it was put.
            home = transform.parent.position;
            Vector2 offset = (Vector2)transform.position - home;
            orbitRadius = Mathf.Max(offset.magnitude, 0.5f);
            phase = Mathf.Atan2(offset.y, offset.x);
            // All mines of one ring turn the same way; different rings may differ.
            int seed = Mathf.RoundToInt(Mathf.Abs(home.x) * 7f + Mathf.Abs(home.y) * 3f);
            turn = seed % 2 == 0 ? 1f : -1f;
        }
    }

    void Update()
    {
        // The lamp blinks slowly while a hunter sleeps and fast when it hunts.
        bool fast = mode == MineMode.Hunter && awake;
        float k = 0.5f + 0.5f * Mathf.Sin(Time.time * (fast ? 14f : 3f));
        lamp.color = new Color(color.r, color.g, color.b, 0.35f + 0.65f * k);
        glow.color = new Color(color.r, color.g, color.b, (fast ? 0.35f : 0.2f) + 0.35f * k);
    }

    float Diameter
    {
        get { return size * Mathf.Abs(transform.lossyScale.x); }
    }

    void FixedUpdate()
    {
        if (exploded) return;
        var game = DraftGame.Instance;
        Transform player = game != null && game.State == GameState.Playing ? game.PlayerTransform : null;
        float dist = player != null ? Vector2.Distance(player.position, rb.position) : float.MaxValue;

        // Close enough to touch: go off (does not rely on the physics contact alone).
        if (dist < Diameter * 0.45f + T.PlayerDiameter * 0.5f + 0.08f)
        {
            Explode(true, false);
            return;
        }

        Vector2 wanted = Vector2.zero;
        float accel = T.MineAccel;
        switch (mode)
        {
            case MineMode.Hunter:
                // Wakes up when the player is close; gives up only when they are far away.
                awake = dist < (awake ? T.MineLoseRadius : T.MineWakeRadius);
                if (awake && dist > 0.01f)
                    wanted = ((Vector2)player.position - rb.position) / dist * T.MineSpeed;
                break;

            case MineMode.Wall:
            case MineMode.Heavy:
                wanted = Toward(home);
                break;

            case MineMode.Patrol:
            {
                float t = Time.time * T.MinePatrolSpeed / T.MinePatrolRange + phase;
                wanted = Toward(home + axis * Mathf.Sin(t) * T.MinePatrolRange);
                accel *= 3f;
                break;
            }
            case MineMode.Orbit:
            {
                float angle = phase + turn * Time.time * T.MineRingSpeed / orbitRadius;
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
        return Vector2.ClampMagnitude((target - rb.position) * 4f, T.MineSpeed * 1.5f);
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
            && collision.relativeVelocity.magnitude > T.MineTriggerSpeed)
            Explode(false, false);
    }

    // Hit by the player's bullet.
    public void Shot()
    {
        if (exploded) return;
        hits--;
        if (hits > 0)
        {
            // Not yet: the lamp turns whiter with every hit.
            color = Color.Lerp(color, Color.white, 0.45f);
            return;
        }
        Explode(false, true);
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
        float blastRadius = T.MineBlastRadius * blastScale;
        Transform player = game != null ? game.PlayerTransform : null;
        if (player != null && (touchedPlayer
            || Vector2.Distance(player.position, pos) < T.MineBreachRadius * blastScale))
            game.BreachHull(pos);

        if (game != null && shotByPlayer)
        {
            int combo = game.RegisterKill();
            float worth = T.CrystalOxygen * (1f + T.ComboBonusPerStep * (combo - 1));
            int crystals = Mathf.RoundToInt(T.CrystalsFromMine * blastScale);
            OxygenCrystal.Burst(crystalPrefab, pos, crystals, worth, transform.parent);
        }
        else if (game != null && !touchedPlayer)
        {
            // Set off by a chain or a rock: still leaves a crystal, so a big chain pays off.
            OxygenCrystal.Burst(crystalPrefab, pos, T.CrystalsFromChainedMine, T.CrystalOxygen, transform.parent);
        }

        Blast.Spawn(blastPrefab, pos, blastRadius, transform.parent);
        CameraRig.Shake();

        foreach (var hit in Physics2D.OverlapCircleAll(pos, blastRadius))
        {
            if (hit.gameObject == gameObject) continue;
            Vector2 to = (Vector2)hit.transform.position - pos;
            float dist = to.magnitude;

            // Push everything away; heavy rocks move less.
            var body = hit.attachedRigidbody;
            if (body != null)
            {
                float falloff = Mathf.Clamp01(1f - dist / blastRadius);
                Vector2 dir = dist > 0.01f ? to / dist : Vector2.up;
                body.AddForce(dir * T.MineBlastImpulse * falloff * Mathf.Min(body.mass, 3f), ForceMode2D.Impulse);
            }

            var other = hit.GetComponent<Mine>();
            if (other != null && !other.exploded) other.Invoke("Chain", T.MineChainDelay);

            var rock = hit.GetComponent<Asteroid>();
            if (rock != null && dist < T.MineRockRadius * blastScale + rock.Size * 0.5f) rock.Hit();
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Tuning t = T;
#if UNITY_EDITOR
        if (t == null) t = Tuning.FindInEditor();
#endif
        if (t == null) return;
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, t.MineBlastRadius * blastScale);
        if (mode == MineMode.Hunter)
        {
            Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, t.MineWakeRadius);
        }
        if (mode == MineMode.Patrol)
        {
            Vector3 a = axis.sqrMagnitude > 0.0001f ? (Vector3)axis.normalized : Vector3.right;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            Gizmos.DrawLine(transform.position - a * t.MinePatrolRange, transform.position + a * t.MinePatrolRange);
        }
    }
}
