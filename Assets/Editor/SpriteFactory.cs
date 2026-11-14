using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Editor only: simple shape sprites (circle, glow, ring, triangle, square) saved as PNG
// files in Assets/Art/Shapes, so prefabs can reference them. Every sprite is
// 1x1 unit; size in the world is set with transform.localScale.
// Also the small pixel icons of the interface (icon_tank, icon_drop, icon_arrow).
public static class SpriteFactory
{
    public const string Folder = "Assets/Art/Shapes";

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

    // Soft round glow: bright in the middle, fading to nothing at the edge.
    public static Sprite Glow()
    {
        return Build("glow", 64, (x, y, size) =>
        {
            float r = size * 0.5f;
            float d = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r)) / r;
            float a = Mathf.Clamp01(1f - d);
            return a * a;
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

    // A pixel icon for the interface, drawn from rows of '#' (see PixelFont.O2).
    // Saved as icon_<key>.png with hard pixels; one icon pixel = one sprite pixel.
    public static Sprite PixelIcon(string key, string[] rows)
    {
        string path = Folder + "/icon_" + key + ".png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        Directory.CreateDirectory(Folder);
        int h = rows.Length, w = rows[0].Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            pixels[(h - 1 - y) * w + x] = rows[y][x] == '#'
                ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 1f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // alpha(x, y, size) returns 0..1 for the pixel center (x, y).
    // The PNG is written once; later calls load the existing asset.
    static Sprite Build(string key, int size, Func<float, float, float, float> alpha)
    {
        string path = Folder + "/" + key + ".png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        Directory.CreateDirectory(Folder);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float a = Mathf.Clamp01(alpha(x + 0.5f, y + 0.5f, size));
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = size;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = size <= 4 ? FilterMode.Point : FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
