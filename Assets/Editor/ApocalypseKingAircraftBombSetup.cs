#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ApocalypseKingAircraftBombSetup
{
    private const string BombFolder = "Assets/Resources/AircraftBomb";
    private const string ObjPath = BombFolder + "/Mk82Bomb.obj";
    private const string PrefabPath = BombFolder + "/Mk82BombPrefab.prefab";

    [MenuItem("Apocalypse King/Bake Aircraft Bomb Prefab")]
    public static void BakeAircraftBombPrefabMenu()
    {
        if (BakeAircraftBombPrefab())
        {
            Debug.Log("[ApocalypseKing] Aircraft bomb prefab bake complete.");
        }
        else
        {
            Debug.LogWarning("[ApocalypseKing] Aircraft bomb prefab bake failed. Run tools/import-aircraft-bomb.ps1 first.");
        }
    }

    public static void EnsureAircraftBombPrefabForBatchMode()
    {
        BakeAircraftBombPrefab();
    }

    public static bool BakeAircraftBombPrefab()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        if (!File.Exists(ObjPath))
        {
            return false;
        }

        var importer = AssetImporter.GetAtPath(ObjPath) as ModelImporter;
        if (importer != null)
        {
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
        }

        GameObject source = PickBestModelRoot(ObjPath);
        if (source == null)
        {
            return false;
        }

        if (!Directory.Exists(BombFolder))
        {
            Directory.CreateDirectory(BombFolder);
        }

        GameObject instance = Object.Instantiate(source);
        instance.name = "Mk82Bomb";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        Object.DestroyImmediate(instance);
        Debug.Log($"[ApocalypseKing] Baked {PrefabPath} from {ObjPath}");
        return true;
    }

    private static GameObject PickBestModelRoot(string assetPath)
    {
        GameObject direct = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (direct != null && direct.GetComponentsInChildren<Renderer>(true).Length > 0)
        {
            return direct;
        }

        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        GameObject best = null;
        int bestRendererCount = 0;
        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] is not GameObject gameObject || gameObject.name.StartsWith("__", System.StringComparison.Ordinal))
            {
                continue;
            }

            int rendererCount = gameObject.GetComponentsInChildren<Renderer>(true).Length;
            if (rendererCount > bestRendererCount)
            {
                best = gameObject;
                bestRendererCount = rendererCount;
            }
        }

        return best;
    }
}
#endif
