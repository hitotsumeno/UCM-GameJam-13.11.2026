using UnityEngine;

public class GunScript : MonoBehaviour
{
    
   public Vector2 _gunDir { get; private set;}
    private void Update()
    {
        FindGunAngle();
    }

    private void FindGunAngle()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        Vector2 direction = mousePosition - transform.position;
        _gunDir = direction.normalized;
        float angle = Vector2.SignedAngle(Vector2.right, direction);
        ApplyNewAngle(angle);
    }

    private void ApplyNewAngle(float angle)
    {
        transform.eulerAngles = new Vector3 (0, 0, angle);
    }
}
