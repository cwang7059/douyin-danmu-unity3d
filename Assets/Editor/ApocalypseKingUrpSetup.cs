#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_RENDER_PIPELINE_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

/// <summary>URP 迁移与 Asset Store Fireball Pack 接入（Unity 2022.3 + URP 14）。</summary>
public static class ApocalypseKingUrpSetup
{
    private const string FireballPackUrl =
        "https://assetstore.unity.com/packages/vfx/particles/fire-explosions/free-asset-vfx-particles-fireball-pack-263814";

    private const string SettingsFolder = "Assets/Settings/URP";
    private const string PipelineAssetPath = SettingsFolder + "/ApocalypseKing_URP.asset";
    private const string RendererAssetPath = SettingsFolder + "/ApocalypseKing_ForwardRenderer.asset";
    private const string ResourcesVfxFolder = "Assets/Resources/Battle/VFX";
    private const string InstalledFireballPath = ResourcesVfxFolder + "/UrpPterosaurFireball.prefab";
    private const string InstalledFireballHitPath = ResourcesVfxFolder + "/UrpPterosaurFireballHit.prefab";

    private static readonly string[] FireballPrefabNamePriority =
    {
        "Fireball_Trail",
        "Fireball Trail",
        "Fireball+Trail",
        "Fireball",
        "fireball",
    };

    [MenuItem("Apocalypse King/URP/Open Fireball Pack (Asset Store)")]
    public static void OpenFireballPackPage()
    {
        Application.OpenURL(FireballPackUrl);
        Debug.Log(
            "[ApocalypseKing][URP] 已在浏览器打开 Fireball Pack 页面。下载后：Package Manager > My Assets > Import，"
            + "再执行菜单「URP/3. 安装翼龙火球 Prefab 到 Resources」。");
    }

    [MenuItem("Apocalypse King/URP/1. Create and Assign URP Pipeline")]
    public static void CreateAndAssignUrpPipeline()
    {
#if UNITY_RENDER_PIPELINE_UNIVERSAL
        EnsureFolder("Assets/Settings");
        EnsureFolder(SettingsFolder);

        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
        if (rendererData == null)
        {
            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            rendererData.name = "ApocalypseKing_ForwardRenderer";
            AssetDatabase.CreateAsset(rendererData, RendererAssetPath);
        }

        UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
        if (pipeline == null)
        {
            pipeline = UniversalRenderPipelineAsset.Create(rendererData);
            pipeline.name = "ApocalypseKing_URP";
            AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);
        }

        AssignPipelineToProject(pipeline);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[ApocalypseKing][URP] 已创建并指派 URP 管线：\n"
            + $"  Pipeline: {PipelineAssetPath}\n"
            + $"  Renderer: {RendererAssetPath}\n"
            + "下一步请执行「URP/2. Upgrade Project Materials to URP」。");
#else
        Debug.LogError(
            "[ApocalypseKing][URP] 未安装 Universal RP 包。请在 Package Manager 安装 "
            + "com.unity.render-pipelines.universal（或等待 manifest 解析完成后重新编译）。");
