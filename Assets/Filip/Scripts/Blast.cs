using UnityEngine;

// The flash of an explosion: an orange glow that swells to the blast radius and fades.
public class Blast : MonoBehaviour
{
    const float Duration = 0.35f;

    SpriteRenderer outer, core;
    float radius;
    float age;

    public static void Create(Vector2 position, float radius, Transform parent)
    {
        var go = new GameObject("Blast");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        var b = go.AddComponent<Blast>();
        b.radius = radius;
        b.outer = SpriteFactory.MakeSprite("Outer", go.transform, SpriteFactory.Glow(),
            new Color(1f, 0.45f, 0.15f, 0.9f), Vector2.one, 25);
        b.core = SpriteFactory.MakeSprite("Core", go.transform, SpriteFactory.Glow(),
            new Color(1f, 0.95f, 0.8f, 1f), Vector2.one, 26);
        b.Apply(0f);
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= Duration)
        {
            Destroy(gameObject);
            return;
        }
        Apply(age / Duration);
    }

    // t: 0 = just exploded, 1 = gone.
    void Apply(float t)
    {
        float grow = 1f - (1f - t) * (1f - t); // fast at first, then slower
        float size = radius * 2f * (0.25f + 0.75f * grow);
        outer.transform.localScale = new Vector3(size * 1.3f, size * 1.3f, 1f);
        core.transform.localScale = new Vector3(size * 0.6f, size * 0.6f, 1f);
        outer.color = new Color(1f, 0.45f, 0.15f, 0.9f * (1f - t));
        core.color = new Color(1f, 0.95f, 0.8f, (1f - t) * (1f - t));
    }
}
