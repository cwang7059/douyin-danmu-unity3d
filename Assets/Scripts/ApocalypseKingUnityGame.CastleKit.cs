using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class ApocalypseKingUnityGame
{
    private const string CastleKitResourceFolderPath = "Kenney/CastleKit";
    private const string CastleKitPrefabFolderPath = CastleKitResourceFolderPath + "/Prefabs";

    private readonly Dictionary<string, GameObject> castleKitPrefabCache =
        new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

    private int castleKitPrefabCount;

    private void CacheCastleKitPrefabs()
    {
        if (castleKitPrefabCount > 0)
        {
            return;
        }

        RegisterCastleKitResourcesFromFolder(CastleKitPrefabFolderPath);
        RegisterCastleKitResourcesFromFolder(CastleKitResourceFolderPath);
    }

    private void RegisterCastleKitResourcesFromFolder(string folderPath)
    {
        UnityEngine.Object[] assets = Resources.LoadAll(folderPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is GameObject gameObject)
            {
                RegisterCastleKitPrefab(gameObject);
            }
        }
    }

    private void RegisterCastleKitPrefab(GameObject prefab)
    {
        if (prefab == null || string.IsNullOrEmpty(prefab.name))
        {
            return;
        }

        if (!castleKitPrefabCache.ContainsKey(prefab.name))
        {
            castleKitPrefabCache[prefab.name] = prefab;
            castleKitPrefabCount++;
        }
    }

    private GameObject LoadCastleKitPrefab(string assetName)
    {
        CacheCastleKitPrefabs();
        if (castleKitPrefabCache.TryGetValue(assetName, out GameObject cached) && cached != null)
        {
            return cached;
        }

        GameObject prefab = Resources.Load<GameObject>(CastleKitPrefabFolderPath + "/" + assetName);
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>(CastleKitResourceFolderPath + "/" + assetName);
        }

        if (prefab != null)
        {
            castleKitPrefabCache[assetName] = prefab;
            castleKitPrefabCount++;
        }

        return prefab;
    }

    private bool HasKenneyCastleAssets()
    {
        return LoadCastleKitPrefab("wall") != null
            && LoadCastleKitPrefab("metal-gate") != null
            && LoadCastleKitPrefab("tower-square-top-roof-high-windows") != null;
    }

    private GameObject CreateCastleKitModule(string assetName, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        var prefab = LoadCastleKitPrefab(assetName);
        if (prefab == null)
        {
            return null;
        }

        var instance = Instantiate(prefab, parent, false);
        instance.name = assetName;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = localRotation;
        instance.transform.localScale = localScale;
        ConfigureCastleKitInstance(instance);
        return instance;
    }

    private void ConfigureCastleKitInstance(GameObject instance)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;

            var materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                ApplyOpaqueDoubleSided(materials[m]);
            }
        }
    }
}
