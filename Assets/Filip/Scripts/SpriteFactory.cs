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

    public static Sprite Circle()
    {
        if (cache.TryGetValue("circle", out var s) && s != null) return s;
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float r = size * 0.5f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x + 0.5f - r, dy = y + 0.5f - r;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(r - d); // 1px soft edge
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        s.name = "circle";
        cache["circle"] = s;
        return s;
    }

    public static Sprite Square()
    {
        if (cache.TryGetValue("square", out var s) && s != null) return s;
        const int size = 4;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;
        var pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(pixels);
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        s.name = "square";
        cache["square"] = s;
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
