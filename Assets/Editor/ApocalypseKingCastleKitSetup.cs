#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ApocalypseKingCastleKitSetup
{
    private const string SourceFolder = "Assets/Resources/Kenney/CastleKit";
    private const string PrefabFolder = SourceFolder + "/Prefabs";

    [MenuItem("Apocalypse King/Bake Castle Kit Prefabs")]
    public static void BakeCastleKitPrefabsMenu()
    {
        int baked = BakeCastleKitPrefabs();
        Debug.Log($"[ApocalypseKing] Castle kit prefab bake finished. Created/updated: {baked}");
    }

    public static void EnsureCastleKitPrefabsForBatchMode()
    {
        BakeCastleKitPrefabs();
    }

    private static int BakeCastleKitPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(SourceFolder))
        {
            return 0;
        }

        EnsureFolder(PrefabFolder);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        string[] guids = AssetDatabase.FindAssets("", new[] { SourceFolder });
        int baked = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)
                || path.IndexOf("/Prefabs/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            if (TryBakeGlbPrefab(path))
            {
                baked++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return baked;
    }

    private static bool TryBakeGlbPrefab(string glbPath)
    {
        string assetName = Path.GetFileNameWithoutExtension(glbPath);
        if (string.IsNullOrEmpty(assetName))
        {
            return false;
        }

        string prefabPath = PrefabFolder + "/" + assetName + ".prefab";
        GameObject source = LoadGlbRootGameObject(glbPath, assetName);
        if (source == null)
        {
            Debug.LogWarning($"[ApocalypseKing] Could not load GLB root for {glbPath}");
            return false;
        }

        ApplyCastleKitMaterialToHierarchy(source);
        GameObject prefabRoot = PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
        return prefabRoot != null;
    }

    private static GameObject LoadGlbRootGameObject(string path, string assetName)
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root != null)
        {
            return root;
        }

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        GameObject bestWithChildren = null;
        for (int i = 0; i < assets.Length; i++)
        {
            GameObject candidate = assets[i] as GameObject;
            if (candidate == null)
            {
                continue;
            }

            if (string.Equals(candidate.name, assetName, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }

            if (candidate.transform.childCount > 0)
            {
                bestWithChildren = candidate;
            }
        }

        return bestWithChildren;
    }

    private static void ApplyCastleKitMaterialToHierarchy(GameObject root)
    {
        Texture2D colormap = AssetDatabase.LoadAssetAtPath<Texture2D>(SourceFolder + "/colormap.png");
        if (colormap == null)
        {
            colormap = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/ThirdParty/Kenney/CastleKit/Models/GLB format/Textures/colormap.png");
        }

        if (colormap == null)
        {
            return;
        }

        Shader shader = Shader.Find("Standard")
            ?? Shader.Find("Legacy Shaders/Diffuse")
            ?? Shader.Find("Unlit/Texture");
        if (shader == null)
        {
            return;
        }

        Material sharedMaterial = new Material(shader);
        sharedMaterial.name = "CastleKit_Colormap";
        sharedMaterial.mainTexture = colormap;
        sharedMaterial.color = Color.white;
        if (sharedMaterial.HasProperty("_Glossiness"))
        {
            sharedMaterial.SetFloat("_Glossiness", 0.08f);
        }

        if (sharedMaterial.HasProperty("_Cull"))
        {
            sharedMaterial.SetInt("_Cull", (int)CullMode.Off);
        }

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                materials[m] = sharedMaterial;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
#endif
