using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ApocalypseKingSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/ApocalypseKing.unity";
    private const string BuildDirectory = "Builds/Windows";
    private const string BuildPath = BuildDirectory + "/ApocalypseKingUnity3D.exe";

    [MenuItem("Apocalypse King/Create Main Scene")]
    public static void CreateMainScene()
    {
        EnsureFolder("Assets/Scenes");
        EnsureRuntimeMaterials();
        ConfigureMobilePlayerSettings();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SceneManager.SetActiveScene(scene);

        var gameObject = new GameObject("ApocalypseKingGame");
        gameObject.AddComponent<DanmuCommandQueue>();
        gameObject.AddComponent<DanmuHttpGateway>();
        gameObject.AddComponent<EffectManager>();
        gameObject.AddComponent<BattleAudioManager>();
        gameObject.AddComponent<ApocalypseKingUnityGame>();
        gameObject.AddComponent<ApocalypseKingRuntimeProbe>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
        };

        AssetDatabase.SaveAssets();
        Debug.Log($"[ApocalypseSceneBuilder] Created scene at {ScenePath}");
    }

    [MenuItem("Apocalypse King/Build Windows Player")]
    public static void BuildWindowsPlayer()
    {
        CreateMainScene();
        ConfigureMobilePlayerSettings();
        Directory.CreateDirectory(BuildDirectory);

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = BuildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[ApocalypseSceneBuilder] Build result: {report.summary.result}, size: {report.summary.totalSize} bytes");

        if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded)
        {
            EditorApplication.Exit(1);
        }
    }

    private static void ConfigureMobilePlayerSettings()
    {
        PlayerSettings.defaultScreenWidth = 720;
        PlayerSettings.defaultScreenHeight = 1280;
        PlayerSettings.defaultIsNativeResolution = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
        string child = Path.GetFileName(folder);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(string.IsNullOrEmpty(parent) ? "Assets" : parent, child);
    }

    private static void EnsureRuntimeMaterials()
    {
        EnsureFolder("Assets/Resources/RuntimeMaterials");
        EnsureMaterial("Assets/Resources/RuntimeMaterials/RuntimeOpaque.mat", "Standard", "Legacy Shaders/Diffuse", "Unlit/Color");
        EnsureMaterial("Assets/Resources/RuntimeMaterials/RuntimeTransparent.mat", "Legacy Shaders/Transparent/Diffuse", "Standard", "Sprites/Default");
        EnsureMaterial("Assets/Resources/RuntimeMaterials/RuntimeUnlit.mat", "Sprites/Default", "Unlit/Color", "Standard");
        EnsureMaterial("Assets/Resources/RuntimeMaterials/RuntimeGltfPbrMetallicRoughness.mat", "GLTF/PbrMetallicRoughness", "Standard", "Legacy Shaders/Diffuse");
    }

    private static void EnsureMaterial(string path, params string[] shaderNames)
    {
        Shader shader = null;
        for (int i = 0; i < shaderNames.Length; i++)
        {
            shader = Shader.Find(shaderNames[i]);
            if (shader != null)
            {
                break;
            }
        }

        if (shader == null)
        {
            Debug.LogWarning($"[ApocalypseSceneBuilder] Could not find shader for {path}");
            return;
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        EditorUtility.SetDirty(material);
    }

}
