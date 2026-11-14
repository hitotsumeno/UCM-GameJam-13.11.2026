using System.Collections.Generic;
using UnityEngine;

// A fast asteroid flying down at the player. It has a fiery tail (fire.png): the base of the
// flame sits on the rock and its tongues trail behind. While it is still off screen,
// ThreatMarkers shows where it will come in.
// Prefabs: Assets/Prefabs/Meteor_Small / _Medium (an Asteroid with this component).
[DefaultExecutionOrder(100)] // after CameraRig has moved the camera
public class Meteor : MonoBehaviour
{
    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    [SerializeField] Asteroid asteroid;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] Transform tail;

    // All meteors in flight (for the danger markers).
    public static readonly List<Meteor> All = new List<Meteor>();

    float age;
    Camera cam;

    public Vector2 Velocity { get { return rb.linearVelocity; } }
    public float Radius { get { return asteroid.Size * 0.5f; } }

    public static void ResetStatics()
    {
        All.Clear();
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Reset()
    {
        asteroid = GetComponent<Asteroid>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Place();
    }

    void LateUpdate()
    {
        age += Time.deltaTime;
        Place();

        if (cam == null) return;
        float bottom = cam.transform.position.y - cam.orthographicSize;
        if (age > T.MeteorLifetime || transform.position.y < bottom - 30f)
            Destroy(gameObject);
    }

    // The asteroid spins, so the tail is placed in world space every frame. The tail's +Y points
    // back along the flight; it flickers a little in length so a single picture does not look frozen.
    void Place()
    {
        float diameter = asteroid.Size;
        Vector2 pos = transform.position;
        Vector2 v = rb.linearVelocity;
        Vector2 dir = v.sqrMagnitude > 0.01f ? v.normalized : Vector2.down;

        float flicker = 1f + 0.05f * Mathf.Sin(age * 23f) + 0.03f * Mathf.Sin(age * 37f + 1.3f);
        tail.position = pos - dir * (diameter * 0.1f);
        tail.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f);
        tail.localScale = new Vector3(1f, flicker, 1f);

        if (cam == null) cam = Camera.main;
    }
}
