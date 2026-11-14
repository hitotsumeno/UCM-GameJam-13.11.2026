using UnityEngine;

// A fast asteroid flying down at the player. It has a fiery tail, and while it is
// still above the screen a red marker at the top edge shows where it will come from.
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
    [SerializeField] Transform warning;

    float age;
    Camera cam;

    void Reset()
    {
        asteroid = GetComponent<Asteroid>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        warning.gameObject.SetActive(false);
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

    // The asteroid spins, so the tail and the marker are placed in world space every frame.
    void Place()
    {
        float diameter = asteroid.Size;
        Vector2 pos = transform.position;
        Vector2 v = rb.linearVelocity;
        Vector2 dir = v.sqrMagnitude > 0.01f ? v.normalized : Vector2.down;

        // Tail behind the meteor, stretched along its flight.
        float length = diameter * 5f;
        tail.position = pos - dir * (length * 0.4f);
        tail.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Marker at the top edge while the meteor is still above the screen.
        float top = cam.transform.position.y + cam.orthographicSize;
        bool above = pos.y - diameter * 0.5f > top;
        if (warning.gameObject.activeSelf != above) warning.gameObject.SetActive(above);
        if (above)
        {
            float margin = T.MeteorWarningSize * 0.8f;
            float halfW = cam.orthographicSize * cam.aspect - margin;
            float x = Mathf.Clamp(pos.x, cam.transform.position.x - halfW, cam.transform.position.x + halfW);
            warning.position = new Vector3(x, top - margin, 0f);
            warning.rotation = Quaternion.Euler(0f, 0f, 180f); // point down
        }
    }
}
