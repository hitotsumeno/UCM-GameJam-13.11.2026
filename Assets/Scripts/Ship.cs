using UnityEngine;

// The goal. Touching it slowly wins the level. Set up in Assets/Prefabs/Ship.prefab
// (a trigger collider matching the picture). Drawn nose to the right (+X).
public class Ship : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        if (DraftGame.Instance != null) DraftGame.Instance.PlayerReachedShip(player, transform.position);
    }
}
