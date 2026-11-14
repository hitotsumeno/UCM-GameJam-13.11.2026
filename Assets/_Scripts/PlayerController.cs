using UnityEngine;

// The astronaut: cannot move on its own, only recoil from the gun pushes it.
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

    float nextFireTime;

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

        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        visual.rotation = Quaternion.Euler(0f, 0f, angle);

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

    // Called on docking: the astronaut stops drifting.
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

    // Hitting an asteroid too fast costs oxygen.
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<Asteroid>() == null) return;
        float speed = collision.relativeVelocity.magnitude;
        if (speed <= T.ImpactSafeSpeed) return;
        float loss = Mathf.Min((speed - T.ImpactSafeSpeed) * T.ImpactOxygenPerSpeed, T.ImpactMaxOxygen);
        if (DraftGame.Instance != null)
        {
            DraftGame.Instance.SpendOxygen(loss);
            DraftGame.Instance.BreakCombo(); // a hard hit ends the combo
        }
        CameraRig.Shake();
    }

    void FixedUpdate()
    {
        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude > T.PlayerMaxSpeed * T.PlayerMaxSpeed)
            rb.linearVelocity = v.normalized * T.PlayerMaxSpeed;
    }
}
