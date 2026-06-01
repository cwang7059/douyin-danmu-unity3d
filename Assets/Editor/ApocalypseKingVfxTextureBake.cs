#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Bakes procedural VFX textures into Resources so standalone builds do not depend on runtime generation only.</summary>
public static class ApocalypseKingVfxTextureBake
{
    private const string OutputFolder = "Assets/Resources/VFX/Online/Selected";

    private static readonly string[] TextureNames =
    {
        "smoke_white",
        "smoke_black",
        "flash_kenney",
        "muzzle_rifle",
        "muzzle_tank",
        "explosion_kenney",
        "explosion_fireball",
        "explosion_bomb",
        "explosion_sinestesia_small",
        "explosion_sinestesia_large",
        "explosion_sinestesia_bomb",
        "shockwave_ring",
    };

    [MenuItem("Apocalypse King/Bake VFX Textures to Resources")]
    public static void BakeFromMenu()
    {
        int written = BakeAll();
        AssetDatabase.Refresh();
        Debug.Log($"[ApocalypseKing] Baked {written} VFX textures to {OutputFolder}. Rebuild player to ship them.");
    }

    public static int BakeAll()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources/VFX/Online/Selected"))
        {
            CreateFolderRecursive("Assets/Resources/VFX/Online");
            AssetDatabase.CreateFolder("Assets/Resources/VFX/Online", "Selected");
        }

        int written = 0;
        for (int i = 0; i < TextureNames.Length; i++)
        {
            string name = TextureNames[i];
            string resourcePath = "VFX/Online/Selected/" + name;
            Texture2D texture = BattleVfxProceduralTextures.Resolve(resourcePath);
            if (texture == null)
            {
                continue;
            }

            string path = OutputFolder + "/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            written++;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }

        return written;
    }

    private static void CreateFolderRecursive(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            CreateFolderRecursive(parent);
        }

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
