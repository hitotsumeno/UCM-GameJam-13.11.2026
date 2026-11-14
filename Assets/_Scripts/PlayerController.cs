using UnityEngine;

// The player: cannot move on its own, only recoil from the gun pushes it.
// A hard hit breaches the hull: a Leak appears on the side the hit came from.
// Set up in Assets/Prefabs/Player.prefab.
public class PlayerController : MonoBehaviour
{
    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    [SerializeField] Rigidbody2D rb;
    [SerializeField] Collider2D col;
    [Tooltip("Rotates toward the cursor; the physics body does not rotate")]
    [SerializeField] Transform visual;
    [Tooltip("Where bullets appear, a child of Visual")]
    [SerializeField] Transform muzzle;
    [SerializeField] Bullet bulletPrefab;
    [Tooltip("Crack with a jet of air, added to Visual for every hull breach")]
    [SerializeField] Leak leakPrefab;

    float nextFireTime;
    float aimAngle;

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 aim = (Vector2)mouse - (Vector2)transform.position;
        if (aim.sqrMagnitude < 0.0001f) aim = Vector2.right;
        aim.Normalize();

        // The whole ship (with its collider) turns in FixedUpdate so that +X, the main turbine,
        // faces the cursor.
        aimAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;

        var game = DraftGame.Instance;
        bool playing = game != null && game.State == GameState.Playing;
        if (playing && Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / T.FireRate;
            Fire(aim);
        }
    }

    public float Speed
    {
        get { return rb.linearVelocity.magnitude; }
    }

    // Puts the player at a point, already flying (thrown out of a wormhole).
    public void Launch(Vector2 position, Vector2 velocity)
    {
        transform.position = position;
        rb.position = position;
        rb.linearVelocity = velocity;
    }

    // Called on docking: the player stops drifting.
    public void Stop()
    {
        rb.linearVelocity = Vector2.zero;
    }

    // Thrown back from a point (hit the ship too fast).
    public void BounceFrom(Vector2 point)
    {
        Vector2 away = (Vector2)transform.position - point;
        if (away.sqrMagnitude < 0.0001f) away = Vector2.down;
        float speed = Mathf.Max(Speed * T.DockBounce, T.DockBounceMinSpeed);
        rb.linearVelocity = away.normalized * speed;
    }

    void Fire(Vector2 aim)
    {
        DraftGame.Instance.SpendOxygen(T.OxygenPerShot);
        Vector2 from = muzzle != null ? (Vector2)muzzle.position
                                      : (Vector2)visual.TransformPoint(new Vector3(0.9f, -0.25f, 0f));
        Bullet.Spawn(bulletPrefab, from, aim, col, transform.parent);
        rb.AddForce(-aim * T.RecoilImpulse, ForceMode2D.Impulse);
        CameraRig.Shake();
    }

    // Hitting a rock or a meteor too hard breaches the hull.
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<Asteroid>() == null) return;
        if (collision.relativeVelocity.magnitude <= T.BreachImpactSpeed) return;
        if (DraftGame.Instance == null) return;
        Vector2 from = collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)collision.transform.position;
        DraftGame.Instance.BreachHull(from);
    }

    // Opens a breach on the side the hit came from. Called by DraftGame.
    public void AddBreach(Vector2 from)
    {
        if (leakPrefab == null) return;
        Vector2 dir = from - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.0001f) dir = Random.insideUnitCircle;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();

        // In the ship's own frame, so the crack and the jet turn with the ship.
        Vector2 local = visual.InverseTransformDirection(dir);
        Leak.Spawn(leakPrefab, visual, local * (T.PlayerDiameter * 0.42f), local, rb, transform.parent);
    }

    // Removes the newest breach (sealed with an oxygen tank). Called by DraftGame.
    public void RemoveBreach()
    {
        var leaks = visual.GetComponentsInChildren<Leak>();
        if (leaks.Length > 0) Destroy(leaks[leaks.Length - 1].gameObject);
    }

    void FixedUpdate()
    {
        // Turn toward the cursor; hits do not make the ship spin.
        rb.angularVelocity = 0f;
        rb.MoveRotation(aimAngle);

        // The storm beyond the level edge pushes the ship back in: gently at the edge,
        // harder deeper in. The ship can still shoot its way in, but it cannot drift away.
        var game = DraftGame.Instance;
        if (game != null && game.State == GameState.Playing)
        {
            Rect b = game.LevelBounds;
            Vector2 p = rb.position;
            Vector2 back = new Vector2(Mathf.Clamp(p.x, b.xMin, b.xMax), Mathf.Clamp(p.y, b.yMin, b.yMax)) - p;
            float depth = back.magnitude;
            if (depth > 0.001f && b.width > 0f)
            {
                float k = 0.35f + 0.65f * Mathf.Clamp01(depth / T.StormPushDepth);
                rb.AddForce(back / depth * T.StormPush * k * rb.mass);
            }
        }

        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude > T.PlayerMaxSpeed * T.PlayerMaxSpeed)
            rb.linearVelocity = v.normalized * T.PlayerMaxSpeed;
    }
}
