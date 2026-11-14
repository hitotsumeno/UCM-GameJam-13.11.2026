using UnityEngine;

// One puff of escaping air: flies off, grows and fades.
// Set up in Assets/Prefabs/AirPuff.prefab.
public class LeakPuff : MonoBehaviour
{
    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    [SerializeField] SpriteRenderer sprite;

    Vector2 velocity;
    float age;
    Color color;

    public static void Spawn(LeakPuff prefab, Vector2 position, Vector2 velocity, Transform parent)
    {
        var puff = Instantiate(prefab, position, Quaternion.identity, parent);
        puff.velocity = velocity;
    }

    void Awake()
    {
        color = sprite.color;
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = age / T.LeakPuffLifetime;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += (Vector3)(velocity * Time.deltaTime);
        velocity *= Mathf.Exp(-1.5f * Time.deltaTime);
        float size = Mathf.Lerp(0.25f, 0.9f, t);
        sprite.transform.localScale = new Vector3(size, size, 1f);
        sprite.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
    }
}
