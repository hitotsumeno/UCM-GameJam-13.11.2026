using UnityEngine;
using UnityEngine.PlayerLoop;

public class PlayerMovementScript : MonoBehaviour
{
    [Header("References")]
    public Transform rodTip;
    public Rigidbody2D target;

    [Header("Settings")]
    public float forceStrength = 10f;

    void Update()
    {
        // Get mouse world position
        Vector3 mousePosition =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Calculate direction
        Vector2 direction =
            (mousePosition - transform.position).normalized;

        // Rotate rod
        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Apply force on mouse click
        if (Input.GetMouseButtonDown(0))
        {
            if (target != null)
            {
                Vector2 force = direction * forceStrength;

                target.AddForceAtPosition(
                    force,
                    rodTip.position,
                    ForceMode2D.Impulse
                );
            }
        }
    }
}
