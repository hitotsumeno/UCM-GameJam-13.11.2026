using UnityEngine;

// The goal. Touching it wins the level. Drawn nose to the right (+X).
public class Ship : MonoBehaviour
{
    public static Ship Create(Vector2 position, Transform parent)
    {
        var go = new GameObject("Ship");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        float len = Tuning.ShipLength, h = Tuning.ShipHeight;
        Vector2 size = new Vector2(len, h);

        // Picture "ship" (drawn nose to the right) or a placeholder.
        var art = Art.TryMake("ship", go.transform, len, 5);
        if (art != null)
        {
            size = Art.WorldSize(art); // docking area matches the picture
        }
        else
        {
            SpriteFactory.MakeSprite("Hull", go.transform, SpriteFactory.Square(),
                new Color(0.55f, 0.65f, 0.8f), new Vector2(len, h), 5);
            var nose = SpriteFactory.MakeSprite("Nose", go.transform, SpriteFactory.Square(),
                new Color(0.8f, 0.9f, 1f), new Vector2(len * 0.2f, h * 0.5f), 6);
            nose.transform.localPosition = new Vector3(len * 0.4f, 0f, 0f);
            var engine = SpriteFactory.MakeSprite("Engine", go.transform, SpriteFactory.Square(),
                new Color(0.3f, 0.35f, 0.45f), new Vector2(len * 0.12f, h * 0.7f), 6);
            engine.transform.localPosition = new Vector3(-len * 0.44f, 0f, 0f);
        }

        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.isTrigger = true;

        return go.AddComponent<Ship>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        if (DraftGame.Instance != null) DraftGame.Instance.PlayerReachedShip(player, transform.position);
    }
}
