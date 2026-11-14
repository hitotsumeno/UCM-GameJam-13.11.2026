using UnityEngine;

// A layer of stars that scrolls on screen at a fraction of the world speed.
// Stars repeat (wrap around the camera), so the layer never runs out.
// The stars are child objects made in the editor (Filip > Rebuild Prefabs And Scene);
// this component only moves them and, for glowing stars, makes them twinkle.
// Part of Assets/Prefabs/Background.prefab.
[DefaultExecutionOrder(100)] // after CameraRig has moved the camera
public class StarLayer : MonoBehaviour
{
    [SerializeField] Vector2 anchor;
    [SerializeField] float factor = 0.3f;
    [SerializeField] float tile = Tuning.StarTileSize;
    [SerializeField] Transform[] stars;
    [Tooltip("Star positions inside the tile")]
    [SerializeField] Vector2[] local;

    [Header("Twinkle (glowing stars only, leave empty for dust)")]
    [SerializeField] SpriteRenderer[] cores;
    [SerializeField] SpriteRenderer[] glows;
    [SerializeField] float[] phase;
    [SerializeField] float[] speed;

    Color[] coreColors, glowColors;
    Camera cam;

    public void Setup(Vector2 anchor, float factor, float tile, Transform[] stars, Vector2[] local,
        SpriteRenderer[] cores, SpriteRenderer[] glows, float[] phase, float[] speed)
    {
        this.anchor = anchor;
        this.factor = factor;
        this.tile = tile;
        this.stars = stars;
        this.local = local;
        this.cores = cores;
        this.glows = glows;
        this.phase = phase;
        this.speed = speed;
        Place(anchor);
    }

    void Awake()
    {
        if (cores == null || cores.Length == 0) return;
        coreColors = new Color[cores.Length];
        glowColors = new Color[glows.Length];
        for (int i = 0; i < cores.Length; i++) coreColors[i] = cores[i].color;
        for (int i = 0; i < glows.Length; i++) glowColors[i] = glows[i].color;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        Place(cam.transform.position);
        if (coreColors != null) Twinkle();
    }

    void Place(Vector2 camPos)
    {
        if (stars == null) return;
        Vector2 scroll = (camPos - anchor) * factor;
        float half = tile * 0.5f;
        for (int i = 0; i < stars.Length; i++)
        {
            Vector2 d = local[i] - scroll;
            d.x = Mathf.Repeat(d.x + half, tile) - half;
            d.y = Mathf.Repeat(d.y + half, tile) - half;
            stars[i].position = camPos + d;
        }
    }

    // Each star slowly breathes: the halo swells and fades, the core dims a little.
    void Twinkle()
    {
        float t = Time.time;
        float strength = Tuning.StarTwinkleStrength;
        for (int i = 0; i < cores.Length; i++)
        {
            float k = 0.5f + 0.5f * Mathf.Sin(t * speed[i] + phase[i]); // 0..1

            Color c = coreColors[i];
            c.a *= 1f - 0.5f * strength * (1f - k);
            cores[i].color = c;

            Color g = glowColors[i];
            g.a *= 1f - strength * (1f - k);
            glows[i].color = g;

            float s = 1f + 0.15f * strength * k;
            stars[i].localScale = new Vector3(s, s, 1f);
        }
    }
}
