using UnityEngine;

// The goal. Touching it slowly wins the level. Set up in Assets/Prefabs/Ship.prefab
// (a trigger collider matching the picture). Drawn nose to the right (+X).
// The ring around it shows whether the player may dock: green = slow enough, red = too fast.
public class Ship : MonoBehaviour
{
    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    [Tooltip("Ring sprite around the ship; invisible until the player comes close")]
    [SerializeField] SpriteRenderer dockRing;

    static readonly Color OkColor = new Color(0.45f, 1f, 0.55f);
    static readonly Color TooFastColor = new Color(1f, 0.35f, 0.3f);

    void Update()
    {
        var game = DraftGame.Instance;
        Transform player = game != null ? game.PlayerTransform : null;
        if (player == null || dockRing == null) return;

        // Fades in as the player approaches, slowly breathes so it is noticed.
        float dist = Vector2.Distance(player.position, transform.position);
        float near = Mathf.InverseLerp(T.DockRingShowDistance, T.DockRingShowDistance * 0.6f, dist);
        Color c = game.PlayerSpeed <= T.DockMaxSpeed ? OkColor : TooFastColor;
        c.a = near * (0.55f + 0.2f * Mathf.Sin(Time.time * 4f));
        dockRing.color = c;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        if (DraftGame.Instance != null) DraftGame.Instance.PlayerReachedShip(player, transform.position);
    }
}
