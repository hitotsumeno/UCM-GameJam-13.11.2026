using UnityEngine;

// The astronaut: cannot move on its own, only recoil from the gun pushes it.
public class PlayerController : MonoBehaviour
{
    Rigidbody2D rb;
    Collider2D col;
    Transform visual;     // rotates toward the cursor; the physics body does not rotate
    float nextFireTime;

    public static PlayerController Create(Vector2 position, Transform parent)
    {
        var go = new GameObject("Player");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = Tuning.PlayerMass;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = Tuning.PlayerDiameter * 0.5f;
        col.sharedMaterial = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = Tuning.Bounciness };

        // Soft white glow under the player: the player is always the easiest thing to find.
        SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
            new Color(1f, 1f, 1f, Tuning.PlayerGlowOpacity),
            Vector2.one * Tuning.PlayerDiameter * Tuning.PlayerGlowSize, 9);

        var visual = new GameObject("Visual").transform;
        visual.SetParent(go.transform, false);
        // Picture "astronaut" (drawn nose to the right) or a placeholder.
        if (Art.TryMake("astronaut", visual, Tuning.PlayerDiameter, 10) == null)
        {
            SpriteFactory.MakeSprite("Body", visual, SpriteFactory.Circle(),
                new Color(0.9f, 0.9f, 0.95f), Vector2.one * Tuning.PlayerDiameter, 10);
            // Visor so the facing direction reads clearly.
            var visor = SpriteFactory.MakeSprite("Visor", visual, SpriteFactory.Circle(),
                new Color(0.3f, 0.6f, 1f), new Vector2(0.35f, 0.5f), 11);
            visor.transform.localPosition = new Vector3(0.2f, 0f, 0f);
            // Gun: a rectangle pointing right (+X) in local space.
            var gun = SpriteFactory.MakeSprite("Gun", visual, SpriteFactory.Square(),
                new Color(0.5f, 0.5f, 0.55f), new Vector2(0.7f, 0.2f), 12);
            gun.transform.localPosition = new Vector3(0.55f, -0.25f, 0f);
        }

        var p = go.AddComponent<PlayerController>();
        p.rb = rb;
        p.col = col;
        p.visual = visual;
        return p;
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
            nextFireTime = Time.time + 1f / Tuning.FireRate;
            Fire(aim);
        }
    }

    public float Speed
    {
        get { return rb.linearVelocity.magnitude; }
    }

    // Puts the player at a point, standing still (restart from a checkpoint).
    public void Teleport(Vector2 position)
    {
        transform.position = position;
        rb.position = position;
        rb.linearVelocity = Vector2.zero;
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
        float speed = Mathf.Max(Speed * Tuning.DockBounce, Tuning.DockBounceMinSpeed);
        rb.linearVelocity = away.normalized * speed;
    }

    void Fire(Vector2 aim)
    {
        DraftGame.Instance.SpendOxygen(Tuning.OxygenPerShot);
        Vector2 muzzle = (Vector2)visual.TransformPoint(new Vector3(0.9f, -0.25f, 0f));
        Bullet.Spawn(muzzle, aim, col, transform.parent);
        rb.AddForce(-aim * Tuning.RecoilImpulse, ForceMode2D.Impulse);
        CameraRig.Shake();
    }

    // Hitting a rock or a meteor too hard breaches the hull.
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<Asteroid>() == null) return;
        if (collision.relativeVelocity.magnitude <= Tuning.BreachImpactSpeed) return;
        if (DraftGame.Instance == null) return;
        Vector2 from = collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)collision.transform.position;
        DraftGame.Instance.BreachHull(from);
    }

    // Opens a breach on the side the hit came from. Called by DraftGame.
    public void AddBreach(Vector2 from)
    {
        Vector2 dir = from - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.0001f) dir = Random.insideUnitCircle;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();

        // In the ship's own frame, so the crack and the jet turn with the ship.
        Vector2 local = visual.InverseTransformDirection(dir);
        Leak.Create(visual, local * (Tuning.PlayerDiameter * 0.42f), local, rb, transform.parent);
    }

    // Removes the newest breach (sealed with an oxygen tank). Called by DraftGame.
    public void RemoveBreach()
    {
        var leaks = visual.GetComponentsInChildren<Leak>();
        if (leaks.Length > 0) Destroy(leaks[leaks.Length - 1].gameObject);
    }

    void FixedUpdate()
    {
        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude > Tuning.PlayerMaxSpeed * Tuning.PlayerMaxSpeed)
            rb.linearVelocity = v.normalized * Tuning.PlayerMaxSpeed;
    }
}
