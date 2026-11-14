using UnityEngine;

// A beacon inside a long level. Once the player flies through it, R restarts from here
// instead of from the very beginning of the level.
public class Checkpoint : MonoBehaviour
{
    SpriteRenderer ring, glow;
    bool used;

    public static Checkpoint Create(Vector2 position, Transform parent, bool alreadyUsed)
    {
        var go = new GameObject("Checkpoint");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        var c = go.AddComponent<Checkpoint>();
        c.glow = SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
            new Color(0.4f, 1f, 0.5f, 0.35f), Vector2.one * Tuning.CheckpointRadius * 2.2f, -30);
        c.ring = SpriteFactory.MakeSprite("Ring", go.transform, SpriteFactory.Ring(),
            new Color(0.4f, 1f, 0.5f, 0.8f), Vector2.one * Tuning.CheckpointRadius * 2f, -29);
        c.SetUsed(alreadyUsed);
        return c;
    }

    void SetUsed(bool value)
    {
        used = value;
        // A passed beacon goes dim.
        glow.color = new Color(0.4f, 1f, 0.5f, used ? 0.08f : 0.35f);
        ring.color = new Color(0.4f, 1f, 0.5f, used ? 0.2f : 0.8f);
    }

    void Update()
    {
        if (used) return;
        var game = DraftGame.Instance;
        Transform player = game != null && game.State == GameState.Playing ? game.PlayerTransform : null;
        if (player == null) return;
        if (Vector2.Distance(player.position, transform.position) > Tuning.CheckpointRadius) return;
        SetUsed(true);
        game.ReachCheckpoint(transform.position);
    }
}
