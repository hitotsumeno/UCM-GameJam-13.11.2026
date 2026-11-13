using UnityEngine;

// A fast asteroid flying down at the player. It has a fiery tail, and while it is
// still above the screen a red marker at the top edge shows where it will come from.
[DefaultExecutionOrder(100)] // after CameraRig has moved the camera
public class Meteor : MonoBehaviour
{
    Rigidbody2D rb;
    Transform tail;
    Transform warning;
    float diameter;
    float age;
    Camera cam;

    public static Meteor Create(Vector2 position, Vector2 velocity, Transform parent)
    {
        var size = Random.value < 0.6f ? AsteroidSize.Small : AsteroidSize.Medium;
        var asteroid = Asteroid.Create(size, position, velocity, parent, false);
        asteroid.SetTint(new Color(1f, 0.7f, 0.5f));
        asteroid.gameObject.name = "Meteor";

        var m = asteroid.gameObject.AddComponent<Meteor>();
        m.rb = asteroid.GetComponent<Rigidbody2D>();
        m.diameter = Asteroid.Diameter(size);

        // Hot glow around the rock and a soft fiery streak behind it (both drawn under the rock).
        SpriteFactory.MakeSprite("Heat", m.transform, SpriteFactory.Glow(),
            new Color(1f, 0.5f, 0.15f, 0.55f), Vector2.one * m.diameter * 2.4f, 1);
        m.tail = SpriteFactory.MakeSprite("Tail", m.transform, SpriteFactory.Glow(),
            new Color(1f, 0.6f, 0.2f, 0.6f), new Vector2(m.diameter * 1.1f, m.diameter * 5f), 2).transform;

        m.warning = SpriteFactory.MakeSprite("Warning", m.transform, SpriteFactory.Triangle(),
            new Color(1f, 0.3f, 0.25f), new Vector2(0.7f, 0.7f), 30).transform;
        m.warning.gameObject.SetActive(false);

        m.Place();
        return m;
    }

    void LateUpdate()
    {
        age += Time.deltaTime;
        Place();

        if (cam == null) return;
        float bottom = cam.transform.position.y - cam.orthographicSize;
        if (age > Tuning.MeteorLifetime || transform.position.y < bottom - 30f)
            Destroy(gameObject);
    }

    // The asteroid spins, so the tail and the marker are placed in world space every frame.
    void Place()
    {
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
            float halfW = cam.orthographicSize * cam.aspect - 0.6f;
            float x = Mathf.Clamp(pos.x, cam.transform.position.x - halfW, cam.transform.position.x + halfW);
            warning.position = new Vector3(x, top - 0.6f, 0f);
            warning.rotation = Quaternion.Euler(0f, 0f, 180f); // point down
        }
    }
}
