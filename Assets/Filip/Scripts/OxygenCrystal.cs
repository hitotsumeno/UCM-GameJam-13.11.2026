using UnityEngine;

// A small piece of frozen oxygen left by a destroyed rock. It drifts, fades out after
// a few seconds and gives oxygen when the player reaches it. The shot pushed the player
// away from the rock, so collecting the crystals means flying back into the debris.
public class OxygenCrystal : MonoBehaviour
{
    Vector2 velocity;
    float age;
    float value;
    SpriteRenderer core, glow;
    Color coreColor, glowColor;

    // Scatters `count` crystals from a point. Each one is worth `value` oxygen.
    public static void Burst(Vector2 position, int count, float value, Transform parent)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(Tuning.CrystalBurstSpeedMin, Tuning.CrystalBurstSpeedMax);
            Create(position, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed, value, parent);
        }
    }

    static void Create(Vector2 position, Vector2 velocity, float value, Transform parent)
    {
        var go = new GameObject("OxygenCrystal");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        var c = go.AddComponent<OxygenCrystal>();
        c.velocity = velocity;
        c.value = value;

        // Cyan glow, like the oxygen tanks: everything that gives air is cyan.
        c.glowColor = new Color(0.3f, 0.85f, 1f, 0.7f);
        c.glow = SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
            c.glowColor, Vector2.one * Tuning.CrystalSize * 3.5f, 6);

        // Picture "crystal" or a placeholder: a small diamond.
        c.core = Art.TryMake("crystal", go.transform, Tuning.CrystalSize, 7);
        if (c.core == null)
        {
            c.core = SpriteFactory.MakeSprite("Core", go.transform, SpriteFactory.Square(),
                new Color(0.75f, 0.97f, 1f), Vector2.one * Tuning.CrystalSize * 0.7f, 7);
            c.core.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
        c.coreColor = c.core.color;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= Tuning.CrystalLifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Drift and slow down.
        Vector2 pos = transform.position;
        velocity *= Mathf.Exp(-Tuning.CrystalDamping * Time.deltaTime);

        var game = DraftGame.Instance;
        Transform player = game != null ? game.PlayerTransform : null;
        if (player != null && game.State == GameState.Playing)
        {
            Vector2 toPlayer = (Vector2)player.position - pos;
            float dist = toPlayer.magnitude;
            if (dist < Tuning.CrystalPickupRadius)
            {
                game.CollectCrystal(value);
                Destroy(gameObject);
                return;
            }
            // Close crystals are pulled toward the player.
            if (dist < Tuning.CrystalMagnetRadius)
                velocity += toPlayer / dist * Tuning.CrystalMagnetAccel * Time.deltaTime;
        }
        transform.position = pos + velocity * Time.deltaTime;

        // Blink before disappearing.
        float left = Tuning.CrystalLifetime - age;
        float a = 1f;
        if (left < Tuning.CrystalBlinkTime) a = Mathf.Repeat(left * 8f, 1f) < 0.5f ? 0.25f : 1f;
        core.color = new Color(coreColor.r, coreColor.g, coreColor.b, coreColor.a * a);
        glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * a);
    }
}
