#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ApocalypseKingNuclearWarheadSetup
{
    private const string NuclearFolder = "Assets/Resources/Nuclear";
    private const string CruiseGlbPath = NuclearFolder + "/CruiseMissile.glb";
    private const string TomahawkGlbPath = NuclearFolder + "/TomahawkMissile.glb";
    private const string FallbackFbxPath = NuclearFolder + "/TacticalMissile.fbx";
    private const string CruisePrefabPath = NuclearFolder + "/CruiseMissilePrefab.prefab";
    private const string LegacyPrefabPath = NuclearFolder + "/TacticalMissilePrefab.prefab";

    [MenuItem("Apocalypse King/Bake Nuclear Missile Prefab")]
    public static void BakeNuclearMissilePrefabMenu()
    {
        if (BakeNuclearMissilePrefab())
        {
            Debug.Log("[ApocalypseKing] Nuclear missile prefab bake complete.");
        }
        else
        {
            Debug.LogWarning("[ApocalypseKing] Nuclear missile prefab bake failed. Run tools/import-nuclear-warhead.ps1 first.");
        }
    }

    public static void EnsureNuclearMissilePrefabForBatchMode()
    {
        BakeNuclearMissilePrefab();
    }

    public static bool BakeNuclearMissilePrefab()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        bool bakedCruise = TryBakePrefab(CruiseGlbPath, CruisePrefabPath, "CruiseMissile")
            || TryBakePrefab(TomahawkGlbPath, CruisePrefabPath, "CruiseMissile");
        bool bakedLegacy = TryBakePrefab(FallbackFbxPath, LegacyPrefabPath, "TacticalMissile");
        return bakedCruise || bakedLegacy;
    }

    private static bool TryBakePrefab(string assetPath, string prefabPath, string instanceName)
    {
        if (!File.Exists(assetPath))
        {
            return false;
        }

        GameObject source = PickBestModelRoot(assetPath);
        if (source == null)
        {
            return false;
        }

        if (!Directory.Exists(NuclearFolder))
        {
            Directory.CreateDirectory(NuclearFolder);
        }

        var instance = Object.Instantiate(source);
        instance.name = instanceName;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
        Debug.Log($"[ApocalypseKing] Baked {prefabPath} from {assetPath}");
        return true;
    }

    private static GameObject PickBestModelRoot(string assetPath)
    {
        if (!File.Exists(assetPath))
        {
            return null;
        }

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
