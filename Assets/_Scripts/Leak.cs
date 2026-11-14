using UnityEngine;

// A breach in the hull: a dark crack with a jet of air escaping from it.
// It sits on the player's Visual, so the jet turns together with the ship.
// Every breach makes the oxygen leave faster (counted in DraftGame).
// Set up in Assets/Prefabs/Leak.prefab; the puffs are Assets/Prefabs/AirPuff.prefab.
public class Leak : MonoBehaviour
{
    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    [Tooltip("The dark crack; turned across the jet when the leak appears")]
    [SerializeField] Transform crack;
    [SerializeField] LeakPuff puffPrefab;

    Vector2 localDir;     // where the jet blows, in the ship's own frame
    Rigidbody2D shipBody;
    Transform world;      // puffs live in the level, not on the ship
    float timer;

    // ship: the turning part of the player. localPos: place of the breach on it.
    public static Leak Spawn(Leak prefab, Transform ship, Vector2 localPos, Vector2 localDir,
        Rigidbody2D shipBody, Transform world)
    {
        var leak = Instantiate(prefab, ship);
        leak.transform.localPosition = localPos;
        leak.localDir = localDir.normalized;
        leak.shipBody = shipBody;
        leak.world = world;
        if (leak.crack != null)
            leak.crack.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(localDir.y, localDir.x) * Mathf.Rad2Deg + 90f + Random.Range(-25f, 25f));
        return leak;
    }

    void Update()
    {
        var game = DraftGame.Instance;
        if (game == null || game.State != GameState.Playing) return; // no air left, or docked
        if (puffPrefab == null || transform.parent == null) return;

        timer -= Time.deltaTime;
        while (timer <= 0f)
        {
            timer += 1f / T.LeakPuffsPerSecond;
            Vector2 dir = transform.parent.TransformDirection(localDir);
            // A narrow cone of puffs flying away from the hull.
            float spread = Random.Range(-0.22f, 0.22f);
            Vector2 side = new Vector2(-dir.y, dir.x);
            Vector2 velocity = (dir + side * spread).normalized * T.LeakJetSpeed * Random.Range(0.7f, 1.2f);
            if (shipBody != null) velocity += shipBody.linearVelocity;
            LeakPuff.Spawn(puffPrefab, transform.position, velocity, world);
        }
    }
}
