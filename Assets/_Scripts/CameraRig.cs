using UnityEngine;

// Smoothly follows the target and shakes on demand.
public class CameraRig : MonoBehaviour
{
    public static CameraRig Instance;

    public Transform target;
    Vector3 basePos;
    float shakeTimer;

    public static void ResetStatics()
    {
        Instance = null;
    }

    void Awake()
    {
        Instance = this;
        basePos = transform.position;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        basePos = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = basePos;
    }

    public static void Shake()
    {
        if (Instance != null) Instance.shakeTimer = Tuning.CameraShakeTime;
    }

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 goal = new Vector3(target.position.x, target.position.y, basePos.z);
            float t = 1f - Mathf.Exp(-Tuning.CameraFollowSharpness * Time.deltaTime);
            basePos = Vector3.Lerp(basePos, goal, t);
        }

        Vector3 offset = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            offset = (Vector3)(Random.insideUnitCircle * Tuning.CameraShakeStrength);
        }
        transform.position = basePos + offset;
    }
}
