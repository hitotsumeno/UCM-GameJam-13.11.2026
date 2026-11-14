using System.IO;
using UnityEditor;
using UnityEngine;

// Every picture put into Assets/_Resources/Art is imported as a Sprite with
// 100 pixels per unit. The picture named "background" also gets Max Size 4096.
// Seamless cloud pictures (nebula_...) repeat without visible seams.
// This runs automatically whenever Unity imports a picture from that folder.
//
// 3D models in Assets/Art/Source (stone.fbx, stars.fbx) are only a source for those
// pictures (Filip > Bake Rocks And Stars From FBX): they are imported as plain meshes,
// without materials, cameras, lights or animation, and are not part of the build.
public class ArtImportSettings : AssetPostprocessor
{
    const string ArtFolder = "Assets/_Resources/Art/";
    public const string ModelFolder = "Assets/Art/Source/";

    void OnPreprocessModel()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.StartsWith(ModelFolder)) return;

        var importer = (ModelImporter)assetImporter;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importVisibility = false;
        importer.importBlendShapes = false;
        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.importNormals = ModelImporterNormals.Import; // the artist's smoothing, as in the old bake
        importer.isReadable = true;                           // the bake reads the vertices
        importer.meshCompression = ModelImporterMeshCompression.Off;
        importer.optimizeMeshVertices = false;
        importer.optimizeMeshPolygons = false;
        importer.weldVertices = false;
        importer.bakeAxisConversion = false;                  // vertices stay as in the file (only X mirrored)
    }

    static bool IsSlicedSheet(TextureImporter importer)
    {
#pragma warning disable 618 // the old spritesheet list is enough to see whether frames exist
        return importer.spriteImportMode == SpriteImportMode.Multiple
            && importer.spritesheet != null && importer.spritesheet.Length > 0;
#pragma warning restore 618
    }

    void OnPreprocessTexture()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.StartsWith(ArtFolder)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        // A sprite sheet already sliced in the Sprite Editor (Sprite Mode: Multiple, with frames)
        // stays as it is; everything else is one picture.
        if (!IsSlicedSheet(importer)) importer.spriteImportMode = SpriteImportMode.Single;
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

        if (name.StartsWith("storm"))
        {
            // The storm wall is drawn tiled along the level edges: whole rectangles, clean edges.
            // Body and glow are 24 units wide (24 px per unit); bolts use the usual 100.
            if (name == "storm_body" || name == "storm_glow")
            {
                importer.spritePixelsPerUnit = 24f;
                importer.wrapMode = TextureWrapMode.Repeat;
            }
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }

        if (name == "ship" || name.StartsWith("player_ship"))
        {
            // Big renders (2028 px) drawn small: smaller copies keep them clean, the alpha outline
            // becomes the physics shape that DraftSetup turns into the collider.
            importer.mipmapEnabled = true;
            importer.maxTextureSize = name == "ship" ? 1024 : 512;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteGenerateFallbackPhysicsShape = true;
            importer.SetTextureSettings(settings);
        }

        if (name.StartsWith("fire"))
        {
            // Meteor tail: drawn at about 7 units, its outline gives the base of the flame.
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteGenerateFallbackPhysicsShape = true;
            importer.SetTextureSettings(settings);
        }

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
}
