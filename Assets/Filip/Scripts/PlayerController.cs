using UnityEngine;

// The player's ship: a rocket that ran out of fuel. It cannot fly by itself.
// Two guns on its sides swing toward the cursor, and their recoil pushes the ship.
public class PlayerController : MonoBehaviour
{
    Rigidbody2D rb;
    Collider2D col;
    Transform hull;               // turns to follow the flight direction; the physics body does not rotate
    Transform gunLeft, gunRight;  // swing around their mounts on the hull
    float heading;                // where the nose points, degrees
    float nextFireTime;
    bool leftNext;

    // heading: where the nose points at the start, degrees (0 = right, 90 = up).
    public static PlayerController Create(Vector2 position, Transform parent, float heading)
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

        // Soft white glow under the ship: the player is always the easiest thing to find.
        SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
            new Color(1f, 1f, 1f, Tuning.PlayerGlowOpacity),
            Vector2.one * Tuning.PlayerDiameter * Tuning.PlayerGlowSize, 8);

        var hull = new GameObject("Hull").transform;
        hull.SetParent(go.transform, false);
        hull.localRotation = Quaternion.Euler(0f, 0f, heading);

        // Picture "ship" (drawn nose to the right) or a placeholder: a white body with a nose.
        float len = Tuning.PlayerShipLength;
        if (Art.TryMake("ship", hull, len, 10) == null)
        {
            SpriteFactory.MakeSprite("Body", hull, SpriteFactory.Square(),
                new Color(0.9f, 0.9f, 0.95f), new Vector2(len * 0.7f, len * 0.3f), 10);
            var nose = SpriteFactory.MakeSprite("Nose", hull, SpriteFactory.Triangle(),
                new Color(0.9f, 0.9f, 0.95f), new Vector2(len * 0.3f, len * 0.3f), 10);
            nose.transform.localPosition = new Vector3(len * 0.5f, 0f, 0f);
            nose.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            var tail = SpriteFactory.MakeSprite("Tail", hull, SpriteFactory.Square(),
                new Color(0.2f, 0.4f, 0.85f), new Vector2(len * 0.2f, len * 0.3f), 10);
            tail.transform.localPosition = new Vector3(-len * 0.3f, 0f, 0f);
        }

        var p = go.AddComponent<PlayerController>();
        p.rb = rb;
        p.col = col;
        p.hull = hull;
        p.heading = heading;
        p.gunLeft = MakeGun("GunLeft", hull, 1f);
        p.gunRight = MakeGun("GunRight", hull, -1f);
        p.AimGuns(heading - 180f); // guns point back, like engines, until the player aims
        return p;
    }

    // side: +1 = left side of the hull, -1 = right side.
    // The gun is not fixed to the hull itself: it hangs on a short strut at the middle of the ship.
    // Returns the mount: an empty object the gun swings around. Its local +X runs along the barrel.
    static Transform MakeGun(string name, Transform hull, float side)
    {
        var root = new Vector2(Tuning.ShipStrutRootX, side * Tuning.ShipStrutRootY);   // on the hull
        var end = new Vector2(Tuning.ShipGunMountX, side * Tuning.ShipGunMountY);      // where the gun hangs

        // Strut from the hull to the gun, drawn under the hull.
        Vector2 along = end - root;
        var strut = SpriteFactory.MakeSprite("Strut", hull, SpriteFactory.Square(),
            new Color(0.36f, 0.38f, 0.45f), new Vector2(along.magnitude, Tuning.ShipStrutThickness), 9);
        strut.transform.localPosition = (root + end) * 0.5f;
        strut.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg);

        var mount = new GameObject(name).transform;
        mount.SetParent(hull, false);
        mount.localPosition = end;

        // Picture "ship_gun" (barrel to the right, mount near the left end) or a grey bar.
        float len = Tuning.ShipGunLength;
        var art = Art.TryMake("ship_gun", mount, len, 11);
        Transform barrel = art != null
            ? art.transform
            : SpriteFactory.MakeSprite("Barrel", mount, SpriteFactory.Square(),
                new Color(0.6f, 0.62f, 0.68f), new Vector2(len, len * 0.18f), 11).transform;
        barrel.localPosition = new Vector3(len * (0.5f - Tuning.ShipGunMountInset), 0f, 0f);
        return mount;
    }

    void Update()
    {
        // The hull slowly turns its nose to where the ship is flying.
        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude > Tuning.ShipTurnMinSpeed * Tuning.ShipTurnMinSpeed)
        {
            float target = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            heading = Mathf.MoveTowardsAngle(heading, target, Tuning.ShipTurnSpeed * Time.deltaTime);
            hull.localRotation = Quaternion.Euler(0f, 0f, heading);
        }

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 aim = (Vector2)mouse - (Vector2)transform.position;
        if (aim.sqrMagnitude < 0.0001f) aim = Vector2.right;
        aim.Normalize();
        float side = AimGuns(Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);

        var game = DraftGame.Instance;
        bool playing = game != null && game.State == GameState.Playing;
        if (playing && Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / Tuning.FireRate;
            Fire(aim, side);
        }
    }

    // Swings both guns. The gun on the cursor's side points exactly at it; the other gun
    // mirrors the move on its own side. Returns the aim angle relative to the nose:
    // positive = the cursor is on the left side of the hull, negative = on the right.
    float AimGuns(float aimAngle)
    {
        float a = Mathf.DeltaAngle(heading, aimAngle);
        float swing = Mathf.Abs(a); // 0 = along the nose, 180 = straight back
        gunLeft.localRotation = Quaternion.Euler(0f, 0f, swing);
        gunRight.localRotation = Quaternion.Euler(0f, 0f, -swing);
        return a;
    }

    void Fire(Vector2 aim, float side)
    {
        // The gun on the cursor's side fires. Straight ahead or straight back both guns
        // can see the target, so they take turns.
        float off = Mathf.Abs(side);
        bool alongHull = off < Tuning.ShipGunAxisTolerance || off > 180f - Tuning.ShipGunAxisTolerance;
        bool useLeft = alongHull ? leftNext : side >= 0f;
        if (alongHull) leftNext = !leftNext;
        Transform gun = useLeft ? gunLeft : gunRight;

        DraftGame.Instance.SpendOxygen(Tuning.OxygenPerShot);
        Vector2 muzzle = (Vector2)gun.position + (Vector2)gun.right * (Tuning.ShipGunLength * 0.8f);
        Bullet.Spawn(muzzle, aim, col, transform.parent);
        rb.AddForce(-aim * Tuning.RecoilImpulse, ForceMode2D.Impulse);
        CameraRig.Shake();
    }

    public float Speed
    {
        get { return rb.linearVelocity.magnitude; }
    }

    // Called on docking: the ship stops drifting.
    public void Stop()
    {
        rb.linearVelocity = Vector2.zero;
    }

    // Thrown back from a point (hit the station too fast).
    public void BounceFrom(Vector2 point)
    {
        Vector2 away = (Vector2)transform.position - point;
        if (away.sqrMagnitude < 0.0001f) away = Vector2.down;
        float speed = Mathf.Max(Speed * Tuning.DockBounce, Tuning.DockBounceMinSpeed);
        rb.linearVelocity = away.normalized * speed;
    }

    // Hitting an asteroid too fast costs oxygen.
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<Asteroid>() == null) return;
        float speed = collision.relativeVelocity.magnitude;
        if (speed <= Tuning.ImpactSafeSpeed) return;
        float loss = Mathf.Min((speed - Tuning.ImpactSafeSpeed) * Tuning.ImpactOxygenPerSpeed, Tuning.ImpactMaxOxygen);
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
        if (v.sqrMagnitude > Tuning.PlayerMaxSpeed * Tuning.PlayerMaxSpeed)
            rb.linearVelocity = v.normalized * Tuning.PlayerMaxSpeed;
    }
}
