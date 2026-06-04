using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class ApocalypseKingUnityGame
{
    private const string CastleKitResourceFolderPath = "Kenney/CastleKit";
    private const string CastleKitPrefabFolderPath = CastleKitResourceFolderPath + "/Prefabs";
    private const string CastleKitColormapResourcePath = CastleKitResourceFolderPath + "/colormap";

    private readonly Dictionary<string, GameObject> castleKitPrefabCache =
        new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

    private int castleKitPrefabCount;
    private Material castleKitSharedMaterial;

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

    private static bool TryGetCastleModuleLocalBounds(GameObject instance, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        if (instance == null)
        {
            return false;
        }

        Transform root = instance.transform;
        MeshFilter[] meshFilters = instance.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            Mesh mesh = meshFilters[i].sharedMesh;
            if (mesh == null)
            {
                continue;
            }

            Bounds meshBounds = mesh.bounds;
            Vector3[] corners =
            {
                new Vector3(meshBounds.min.x, meshBounds.min.y, meshBounds.min.z),
                new Vector3(meshBounds.min.x, meshBounds.min.y, meshBounds.max.z),
                new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.min.z),
                new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.max.z),
                new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.min.z),
                new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.max.z),
                new Vector3(meshBounds.max.x, meshBounds.max.y, meshBounds.min.z),
                new Vector3(meshBounds.max.x, meshBounds.max.y, meshBounds.max.z),
            };

            Transform meshTransform = meshFilters[i].transform;
            for (int c = 0; c < corners.Length; c++)
            {
                Vector3 localCorner = root.InverseTransformPoint(meshTransform.TransformPoint(corners[c]));
                if (!hasBounds)
                {
                    bounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(localCorner);
                }
            }
        }

        return hasBounds;
    }

    private static bool IsAircraftFuselageMesh(Transform meshTransform)
    {
        if (meshTransform == null)
        {
            return false;
        }

        string name = meshTransform.name.ToLowerInvariant();
        if (name.Contains("propeller")
            || name.Contains("rotor")
            || name.Contains("missile")
            || string.Equals(name, "helicopterbase", StringComparison.OrdinalIgnoreCase)
            || name.Contains("camera")
            || (name.Contains("light") && meshTransform.GetComponent<Light>() != null))
        {
            return false;
        }

        if (name.Contains("prop") && !name.Contains("property"))
        {
            return false;
        }

        return true;
    }

    private static bool TryGetAircraftFuselageLocalBounds(GameObject instance, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        if (instance == null)
        {
            return false;
        }

        Transform root = instance.transform;
        MeshFilter[] meshFilters = instance.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];
            if (meshFilter == null || !IsAircraftFuselageMesh(meshFilter.transform))
            {
                continue;
            }

            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null)
            {
                continue;
            }

            Bounds meshBounds = mesh.bounds;
            Vector3[] corners =
            {
                new Vector3(meshBounds.min.x, meshBounds.min.y, meshBounds.min.z),
                new Vector3(meshBounds.min.x, meshBounds.min.y, meshBounds.max.z),
                new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.min.z),
                new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.max.z),
                new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.min.z),
                new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.max.z),
                new Vector3(meshBounds.max.x, meshBounds.max.y, meshBounds.min.z),
                new Vector3(meshBounds.max.x, meshBounds.max.y, meshBounds.max.z),
            };

            Transform meshTransform = meshFilter.transform;
            for (int c = 0; c < corners.Length; c++)
            {
                Vector3 localCorner = root.InverseTransformPoint(meshTransform.TransformPoint(corners[c]));
                if (!hasBounds)
                {
                    bounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(localCorner);
                }
            }
        }

        SkinnedMeshRenderer[] skinnedMeshes = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedMeshes.Length; i++)
        {
            SkinnedMeshRenderer skinned = skinnedMeshes[i];
            if (skinned == null
                || skinned.sharedMesh == null
                || !IsAircraftFuselageMesh(skinned.transform))
            {
                continue;
            }

            Bounds meshBounds = skinned.sharedMesh.bounds;
            Vector3[] corners =
            {
                new Vector3(meshBounds.min.x, meshBounds.min.y, meshBounds.min.z),
                new Vector3(meshBounds.min.x, meshBounds.min.y, meshBounds.max.z),
                new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.min.z),
                new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.max.z),
                new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.min.z),
                new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.max.z),
                new Vector3(meshBounds.max.x, meshBounds.max.y, meshBounds.min.z),
                new Vector3(meshBounds.max.x, meshBounds.max.y, meshBounds.max.z),
            };

            Transform meshTransform = skinned.transform;
            for (int c = 0; c < corners.Length; c++)
            {
                Vector3 localCorner = root.InverseTransformPoint(meshTransform.TransformPoint(corners[c]));
                if (!hasBounds)
                {
                    bounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(localCorner);
                }
            }
        }

        if (!hasBounds)
        {
            return TryGetCastleModuleLocalBounds(instance, out bounds);
        }

        return true;
    }

    private static bool IsPterosaurCruiseMesh(Transform meshTransform)
    {
        if (meshTransform == null)
        {
            return false;
        }

        string name = meshTransform.name.ToLowerInvariant();
        if (name.Contains("wing") || name.Contains("tail") || name.Contains("crest"))
        {
            return false;
        }

        return true;
    }

    private static bool TryGetPterosaurFuselageLocalBounds(GameObject instance, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        if (instance == null)
        {
            return false;
        }

        Transform root = instance.transform;
        MeshFilter[] meshFilters = instance.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];
            if (meshFilter == null || !IsPterosaurCruiseMesh(meshFilter.transform))
            {
                continue;
            }

            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null)
            {
                continue;
            }

            EncapsulateMeshCornersInRootLocal(root, meshFilter.transform, mesh.bounds, ref bounds, ref hasBounds);
        }

        SkinnedMeshRenderer[] skinnedMeshes = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedMeshes.Length; i++)
        {
            SkinnedMeshRenderer skinned = skinnedMeshes[i];
            if (skinned == null
                || skinned.sharedMesh == null
                || !IsPterosaurCruiseMesh(skinned.transform))
            {
                continue;
            }

            EncapsulateMeshCornersInRootLocal(root, skinned.transform, skinned.sharedMesh.bounds, ref bounds, ref hasBounds);
        }

        return hasBounds;
    }

    private static void EncapsulateMeshCornersInRootLocal(
        Transform root,
        Transform meshTransform,
        Bounds meshBounds,
        ref Bounds bounds,
        ref bool hasBounds)
    {
        Vector3[] corners =
        {
            new Vector3(meshBounds.min.x, meshBounds.min.y, meshBounds.min.z),
            new Vector3(meshBounds.min.x, meshBounds.min.y, meshBounds.max.z),
            new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.min.z),
            new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.max.z),
            new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.min.z),
            new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.max.z),
            new Vector3(meshBounds.max.x, meshBounds.max.y, meshBounds.min.z),
            new Vector3(meshBounds.max.x, meshBounds.max.y, meshBounds.max.z),
        };

        for (int c = 0; c < corners.Length; c++)
        {
            Vector3 localCorner = root.InverseTransformPoint(meshTransform.TransformPoint(corners[c]));
            if (!hasBounds)
            {
                bounds = new Bounds(localCorner, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(localCorner);
            }
        }
    }

    private static void AlignCastleKitModuleToFloor(GameObject instance, float floorLocalY)
    {
        if (!TryGetCastleModuleLocalBounds(instance, out Bounds bounds))
        {
            return;
        }

        Vector3 position = instance.transform.localPosition;
        position.y = floorLocalY - bounds.min.y;
        instance.transform.localPosition = position;
    }

    private static float GetCastleModuleStackTopY(GameObject instance, float floorLocalY)
    {
        if (!TryGetCastleModuleLocalBounds(instance, out Bounds bounds))
        {
            return floorLocalY;
        }

        return floorLocalY + bounds.size.y;
    }

    private GameObject PlaceCastleKitModuleOnFloor(
        string assetName,
        Transform parent,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        float floorLocalY = 0f)
    {
        GameObject instance = CreateCastleKitModule(assetName, parent, localPosition, localRotation, localScale);
        if (instance != null)
        {
            AlignCastleKitModuleToFloor(instance, floorLocalY);
        }

        return instance;
    }

    private float StackCastleKitModule(
        string assetName,
        Transform parent,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        float floorLocalY)
    {
        GameObject instance = PlaceCastleKitModuleOnFloor(assetName, parent, localPosition, localRotation, localScale, floorLocalY);
        if (instance == null)
        {
            return floorLocalY;
        }

        return GetCastleModuleStackTopY(instance, floorLocalY);
    }

    private Material GetCastleKitSharedMaterial()
    {
        if (castleKitSharedMaterial != null)
        {
            return castleKitSharedMaterial;
        }

        castleKitSharedMaterial = GetTexturedOpaqueMaterial(
            CastleKitColormapResourcePath,
            Color.white,
            Vector2.one,
            0.08f);
        return castleKitSharedMaterial;
    }

    private static bool CastleKitMaterialNeedsRemap(Material material)
    {
        if (material == null)
        {
            return true;
        }

        if (material.shader != null
            && material.shader.name.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        Texture mainTexture = null;
        if (material.HasProperty("_MainTex"))
        {
            mainTexture = material.GetTexture("_MainTex");
        }
        else if (material.HasProperty("_BaseMap"))
        {
            mainTexture = material.GetTexture("_BaseMap");
        }

        return mainTexture == null;
    }

    private void ConfigureCastleKitInstance(GameObject instance)
    {
        Material castleMaterial = GetCastleKitSharedMaterial();
        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;

            var materials = renderer.sharedMaterials;
            bool remapAll = castleMaterial != null;
            if (!remapAll)
            {
                for (int m = 0; m < materials.Length; m++)
                {
                    if (CastleKitMaterialNeedsRemap(materials[m]))
                    {
                        remapAll = true;
                        break;
                    }
                }
            }

            if (remapAll && castleMaterial != null)
            {
                for (int m = 0; m < materials.Length; m++)
                {
                    materials[m] = castleMaterial;
                }

                renderer.sharedMaterials = materials;
                continue;
            }

            for (int m = 0; m < materials.Length; m++)
            {
                ApplyOpaqueDoubleSided(materials[m]);
            }
        }
    }
}
