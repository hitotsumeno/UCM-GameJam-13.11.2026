using UnityEngine;

// Smoothly follows the target and shakes on demand.
public class CameraRig : MonoBehaviour
{
    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

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
        if (Instance != null) Instance.shakeTimer = Instance.T.CameraShakeTime;
    }

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 goal = new Vector3(target.position.x, target.position.y, basePos.z);
            float t = 1f - Mathf.Exp(-T.CameraFollowSharpness * Time.deltaTime);
            basePos = Vector3.Lerp(basePos, goal, t);
        }

        Vector3 offset = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            offset = (Vector3)(Random.insideUnitCircle * T.CameraShakeStrength);
        }
        transform.position = basePos + offset;
    }
}
