using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Editor only: finds the artist's pictures in Assets/_Resources/Art/<name> while the
// prefabs are made (Filip > Rebuild Prefabs And Scene). If a file is missing the caller
// draws a placeholder instead. Size in the game is set by code, not by the picture.
// Variants: a number at the end of the name (asteroid_m1, asteroid_m2, ...).
public static class Art
{
    const string Folder = "Assets/_Resources/Art/";
    static readonly string[] Extensions = { ".png", ".jpg", ".jpeg", ".psd", ".tga" };

    // The picture called <name> and all of <name>1 .. <name>9 that exist.
    public static List<Sprite> LoadAll(string name)
    {
        var list = new List<Sprite>();
        AddIfExists(list, name);
        for (int i = 1; i <= 9; i++) AddIfExists(list, name + i);
        return list;
    }

    // A random one of LoadAll(name). Null if there is none.
    public static Sprite Load(string name)
    {
        var list = LoadAll(name);
        if (list.Count == 0) return null;
        return list[Random.Range(0, list.Count)];
    }

    // The picture with exactly this name (no variants). Null if there is none.
    public static Sprite LoadExact(string name)
    {
        foreach (var ext in Extensions)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + name + ext);
            if (s != null) return s;
        }
        return null;
    }

    static void AddIfExists(List<Sprite> list, string name)
    {
        var s = LoadExact(name);
        if (s != null) list.Add(s);
    }

    // Creates a child object showing the picture, scaled so that its larger side is `size` units.
    // Returns null if there is no such picture (then draw a placeholder).
    public static SpriteRenderer TryMake(string name, Transform parent, float size, int sortingOrder)
    {
        var sprite = Load(name);
        if (sprite == null) return null;
        return Make(name, sprite, parent, size, sortingOrder);
    }

    public static SpriteRenderer Make(string objectName, Sprite sprite, Transform parent, float size, int sortingOrder)
    {
        var go = new GameObject(objectName);
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
