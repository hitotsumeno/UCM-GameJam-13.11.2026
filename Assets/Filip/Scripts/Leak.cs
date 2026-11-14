using UnityEngine;

// A breach in the hull: a dark crack with a jet of air escaping from it.
// It sits on the ship's picture, so the jet turns together with the ship.
// Every breach makes the oxygen leave faster (counted in DraftGame).
public class Leak : MonoBehaviour
{
    Vector2 localDir;     // where the jet blows, in the ship's own frame
    Rigidbody2D shipBody;
    Transform world;      // puffs live in the world, not on the ship
    float timer;

    // ship: the turning part of the player. localPos: place of the breach on it.
    public static Leak Create(Transform ship, Vector2 localPos, Vector2 localDir, Rigidbody2D shipBody, Transform world)
    {
        var go = new GameObject("Leak");
        go.transform.SetParent(ship, false);
        go.transform.localPosition = localPos;

        // The crack itself.
        var crack = SpriteFactory.MakeSprite("Crack", go.transform, SpriteFactory.Square(),
            new Color(0.05f, 0.05f, 0.08f), new Vector2(0.26f, 0.07f), 13);
        crack.transform.localRotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(localDir.y, localDir.x) * Mathf.Rad2Deg + 90f + Random.Range(-25f, 25f));

        var leak = go.AddComponent<Leak>();
        leak.localDir = localDir.normalized;
        leak.shipBody = shipBody;
        leak.world = world;
        return leak;
    }

    void Update()
    {
        var game = DraftGame.Instance;
        if (game == null || game.State != GameState.Playing) return; // no air left, or docked

        timer -= Time.deltaTime;
        while (timer <= 0f)
        {
            timer += 1f / Tuning.LeakPuffsPerSecond;
            Vector2 dir = transform.parent.TransformDirection(localDir);
            // A narrow cone of puffs flying away from the hull.
            float spread = Random.Range(-0.22f, 0.22f);
            Vector2 side = new Vector2(-dir.y, dir.x);
            Vector2 velocity = (dir + side * spread).normalized
                * Tuning.LeakJetSpeed * Random.Range(0.7f, 1.2f);
            if (shipBody != null) velocity += shipBody.linearVelocity;
            LeakPuff.Create(transform.position, velocity, world);
        }
    }
}

