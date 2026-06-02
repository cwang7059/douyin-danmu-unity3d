#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Pixelhouse FBX must import as Legacy so bone walk cycles drive the skinned mesh.
/// </summary>
public static class ApocalypseKingPixelhouseZombieImport
{
    private const string PixelhouseFolder = "Assets/Resources/RealisticZombies/Pixelhouse";

    [MenuItem("ApocalypseKing/Zombie/Reimport Pixelhouse As Legacy Animation")]
    public static void ReimportPixelhouseAsLegacy()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { PixelhouseFolder });
        int count = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                continue;
            }

            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
            count++;
        }

        Debug.Log($"[ApocalypseKing] Reimported {count} Pixelhouse FBX as Legacy animation.");
    }
}
#endif
