using System;
using System.Collections.Generic;
using UnityEngine;

// Generates placeholder sprites in code. Every sprite is 1x1 unit;
// size in the world is set with transform.localScale.
public static class SpriteFactory
{
    static Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static void ResetStatics()
    {
        cache = new Dictionary<string, Sprite>();
    }

    // Filled circle.
    public static Sprite Circle()
    {
        return Build("circle", 64, (x, y, size) =>
        {
            float r = size * 0.5f;
            float d = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r));
            return Mathf.Clamp01(r - d);
        });
    }

    // Thin circle outline.
    public static Sprite Ring()
    {
        return Build("ring", 128, (x, y, size) =>
        {
            float r = size * 0.5f;
            float d = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r));
            return Mathf.Clamp01(r - d) * Mathf.Clamp01(d - r * 0.92f);
        });
    }

    // Triangle pointing up (+Y).
    public static Sprite Triangle()
    {
        return Build("triangle", 64, (x, y, size) =>
        {
            float c = size * 0.5f;
            float halfWidth = (1f - y / size) * c;
            return Mathf.Clamp01(halfWidth - Mathf.Abs(x - c));
        });
    }

    // Solid square.
    public static Sprite Square()
    {
        return Build("square", 4, (x, y, size) => 1f);
    }

    // alpha(x, y, size) returns 0..1 for the pixel center (x, y).
    static Sprite Build(string key, int size, Func<float, float, float, float> alpha)
    {
        if (cache.TryGetValue(key, out var s) && s != null) return s;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = size <= 4 ? FilterMode.Point : FilterMode.Bilinear;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float a = Mathf.Clamp01(alpha(x + 0.5f, y + 0.5f, size));
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        s.name = key;
        cache[key] = s;
        return s;
    }

    // Helper: child object with a sprite, color, size and draw order.
    public static SpriteRenderer MakeSprite(string name, Transform parent, Sprite sprite,
        Color color, Vector2 size, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return sr;
    }
}
