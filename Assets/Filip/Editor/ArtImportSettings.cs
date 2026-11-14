using System.IO;
using UnityEditor;
using UnityEngine;

// Every picture put into Assets/Resources/Art is imported as a Sprite with
// 100 pixels per unit. The picture named "background" also gets Max Size 4096.
// Seamless cloud pictures (nebula_...) repeat without visible seams.
// This runs automatically whenever Unity imports a picture from that folder.
public class ArtImportSettings : AssetPostprocessor
{
    const string ArtFolder = "Assets/Resources/Art/";
    const string LevelsFolder = "Assets/Resources/Levels/";

    void OnPreprocessTexture()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.StartsWith(ArtFolder)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;

        string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        if (name == "background")
            importer.maxTextureSize = 4096;

        if (name == "background" || name.StartsWith("nebula") || name.StartsWith("backdrop"))
        {
            // Smooth dark gradients look blocky with normal compression.
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            // Whole rectangle instead of a mesh cut around the opaque pixels.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
        if (name.StartsWith("nebula"))
            importer.wrapMode = TextureWrapMode.Repeat; // edges blend into the next copy

        if (name.StartsWith("asteroid"))
            importer.textureCompression = TextureImporterCompression.CompressedHQ; // keep the rock relief clean

        if (name.StartsWith("star"))
        {
            // Small pictures with soft glow: keep them exact and do not cut the faint halo off.
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }

    // When pictures are added, changed or removed, refresh the level preview.
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (Touches(imported) || Touches(deleted) || Touches(moved) || Touches(movedFrom))
            DraftPreview.Schedule();
    }

    // Pictures or level files changed?
    static bool Touches(string[] paths)
    {
        foreach (var p in paths)
        {
            string path = p.Replace('\\', '/');
            if (path.StartsWith(ArtFolder) || path.StartsWith(LevelsFolder)) return true;
        }
        return false;
    }
}
