using UnityEngine;

// Place in a level where a hint is shown while the player is within Tuning.HintRadius.
// Invisible in the game; drawn as a gizmo in the Scene view.
public class HintMarker : MonoBehaviour
{
    [TextArea] public string text = "";

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.6f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0.4f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, Tuning.HintRadius);
    }
}
