using UnityEngine;

// Pick it up to restore oxygen. Set up in Assets/Prefabs/OxygenTank.prefab.
public class OxygenTank : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        var game = DraftGame.Instance;
        if (game == null || game.State != GameState.Playing) return;
        game.AddOxygen(Tuning.OxygenTankAmount);
        Destroy(gameObject);
    }
}
