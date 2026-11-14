using UnityEngine;

// Lightning inside the storm at the level edge: from time to time a bolt flickers in a part
// of the storm that is on screen. Made by Filip > Rebuild Prefabs And Scene
// (pictures: storm_bolt1..3.png); the colour is the storm's colour, close to white.
public class StormBolts : MonoBehaviour
{
    [Tooltip("The level area; bolts only appear outside it, inside the storm")]
    [SerializeField] Rect bounds;
    [SerializeField] SpriteRenderer[] bolts;
    [SerializeField] Sprite[] shapes;
    [SerializeField] Color color = Color.white;
    [SerializeField] float intervalMin = 1.2f;
    [SerializeField] float intervalMax = 3.5f;

    const float Life = 0.35f;
    float next;
    float[] age;

    public void Setup(Rect bounds, SpriteRenderer[] bolts, Sprite[] shapes, Color color)
    {
        this.bounds = bounds;
        this.bolts = bolts;
        this.shapes = shapes;
        this.color = color;
    }

    void Awake()
    {
        age = new float[bolts != null ? bolts.Length : 0];
        for (int i = 0; i < age.Length; i++) { age[i] = Life; if (bolts[i] != null) bolts[i].enabled = false; }
        next = Time.time + Random.Range(intervalMin, intervalMax);
    }

    void Update()
    {
        for (int i = 0; i < age.Length; i++)
        {
            if (age[i] >= Life || bolts[i] == null) continue;
            age[i] += Time.deltaTime;
            if (age[i] >= Life) { bolts[i].enabled = false; continue; }
            // Flash, a short dip, a second flash, then fade.
            float t = age[i] / Life;
            float a = t < 0.15f ? 1f : (t < 0.3f ? 0.25f : 1f - (t - 0.3f) / 0.7f);
            bolts[i].color = new Color(color.r, color.g, color.b, color.a * a);
        }

        if (Time.time < next || shapes == null || shapes.Length == 0) return;
        next = Time.time + Random.Range(intervalMin, intervalMax);
        Strike();
    }

    // A bolt in a random visible spot that is 2 to 16 units deep into the storm.
    void Strike()
    {
        var cam = Camera.main;
        if (cam == null) return;
        float h = cam.orthographicSize, w = h * cam.aspect;
        Vector2 c = cam.transform.position;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            var p = new Vector2(c.x + Random.Range(-w, w), c.y + Random.Range(-h, h));
            Vector2 inside = new Vector2(Mathf.Clamp(p.x, bounds.xMin, bounds.xMax), Mathf.Clamp(p.y, bounds.yMin, bounds.yMax));
            float depth = Vector2.Distance(p, inside);
            if (depth < 2f || depth > 16f) continue;

            int free = -1;
            for (int i = 0; i < age.Length; i++) if (age[i] >= Life && bolts[i] != null) { free = i; break; }
            if (free < 0) return;
            var sr = bolts[free];
            sr.sprite = shapes[Random.Range(0, shapes.Length)];
            sr.transform.position = p;
            sr.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            float s = Random.Range(0.9f, 1.6f);
            sr.transform.localScale = new Vector3(Random.value < 0.5f ? -s : s, s, 1f);
            sr.color = color;
            sr.enabled = true;
            age[free] = 0f;
            return;
        }
    }
}
