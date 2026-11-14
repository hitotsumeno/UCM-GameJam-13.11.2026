using UnityEngine;

// The flash of an explosion: an orange glow that swells to the blast radius and fades.
// Set up in Assets/Prefabs/Blast.prefab.
public class Blast : MonoBehaviour
{
    const float Duration = 0.35f;

    [SerializeField] SpriteRenderer outer;
    [SerializeField] SpriteRenderer core;

    float radius = 1f;
    float age;
    Color outerColor, coreColor;

    public static void Spawn(Blast prefab, Vector2 position, float radius, Transform parent)
    {
        if (prefab == null) return;
        var b = Instantiate(prefab, position, Quaternion.identity, parent);
        b.radius = radius;
        b.Apply(0f);
    }

    void Awake()
    {
        outerColor = outer.color;
        coreColor = core.color;
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
        outer.color = new Color(outerColor.r, outerColor.g, outerColor.b, outerColor.a * (1f - t));
        core.color = new Color(coreColor.r, coreColor.g, coreColor.b, coreColor.a * (1f - t) * (1f - t));
    }
}
