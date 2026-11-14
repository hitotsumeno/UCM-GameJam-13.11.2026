using System.Collections.Generic;
using UnityEngine;

// A black hole. It pulls the player, the bullets, the mines and the oxygen crystals.
// Bullets, mines and crystals that reach its dark middle are gone. The player is not
// killed: the hole is a wormhole and throws them out of another hole nearby.
// Big and small holes stand still with rocks circling them (children with an Orbiter);
// drifting ones glide back and forth along Axis, so the safe path keeps changing.
// Prefabs: Assets/Prefabs/BlackHole_Big / _Small / _Drift.
public class BlackHole : MonoBehaviour
{
    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    // All holes of the current level; oxygen crystals ask them for their pull.
    public static readonly List<BlackHole> All = new List<BlackHole>();
    static float immuneUntil;

    [Tooltip("The dark middle, units")]
    [SerializeField] float bodyRadius = 1.1f;
    [Tooltip("The pull reaches this far, units")]
    [SerializeField] float fieldRadius = 11f;
    [Tooltip("Pull = this / distance squared")]
    [SerializeField] float strength = 200f;
    [Tooltip("Glides back and forth along Axis around the place it was put")]
    [SerializeField] bool drifts;
    [SerializeField] Vector2 axis = Vector2.right;
    [Tooltip("The picture; it slowly turns")]
    [SerializeField] Transform picture;

    Vector2 home;
    float phase;

    public static void ResetStatics()
    {
        All.Clear();
        immuneUntil = 0f;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        home = transform.position;
        axis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector2.right;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (picture != null) picture.Rotate(0f, 0f, -T.HoleSpinSpeed * Time.deltaTime);
    }

    // Acceleration this hole gives to a thing at `position`. Zero outside its field.
    public Vector2 PullAt(Vector2 position)
    {
        Vector2 to = (Vector2)transform.position - position;
        float dist = to.magnitude;
        if (dist > fieldRadius || dist < 0.01f) return Vector2.zero;
        float d = Mathf.Max(dist, bodyRadius * 0.5f);
        return to / dist * Mathf.Min(strength / (d * d), T.HoleMaxAccel);
    }

    // True if the point is inside the dark middle.
    public bool Swallows(Vector2 position, float margin)
    {
        return Vector2.Distance(transform.position, position) < bodyRadius + margin;
    }

    // Falling into a hole is not death: it spits the player out of one of the two nearest
    // other holes, at speed, with a breached hull and some air lost.
    // It may throw them forward or back: a gamble, not a shortcut.
    void ThrowThroughWormhole(PlayerController player)
    {
        var game = DraftGame.Instance;
        if (game == null || game.State != GameState.Playing) return;
        if (Time.time < immuneUntil) return; // just came out of a hole: cannot fall in again right away
        immuneUntil = Time.time + T.WormholeImmunity;

        // The two nearest other holes; one of them is the exit.
        BlackHole first = null, second = null;
        float d1 = float.MaxValue, d2 = float.MaxValue;
        foreach (var other in All)
        {
            if (other == this) continue;
            float d = Vector2.Distance(other.transform.position, transform.position);
            if (d < d1) { second = first; d2 = d1; first = other; d1 = d; }
            else if (d < d2) { second = other; d2 = d; }
        }
        BlackHole exit = first == null ? this : (second != null && Random.value < 0.5f ? second : first);

        Vector2 dir = Random.insideUnitCircle.normalized;
        if (dir == Vector2.zero) dir = Vector2.up;
        Vector2 from = transform.position;
        Vector2 to = (Vector2)exit.transform.position + dir * (exit.bodyRadius + 2.5f);
        player.Launch(to, dir * T.WormholeExitSpeed);
        game.ThrownThroughWormhole(from, to);
    }

    void FixedUpdate()
    {
        if (drifts)
        {
            float t = Time.time * T.HoleDriftSpeed / T.HoleDriftRange + phase;
            transform.position = home + axis * Mathf.Sin(t) * T.HoleDriftRange;
        }

        Vector2 center = transform.position;
        foreach (var hit in Physics2D.OverlapCircleAll(center, fieldRadius))
        {
            var body = hit.attachedRigidbody;
            if (body == null) continue;
            var player = hit.GetComponent<PlayerController>();
            bool bullet = hit.GetComponent<Bullet>() != null;
            bool mine = hit.GetComponent<Mine>() != null;
            if (player == null && !bullet && !mine) continue; // rocks do not feel the pull

            if (Swallows(body.position, player != null ? T.PlayerDiameter * 0.2f : 0f))
            {
                if (player != null) ThrowThroughWormhole(player);
                else Destroy(hit.gameObject); // bullets and mines just vanish: a hunter can be lured into a hole
                continue;
            }
            // Bullets are pulled harder, so their arc is easy to see.
            float scale = bullet ? T.HoleBulletPull : 1f;
            body.AddForce(PullAt(body.position) * body.mass * scale);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.55f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, fieldRadius);
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, bodyRadius);
    }
}
