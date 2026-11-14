using UnityEngine;

// A layer of stars that scrolls on screen at a fraction of the world speed.
// Stars repeat (wrap around the camera), so the layer never runs out.
// Two kinds of layers:
//  - dust: lots of tiny dim dots;
//  - glowing stars: a bright core (pictures star1, star2, ...) over a soft coloured
//    halo (star_glow1, star_glow2, ...), tinted in the nebula colours, slowly twinkling.
[DefaultExecutionOrder(100)] // after CameraRig has moved the camera
public class StarLayer : MonoBehaviour
{
    // The star shape takes this share of its picture; the rest is room for the glow.
    const float ShapeFraction = 0.42f;

    // Halo colours, picked to match the nebula; the last two are the artist's gold and plain white.
    static readonly Color[] Palette =
    {
        new Color(0.65f, 0.82f, 1.00f), // ice blue
        new Color(0.55f, 0.95f, 1.00f), // cyan
        new Color(0.78f, 0.65f, 1.00f), // violet
        new Color(1.00f, 0.68f, 0.88f), // pink
        new Color(1.00f, 0.82f, 0.50f), // gold
        new Color(1.00f, 1.00f, 1.00f), // white
    };

    Vector2 anchor;
    float factor;
    float tile;
    Transform[] stars;
    Vector2[] local;   // star positions inside the tile
    Camera cam;

    // Twinkle (glowing stars only).
    SpriteRenderer[] cores, glows;
    Color[] coreColors, glowColors;
    float[] phase, speed;

    // Tiny dim dots.
    // levelColor: colour of the stars on this level (white = as usual).
    public static StarLayer CreateDust(string name, Transform parent, Vector2 anchor, float factor, int count,
        int sortingOrder, Color levelColor)
    {
        var layer = Begin(name, parent, anchor, factor, count);
        var circle = SpriteFactory.Circle();
        for (int i = 0; i < count; i++)
        {
            var color = new Color(0.84f * levelColor.r, 0.9f * levelColor.g, levelColor.b, Random.Range(0.18f, 0.5f));
            var sr = SpriteFactory.MakeSprite("Dust", layer.transform, circle, color,
                Vector2.one * Random.Range(0.03f, 0.07f), sortingOrder);
            layer.stars[i] = sr.transform;
        }
        layer.Place(anchor);
        return layer;
    }

    // Glowing stars. minSize/maxSize: size of the star shape itself, in units.
    public static StarLayer CreateGlowing(string name, Transform parent, Vector2 anchor, float factor, int count,
        float minSize, float maxSize, float minBrightness, int sortingOrder, Color levelColor)
    {
        var layer = Begin(name, parent, anchor, factor, count);
        layer.cores = new SpriteRenderer[count];
        layer.glows = new SpriteRenderer[count];
        layer.coreColors = new Color[count];
        layer.glowColors = new Color[count];
        layer.phase = new float[count];
        layer.speed = new float[count];

        for (int i = 0; i < count; i++)
        {
            var star = new GameObject("Star").transform;
            star.SetParent(layer.transform, false);
            layer.stars[i] = star;

            float size = Random.Range(minSize, maxSize);
            float brightness = Random.Range(minBrightness, 1f);
            Color tint = Palette[Random.Range(0, Palette.Length)] * levelColor;

            SpriteRenderer core, glow;
            var shape = Art.Load("star");
            if (shape != null)
            {
                // Core and glow pictures share one canvas, so both get the same size.
                float canvas = size / ShapeFraction;
                var glowSprite = Art.LoadExact(shape.name.Replace("star", "star_glow"));
                glow = glowSprite != null
                    ? MakePart("Glow", star, glowSprite, canvas, sortingOrder)
                    : MakePart("Glow", star, SpriteFactory.Glow(), canvas, sortingOrder);
                core = MakePart("Core", star, shape, canvas, sortingOrder + 1);
            }
            else
            {
                // Placeholder: a dot with a round halo.
                glow = MakePart("Glow", star, SpriteFactory.Glow(), size * 2.4f, sortingOrder);
                core = MakePart("Core", star, SpriteFactory.Circle(), size * 0.4f, sortingOrder + 1);
            }

            Color coreColor = Color.Lerp(Color.white, tint, 0.25f);
            coreColor.a = brightness * Tuning.StarBrightness;
            Color glowColor = tint;
            glowColor.a = brightness * Tuning.StarGlowOpacity;
            core.color = coreColor;
            glow.color = glowColor;

            layer.cores[i] = core;
            layer.glows[i] = glow;
            layer.coreColors[i] = coreColor;
            layer.glowColors[i] = glowColor;
            layer.phase[i] = Random.Range(0f, Mathf.PI * 2f);
            layer.speed[i] = Random.Range(Tuning.StarTwinkleSpeedMin, Tuning.StarTwinkleSpeedMax);
        }
        layer.Place(anchor);
        return layer;
    }

    static StarLayer Begin(string name, Transform parent, Vector2 anchor, float factor, int count)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var layer = go.AddComponent<StarLayer>();
        layer.anchor = anchor;
        layer.factor = factor;
        layer.tile = Tuning.StarTileSize;
        layer.stars = new Transform[count];
        layer.local = new Vector2[count];
        float half = layer.tile * 0.5f;
        for (int i = 0; i < count; i++)
            layer.local[i] = new Vector2(Random.Range(-half, half), Random.Range(-half, half));
        return layer;
    }

    // Child sprite scaled so that its larger side is `size` units.
    static SpriteRenderer MakePart(string name, Transform parent, Sprite sprite, float size, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Vector2 b = sprite.bounds.size;
        float scale = size / Mathf.Max(b.x, b.y);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        Place(cam.transform.position);
        if (cores != null) Twinkle();
    }

    void Place(Vector2 camPos)
    {
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
