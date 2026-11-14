using UnityEngine;

// Keeps a rock circling around a point (a black hole). If something knocks the rock
// off its circle, it drifts back. Added to the rocks inside the black hole prefabs.
public class Orbiter : MonoBehaviour
{
    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    [SerializeField] Rigidbody2D rb;
    [SerializeField] Transform center;
    [SerializeField] float radius = 5f;
    [Tooltip("Starting angle on the circle, radians")]
    [SerializeField] float angle;
    [Tooltip("+1 = counter-clockwise, -1 = clockwise")]
    [SerializeField] float direction = 1f;

    public void Setup(Rigidbody2D rb, Transform center, float radius, float startAngle, float direction)
    {
        this.rb = rb;
        this.center = center;
        this.radius = radius;
        this.angle = startAngle;
        this.direction = direction;
    }

    void FixedUpdate()
    {
        if (center == null || rb == null) return;
        angle += direction * T.HoleOrbitSpeed / radius * Time.fixedDeltaTime;
        Vector2 target = (Vector2)center.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        Vector2 wanted = Vector2.ClampMagnitude((target - rb.position) * 4f, T.HoleOrbitSpeed * 2.5f);
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, wanted, 20f * Time.fixedDeltaTime);
    }
}
