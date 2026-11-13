using System.Collections.Generic;
using UnityEngine;

// Loads pictures from Assets/Resources/Art/<name>. If the file is missing the caller
// draws a placeholder instead. Size in the game is set by code, not by the picture.
// Variants: a number at the end of the name (asteroid_m1, asteroid_m2, ...);
// a random one of the found files is used.
public static class Art
{
    static Dictionary<string, List<Sprite>> cache = new Dictionary<string, List<Sprite>>();

    public static void ResetStatics()
    {
        cache = new Dictionary<string, List<Sprite>>();
    }

    // Returns the picture called <name> or a random one of <name>1 .. <name>9. Null if there is none.
    public static Sprite Load(string name)
    {
        if (!cache.TryGetValue(name, out var list))
        {
            list = new List<Sprite>();
            AddIfExists(list, name);
            for (int i = 1; i <= 9; i++) AddIfExists(list, name + i);
            cache[name] = list;
        }
        list.RemoveAll(s => s == null);
        if (list.Count == 0) return null;
        return list[Random.Range(0, list.Count)];
    }

    static void AddIfExists(List<Sprite> list, string name)
    {
        var s = Resources.Load<Sprite>("Art/" + name);
        if (s != null) list.Add(s);
    }

    // Creates a child object showing the picture, scaled so that its larger side is `size` units.
    // Returns null if there is no such picture (then draw a placeholder).
    public static SpriteRenderer TryMake(string name, Transform parent, float size, int sortingOrder)
    {
        var sprite = Load(name);
        if (sprite == null) return null;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Vector2 b = sprite.bounds.size;
        float larger = Mathf.Max(b.x, b.y);
        float scale = larger > 0f ? size / larger : 1f;
        go.transform.localScale = new Vector3(scale, scale, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    // Size of the picture in the world after scaling.
    public static Vector2 WorldSize(SpriteRenderer sr)
    {
        return Vector2.Scale(sr.sprite.bounds.size, sr.transform.localScale);
    }
}
