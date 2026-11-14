using UnityEngine;

// Pick it up to restore oxygen. Set up in Assets/Prefabs/OxygenTank.prefab.
public class OxygenTank : MonoBehaviour
{
    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        var game = DraftGame.Instance;
        if (game == null || game.State != GameState.Playing) return;
        game.AddOxygen(T.OxygenTankAmount);
        Destroy(gameObject);
    }
}