#endif
    }

    [MenuItem("Apocalypse King/URP/2. Upgrade Project Materials to URP")]
    public static void UpgradeProjectMaterialsToUrp()
    {
#if UNITY_RENDER_PIPELINE_UNIVERSAL
        if (GraphicsSettings.currentRenderPipeline == null)
        {
            Debug.LogWarning("[ApocalypseKing][URP] 尚未指派 URP Pipeline，请先执行「URP/1. Create and Assign URP Pipeline」。");
            return;
        }

        bool executed = EditorApplication.ExecuteMenuItem(
            "Edit/Render Pipeline/Universal Render Pipeline/Upgrade Project Materials to URP Materials");
        if (!executed)
        {
            Debug.LogWarning(
                "[ApocalypseKing][URP] 无法自动执行材质升级菜单。请手动："
                + "Edit > Render Pipeline > Universal Render Pipeline > Upgrade Project Materials to URP Materials");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ApocalypseKing][URP] 已触发全项目材质升级（Built-in → URP）。");
#else
        Debug.LogError("[ApocalypseKing][URP] 需要先安装并编译 Universal RP 包。");
#endif
    }

    [MenuItem("Apocalypse King/URP/3. Install Pterosaur Fireball Prefab to Resources")]
    public static void InstallPterosaurFireballToResources()
    {
        GameObject fireball = FindBestFireballPrefab();
        if (fireball == null)
        {
            Debug.LogError(
                "[ApocalypseKing][URP] 未找到 Fireball Pack 里的 Prefab。请先导入 "
                + "VFX Particles: Fireball Pack，或执行「URP/Open Fireball Pack」。");
            return;
        }

        GameObject hit = FindBestFireballHitPrefab() ?? fireball;
        EnsureFolder("Assets/Resources/Battle");
        EnsureFolder(ResourcesVfxFolder);

        CopyPrefabAsset(fireball, InstalledFireballPath);
        CopyPrefabAsset(hit, InstalledFireballHitPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[ApocalypseKing][URP] 翼龙火球已写入 Resources：\n"
            + $"  {InstalledFireballPath}  (from {fireball.name})\n"
            + $"  {InstalledFireballHitPath}  (from {hit.name})");
    }

    [MenuItem("Apocalypse King/URP/Run Full URP + Fireball Setup")]
    public static void RunFullUrpFireballSetup()
    {
        CreateAndAssignUrpPipeline();
        UpgradeProjectMaterialsToUrp();
        if (FindBestFireballPrefab() != null)
        {
            InstallPterosaurFireballToResources();
        }
        else
        {
            OpenFireballPackPage();
            Debug.LogWarning("[ApocalypseKing][URP] Fireball Pack 未导入，已打开下载页；导入后请再执行「URP/3」。");
        }
    }

#if UNITY_RENDER_PIPELINE_UNIVERSAL
    private static void AssignPipelineToProject(UniversalRenderPipelineAsset pipeline)
    {
        GraphicsSettings.renderPipelineAsset = pipeline;
        QualitySettings.renderPipeline = pipeline;
        for (int i = 0; i < QualitySettings.count; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipeline;
        }

        EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());
    }
#endif

    private static GameObject FindBestFireballPrefab()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        GameObject best = null;
        int bestScore = int.MinValue;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(path)
                || path.StartsWith("Assets/Resources/Battle/VFX/Urp", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.IndexOf("fireball", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            if (fileName.IndexOf("hit", System.StringComparison.OrdinalIgnoreCase) >= 0
                || fileName.IndexOf("impact", System.StringComparison.OrdinalIgnoreCase) >= 0
                || fileName.IndexOf("explosion", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            int score = ScoreFireballPrefabName(fileName);
            if (path.IndexOf("Fireball", System.StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("SLODREAM", System.StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("263814", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 20;
            }

            if (fileName.IndexOf("trail", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 40;
            }

            if (score <= bestScore)
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponentsInChildren<ParticleSystem>(true).Length == 0)
            {
                continue;
            }

            bestScore = score;
            best = prefab;
        }

        return best;
    }

    private static GameObject FindBestFireballHitPrefab()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        GameObject best = null;
        int bestScore = int.MinValue;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.IndexOf("fireball", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            bool isHit = fileName.IndexOf("hit", System.StringComparison.OrdinalIgnoreCase) >= 0
                || fileName.IndexOf("impact", System.StringComparison.OrdinalIgnoreCase) >= 0
                || fileName.IndexOf("explosion", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isHit)
            {
                continue;
            }

            int score = 10;
            if (fileName.IndexOf("fire", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 8;
            }

            if (score <= bestScore)
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                continue;
            }

            bestScore = score;
            best = prefab;
        }

        return best;
    }

    private static int ScoreFireballPrefabName(string fileName)
    {
        for (int i = 0; i < FireballPrefabNamePriority.Length; i++)
        {
            if (string.Equals(fileName, FireballPrefabNamePriority[i], System.StringComparison.OrdinalIgnoreCase))
            {
                return 100 - i * 5;
            }
        }

        return fileName.IndexOf("trail", System.StringComparison.OrdinalIgnoreCase) >= 0 ? 50 : 20;
    }

    private static void CopyPrefabAsset(GameObject source, string targetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(targetPath) != null)
        {
            AssetDatabase.DeleteAsset(targetPath);
        }

        if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), targetPath))
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
            {
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
            Object.DestroyImmediate(instance);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
        {
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
