using UnityEngine;

// A planet. It pulls the player and the bullets: the closer, the stronger (up to a limit).
// The edge of its gravity field is drawn as a dotted circle. Touching the planet is fatal.
// Flying past it bends the path, so the player can turn around it without shooting.
// Set up in Assets/Prefabs/Planet.prefab (not placed in any level yet: map symbol @).
public class Planet : MonoBehaviour
{
    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    void FixedUpdate()
    {
        Vector2 center = transform.position;
        foreach (var hit in Physics2D.OverlapCircleAll(center, T.GravityRadius))
        {
            var body = hit.attachedRigidbody;
            if (body == null) continue;
            // Only the player and the bullets feel the pull.
            if (hit.GetComponent<PlayerController>() == null && hit.GetComponent<Bullet>() == null) continue;

            Vector2 to = center - body.position;
            float dist = Mathf.Max(to.magnitude, 0.5f);
            float accel = Mathf.Min(T.GravityStrength / (dist * dist), T.GravityMaxAccel);
            body.AddForce(to / dist * accel * body.mass);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<PlayerController>() == null) return;
        if (DraftGame.Instance != null) DraftGame.Instance.Lose(Texts.HitPlanet);
    }
}
