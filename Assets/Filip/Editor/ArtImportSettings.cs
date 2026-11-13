using System.IO;
using UnityEditor;

// Every picture put into Assets/Resources/Art is imported as a Sprite with
// 100 pixels per unit. The picture named "background" also gets Max Size 4096.
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

        if (Path.GetFileNameWithoutExtension(path).ToLowerInvariant() == "background")
            importer.maxTextureSize = 4096;
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
