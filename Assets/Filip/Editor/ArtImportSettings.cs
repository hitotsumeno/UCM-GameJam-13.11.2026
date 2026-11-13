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

        if (name == "background" || name.StartsWith("nebula"))
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
    }

    // When pictures are added, changed or removed, refresh the level preview.
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (Touches(imported) || Touches(deleted) || Touches(moved) || Touches(movedFrom))
            DraftPreview.Schedule();
    }

    static bool Touches(string[] paths)
    {
        foreach (var p in paths)
            if (p.Replace('\\', '/').StartsWith(ArtFolder)) return true;
        return false;
    }
}
