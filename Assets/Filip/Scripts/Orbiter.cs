using UnityEngine;

// Keeps a rock circling around a point (a black hole). If something knocks the rock
// off its circle, it drifts back.
public class Orbiter : MonoBehaviour
{
    Rigidbody2D rb;
    Transform center;
    float radius, angle, direction;

    // direction: +1 = counter-clockwise, -1 = clockwise.
    public static void Attach(GameObject rock, Transform center, float radius, float startAngle, float direction)
    {
        var o = rock.AddComponent<Orbiter>();
        o.rb = rock.GetComponent<Rigidbody2D>();
        o.center = center;
        o.radius = radius;
        o.angle = startAngle;
        o.direction = direction;
    }

    void FixedUpdate()
    {
        if (center == null || rb == null) return;
        angle += direction * Tuning.HoleOrbitSpeed / radius * Time.fixedDeltaTime;
        Vector2 target = (Vector2)center.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        Vector2 wanted = Vector2.ClampMagnitude((target - rb.position) * 4f, Tuning.HoleOrbitSpeed * 2.5f);
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, wanted, 20f * Time.fixedDeltaTime);
    }
}
