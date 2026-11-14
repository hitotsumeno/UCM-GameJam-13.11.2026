using UnityEngine;

// The goal. Touching it wins the level. Drawn nose to the right (+X).
// A ring around it shows whether the player may dock: green = slow enough, red = too fast.
public class Ship : MonoBehaviour
{
    SpriteRenderer dockRing;

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

        // Docking ring: invisible until the player comes close (see Update).
        var ship = go.AddComponent<Ship>();
        float ringSize = Mathf.Max(size.x, size.y) + Tuning.DockRingMargin * 2f;
        ship.dockRing = SpriteFactory.MakeSprite("DockRing", go.transform, SpriteFactory.Ring(),
            new Color(1f, 1f, 1f, 0f), Vector2.one * ringSize, 4);
        return ship;
    }

    void Update()
    {
        var game = DraftGame.Instance;
        Transform player = game != null ? game.PlayerTransform : null;
        if (player == null || dockRing == null) return;

        // Fades in as the player approaches, slowly breathes so it is noticed.
        float dist = Vector2.Distance(player.position, transform.position);
        float near = Mathf.InverseLerp(Tuning.DockRingShowDistance, Tuning.DockRingShowDistance * 0.6f, dist);
        bool ok = game.PlayerSpeed <= Tuning.DockMaxSpeed;
        Color c = ok ? new Color(0.45f, 1f, 0.55f) : new Color(1f, 0.35f, 0.3f);
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
