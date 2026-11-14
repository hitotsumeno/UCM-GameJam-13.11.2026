using UnityEngine;

// One puff of escaping air: flies off, grows and fades.
public class LeakPuff : MonoBehaviour
{
    Vector2 velocity;
    float age;
    SpriteRenderer sr;

    public static void Create(Vector2 position, Vector2 velocity, Transform parent)
    {
        var go = new GameObject("AirPuff");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var puff = go.AddComponent<LeakPuff>();
        puff.velocity = velocity;
        puff.sr = SpriteFactory.MakeSprite("Puff", go.transform, SpriteFactory.Glow(),
            new Color(0.85f, 0.97f, 1f, 0.7f), Vector2.one * 0.25f, 9);
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = age / Tuning.LeakPuffLifetime;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += (Vector3)(velocity * Time.deltaTime);
        velocity *= Mathf.Exp(-1.5f * Time.deltaTime);
        float size = Mathf.Lerp(0.25f, 0.9f, t);
        sr.transform.localScale = new Vector3(size, size, 1f);
        sr.color = new Color(0.85f, 0.97f, 1f, 0.7f * (1f - t));
    }
}
