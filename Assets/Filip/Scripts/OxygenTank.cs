using UnityEngine;

// Pick it up to restore oxygen.
public class OxygenTank : MonoBehaviour
{
    public static OxygenTank Create(Vector2 position, Transform parent)
    {
        var go = new GameObject("OxygenTank");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        // Cyan glow, so the tank is easy to spot from far away.
        SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
            new Color(0.3f, 0.85f, 1f, Tuning.OxygenTankGlowOpacity), Vector2.one * Tuning.OxygenTankGlowSize, 2);

        // Picture "oxygen" or a placeholder: a small cyan bottle.
        float s = Tuning.OxygenTankSize;
        if (Art.TryMake("oxygen", go.transform, s, 4) == null)
        {
            var color = new Color(0.3f, 0.85f, 1f);
            var bottle = SpriteFactory.MakeSprite("Bottle", go.transform, SpriteFactory.Square(),
                color, new Vector2(s * 0.5f, s * 0.8f), 4);
            bottle.transform.localPosition = new Vector3(0f, -s * 0.1f, 0f);
            var cap = SpriteFactory.MakeSprite("Cap", go.transform, SpriteFactory.Square(),
                Color.white, new Vector2(s * 0.25f, s * 0.2f), 4);
            cap.transform.localPosition = new Vector3(0f, s * 0.4f, 0f);
        }

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = Tuning.OxygenTankPickupRadius;
        col.isTrigger = true;

        return go.AddComponent<OxygenTank>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        var game = DraftGame.Instance;
        if (game == null || game.State != GameState.Playing) return;
        game.AddOxygen(Tuning.OxygenTankAmount);
        Destroy(gameObject);
    }
}
