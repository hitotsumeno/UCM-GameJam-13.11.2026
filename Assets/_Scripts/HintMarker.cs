using UnityEngine;

// Place in a level where a hint is shown while the player is within Tuning.HintRadius.
// Invisible in the game; drawn as a gizmo in the Scene view.
public class HintMarker : MonoBehaviour
{
    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    [TextArea] public string text = "";

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.6f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0.4f, 0.25f);
        Tuning t = T;
#if UNITY_EDITOR
        if (t == null) t = Tuning.FindInEditor();
#endif
        if (t != null) Gizmos.DrawWireSphere(transform.position, t.HintRadius);
    }
}
