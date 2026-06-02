#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Unity Asset Store Zombie (30232) — open download UI and copy models into Resources.</summary>
public static class ApocalypseKingZombieAssetImport
{
    private const string AssetStoreUrl = "https://assetstore.unity.com/packages/3d/characters/humanoids/zombie-30232";
    private const string RealisticUnityStoreDir = "Assets/Resources/RealisticZombies/UnityStore";

    [MenuItem("Apocalypse King/Install Unity Store Zombie/1. Open Asset Store Page")]
    public static void OpenAssetStorePage()
    {
        Application.OpenURL(AssetStoreUrl);
        Debug.Log("[ApocalypseKing] Opened Zombie (30232) on Asset Store. Sign in, click Add to My Assets, then use step 2.");
    }

    [MenuItem("Apocalypse King/Install Unity Store Zombie/2. Open Package Manager (My Assets)")]
    public static void OpenPackageManager()
    {
        EditorApplication.ExecuteMenuItem("Window/Package Manager");
        Debug.Log(
            "[ApocalypseKing] Package Manager opened. "
            + "Top-left: Packages > My Assets. Search 'Zombie', Download, then Import into this project.");
    }

    [MenuItem("Apocalypse King/Install Unity Store Zombie/3. Copy Zombie1-3 to RealisticZombies")]
    public static void CopyImportedModelsToResources()
    {
        int copied = CopyUnityStoreZombieModelsToResources(logDetails: true);
        AssetDatabase.Refresh();
        if (copied > 0)
        {
            Debug.Log($"[ApocalypseKing] Copied {copied} Unity Store zombie model(s) to {RealisticUnityStoreDir}. Rebuild the game.");
        }
        else
        {
            Debug.LogWarning(
                "[ApocalypseKing] No Zombie1/2/3.fbx found in project. "
                + "Complete Package Manager Download + Import first (menu step 2).");
        }
    }

    /// <summary>Batchmode: copy after user imported via Package Manager.</summary>
    public static void FinalizeUnityStoreZombieForBatchMode()
    {
        int copied = CopyUnityStoreZombieModelsToResources(logDetails: true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (copied == 0)
        {
            Debug.LogError(
                "[ApocalypseKing] Unity Store Zombie not found. "
                + "Sign in via Unity Hub, import package 30232 in Package Manager > My Assets, then re-run install script.");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[ApocalypseKing] Unity Store Zombie finalize OK ({copied} model(s)).");
        EditorApplication.Exit(0);
    }

    private static int CopyUnityStoreZombieModelsToResources(bool logDetails)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources/RealisticZombies"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "RealisticZombies");
        }

        if (!AssetDatabase.IsValidFolder(RealisticUnityStoreDir))
        {
            AssetDatabase.CreateFolder("Assets/Resources/RealisticZombies", "UnityStore");
        }

        string[] names = { "Zombie1", "Zombie2", "Zombie3" };
        int copied = 0;
        for (int i = 0; i < names.Length; i++)
        {
            string sourcePath = FindZombieModelAssetPath(names[i]);
            if (string.IsNullOrEmpty(sourcePath))
            {
                continue;
            }

            string destPath = $"{RealisticUnityStoreDir}/{names[i]}{Path.GetExtension(sourcePath)}";
            if (AssetDatabase.CopyAsset(sourcePath, destPath))
            {
                copied++;
                if (logDetails)
                {
                    Debug.Log($"[ApocalypseKing] {names[i]}: {sourcePath} -> {destPath}");
                }
            }
            else if (logDetails)
            {
                Debug.LogWarning($"[ApocalypseKing] Failed to copy {sourcePath} to {destPath}");
            }
        }

        return copied;
    }

    private static string FindZombieModelAssetPath(string modelName)
    {
        string[] guids = AssetDatabase.FindAssets($"{modelName} t:Model");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            string fileName = Path.GetFileNameWithoutExtension(path);
            if (!fileName.Equals(modelName, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (path.StartsWith(RealisticUnityStoreDir, System.StringComparison.Ordinal))
            {
                return path;
            }

            return path;
        }

        return null;
    }
}
#endif
