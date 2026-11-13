using UnityEngine;
using UnityEngine.PlayerLoop;

public class PlayerMovementScript : MonoBehaviour
{

    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private Collider2D _coll;

    [SerializeField] private Vector2 shootDir;
    [SerializeField] private float shootForce;

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector2 Shoot = new Vector2 (shootDir.x * shootForce, shootDir.y * shootForce);

            _rb.AddForce(Shoot, ForceMode2D.Impulse);
        }    
    }
}
