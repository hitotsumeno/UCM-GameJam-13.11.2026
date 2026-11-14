using UnityEngine;

// The goal of a level: a space station. Reaching it slowly enough docks the ship.
public class Station : MonoBehaviour
{
    public static Station Create(Vector2 position, Transform parent)
    {
        var go = new GameObject("Station");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        // Picture "station" or a placeholder: a round hub with two blue panels.
        float s = Tuning.StationSize;
        if (Art.TryMake("station", go.transform, s, 5) == null)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var panel = SpriteFactory.MakeSprite("Panel", go.transform, SpriteFactory.Square(),
                    new Color(0.2f, 0.4f, 0.85f), new Vector2(s * 0.24f, s * 0.36f), 5);
                panel.transform.localPosition = new Vector3(side * s * 0.38f, 0f, 0f);
            }
            SpriteFactory.MakeSprite("Hub", go.transform, SpriteFactory.Circle(),
                new Color(0.8f, 0.82f, 0.88f), Vector2.one * s * 0.55f, 6);
            SpriteFactory.MakeSprite("Port", go.transform, SpriteFactory.Circle(),
                new Color(0.3f, 0.9f, 0.5f), Vector2.one * s * 0.14f, 7);
        }

        // The docking area is the round middle part.
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = s * 0.3f;
        col.isTrigger = true;

        return go.AddComponent<Station>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        if (DraftGame.Instance != null) DraftGame.Instance.PlayerReachedStation(player, transform.position);
    }
}
