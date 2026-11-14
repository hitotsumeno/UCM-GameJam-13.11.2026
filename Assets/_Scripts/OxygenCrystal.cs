using UnityEngine;

// A small piece of frozen oxygen left by a destroyed rock. It drifts, fades out after
// a few seconds and gives oxygen when the player reaches it. The shot pushed the player
// away from the rock, so collecting the crystals means flying back into the debris.
// Set up in Assets/Prefabs/OxygenCrystal.prefab.
public class OxygenCrystal : MonoBehaviour
{
    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    [SerializeField] SpriteRenderer core;
    [SerializeField] SpriteRenderer glow;

    Vector2 velocity;
    float age;
    float value;
    Color coreColor, glowColor;

    // Scatters `count` crystals from a point. Each one is worth `value` oxygen.
    public static void Burst(OxygenCrystal prefab, Vector2 position, int count, float value, Transform parent)
    {
        if (prefab == null) return;
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(prefab.T.CrystalBurstSpeedMin, prefab.T.CrystalBurstSpeedMax);
            var c = Instantiate(prefab, position, Quaternion.identity, parent);
            c.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            c.value = value;
        }
    }

    void Awake()
    {
        coreColor = core.color;
        glowColor = glow.color;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= T.CrystalLifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Drift and slow down.
        Vector2 pos = transform.position;
        velocity *= Mathf.Exp(-T.CrystalDamping * Time.deltaTime);

        var game = DraftGame.Instance;
        Transform player = game != null ? game.PlayerTransform : null;
        if (player != null && game.State == GameState.Playing)
        {
            Vector2 toPlayer = (Vector2)player.position - pos;
            float dist = toPlayer.magnitude;
            if (dist < T.CrystalPickupRadius)
            {
                game.CollectCrystal(value);
                Destroy(gameObject);
                return;
            }
            // Close crystals are pulled toward the player.
            if (dist < T.CrystalMagnetRadius)
                velocity += toPlayer / dist * T.CrystalMagnetAccel * Time.deltaTime;
        }
        transform.position = pos + velocity * Time.deltaTime;

        // Blink before disappearing.
        float left = T.CrystalLifetime - age;
        float a = 1f;
        if (left < T.CrystalBlinkTime) a = Mathf.Repeat(left * 8f, 1f) < 0.5f ? 0.25f : 1f;
        core.color = new Color(coreColor.r, coreColor.g, coreColor.b, coreColor.a * a);
        glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * a);
    }
}
