#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ApocalypseKingProjectSetup
{
    private const string SettingsFolder = "Assets/Settings";
    private const string ResourcesApocalypseFolder = "Assets/Resources/Apocalypse";
    private const string DanmuMappingPath = SettingsFolder + "/DanmuSpawnMappingConfig.asset";
    private const string DanmuMappingResourcesPath = ResourcesApocalypseFolder + "/DanmuSpawnMappingConfig.asset";
    private const string HudPrefabPath = ResourcesApocalypseFolder + "/ApocalypseHudPrefab.prefab";
    private const string ScenePath = "Assets/Scenes/ApocalypseKing.unity";

    [MenuItem("Apocalypse King/Setup Project Assets")]
    public static void SetupProjectAssets()
    {
        RunFullSetup();
    }

    [MenuItem("Apocalypse King/Setup Project Assets (Scene Independent)")]
    public static void SetupProjectAssetsOnly()
    {
        RunAssetsOnly();
    }

    /// <summary>Batchmode entry: create assets, open main scene, assign references, save.</summary>
    public static void ValidateDanmuSpawnMappingForBatchMode()
    {
        DanmuSpawnMappingEditorTests.ValidateDanmuSpawnMapping();
    }

    public static void SetupProjectAssetsForBatchMode()
    {
        RunAssetsOnly();
        if (!System.IO.File.Exists(ScenePath))
        {
            ApocalypseKingSceneBuilder.CreateMainScene();
        }
        else
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        DanmuSpawnMappingConfig mapping = AssetDatabase.LoadAssetAtPath<DanmuSpawnMappingConfig>(DanmuMappingPath);
        ApocalypseHudPrefab hudPrefab = AssetDatabase.LoadAssetAtPath<ApocalypseHudPrefab>(HudPrefabPath);
        AssignSceneReferences(mapping, hudPrefab);
        ApocalypseKingBattleContentSetup.AssignBattleContentToOpenScene();
        UnitConfigSetup.SetupConfigs();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ApocalypseKing] Batch setup complete.");
    }

    public static void RunFullSetup()
    {
        RunAssetsOnly();
        DanmuSpawnMappingConfig mapping = AssetDatabase.LoadAssetAtPath<DanmuSpawnMappingConfig>(DanmuMappingPath);
        ApocalypseHudPrefab hudPrefab = AssetDatabase.LoadAssetAtPath<ApocalypseHudPrefab>(HudPrefabPath);
        AssignSceneReferences(mapping, hudPrefab);
        ApocalypseKingBattleContentSetup.AssignBattleContentToOpenScene();
        UnitConfigSetup.SetupConfigs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ApocalypseKing] Project assets ready. Danmu mapping + HUD prefab assigned when scene is open.");
    }

    public static void RunAssetsOnly()
    {
        EnsureFolder("Assets/Settings");
        EnsureFolder("Assets/Resources");
        EnsureFolder(ResourcesApocalypseFolder);

        DanmuSpawnMappingConfig mapping = GetOrCreateDanmuMapping(DanmuMappingPath);
        DanmuSpawnMappingConfig resourcesMapping = GetOrCreateDanmuMapping(DanmuMappingResourcesPath);
        CopyDanmuMapping(mapping, resourcesMapping);
        GetOrCreateHudPrefab();
        GetOrCreateApocalypseMatchSettings(ResourcesApocalypseFolder + "/ApocalypseMatchSettings.asset");
        GetOrCreateApocalypseGiftCatalog(ResourcesApocalypseFolder + "/ApocalypseGiftCatalog.asset");
        ApocalypseKingBattleContentSetup.CreateOrUpdateBattleContentAssets();
        ApocalypseKingVfxTextureBake.BakeAll();
        ApocalypseKingVfxPrefabBinder.TryBindAllEffectConfigs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ApocalypseKing] Created/updated DanmuSpawnMappingConfig, HUD prefab, and battle effect/audio assets.");
    }

    private static ApocalypseMatchSettings GetOrCreateApocalypseMatchSettings(string path)
    {
        var settings = AssetDatabase.LoadAssetAtPath<ApocalypseMatchSettings>(path);
        if (settings != null)
        {
            return settings;
        }

        settings = ApocalypseMatchSettings.CreateRuntimeDefault();
        AssetDatabase.CreateAsset(settings, path);
        Debug.Log($"[ApocalypseKing] Created {path}");
        return settings;
    }

    private static ApocalypseGiftCatalog GetOrCreateApocalypseGiftCatalog(string path)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ApocalypseGiftCatalog>(path);
        if (catalog != null)
        {
            return catalog;
        }

        catalog = ScriptableObject.CreateInstance<ApocalypseGiftCatalog>();
        catalog.Entries = ApocalypseGiftCatalog.CreateDefaultEntries();
        AssetDatabase.CreateAsset(catalog, path);
        Debug.Log($"[ApocalypseKing] Created {path}");
        return catalog;
    }

    private static DanmuSpawnMappingConfig GetOrCreateDanmuMapping(string path)
    {
        var config = AssetDatabase.LoadAssetAtPath<DanmuSpawnMappingConfig>(path);
        if (config != null)
        {
            return config;
        }

        config = ScriptableObject.CreateInstance<DanmuSpawnMappingConfig>();
        config.HumanSpawnMappings = DanmuSpawnMapping.CreateDefaultHumanMappings();
        config.DefaultHumanAction = DanmuHumanSpawnAction.Soldier;
        config.UseDefaultActionForUnknownKeys = true;
        AssetDatabase.CreateAsset(config, path);
        Debug.Log($"[ApocalypseKing] Created {path}");
        return config;
    }

    private static void CopyDanmuMapping(DanmuSpawnMappingConfig source, DanmuSpawnMappingConfig target)
    {
        if (source == null || target == null || source == target)
        {
            return;
        }

        target.HumanSpawnMappings = source.HumanSpawnMappings;
        target.DefaultHumanAction = source.DefaultHumanAction;
        target.UseDefaultActionForUnknownKeys = source.UseDefaultActionForUnknownKeys;
        EditorUtility.SetDirty(target);
    }

    private static ApocalypseHudPrefab GetOrCreateHudPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<ApocalypseHudPrefab>(HudPrefabPath);
        if (existing != null)
        {
            return existing;
        }

        var root = new GameObject("ApocalypseHudPrefab", typeof(RectTransform), typeof(ApocalypseHudPrefab));
        var view = root.GetComponent<ApocalypseHudPrefab>();

        Font font = ApocalypseUiFonts.GetBuiltinUiFont();
        view.StaticCanvas = CreateHudCanvas(root.transform, "HUD_Static", 0, false, font);
        view.DynamicCanvas = CreateHudCanvas(root.transform, "HUD_Dynamic", 1, true, font);

        view.StaticHudRoot = CreateStretchRoot(view.StaticCanvas.transform, "StaticHudRoot");
        view.DynamicHudRoot = CreateStretchRoot(view.DynamicCanvas.transform, "DynamicHudRoot");

        var topPanel = CreatePanel(view.StaticHudRoot, "TopPanel", new Color(0.03f, 0.035f, 0.045f, 0.88f));
        Stretch(topPanel.rectTransform, 0.035f, 0.89f, 0.965f, 0.992f);

        var topDynamicRoot = CreateStretchRoot(view.DynamicHudRoot, "TopDynamicRoot");
        Stretch(topDynamicRoot, 0.035f, 0.89f, 0.965f, 0.992f);

        Color humanColor = new Color(0.24f, 0.64f, 1f, 1f);
        Color giantColor = new Color(1f, 0.36f, 0.28f, 1f);

        view.LeftTeamLabel = CreateText(topDynamicRoot, "LeftTeamLabel", "BLUE FORCE", 15, humanColor, TextAnchor.MiddleLeft, font);
        Stretch(view.LeftTeamLabel.rectTransform, 0.03f, 0.66f, 0.25f, 0.93f);

        view.RightTeamLabel = CreateText(topDynamicRoot, "RightTeamLabel", "丧尸", 15, giantColor, TextAnchor.MiddleRight, font);
        Stretch(view.RightTeamLabel.rectTransform, 0.75f, 0.66f, 0.97f, 0.93f);

        view.BattlePhaseLabel = CreateText(topDynamicRoot, "BattlePhaseLabel", "LIVE BARRAGE WAR", 12, new Color(0.78f, 0.82f, 0.86f, 1f), TextAnchor.MiddleCenter, font);
        Stretch(view.BattlePhaseLabel.rectTransform, 0.30f, 0.72f, 0.70f, 0.94f);

        view.PoolLabel = CreateText(topDynamicRoot, "PoolLabel", "POINT POOL 000,000", 24, new Color(1f, 0.85f, 0.34f, 1f), TextAnchor.MiddleCenter, font);
        Stretch(view.PoolLabel.rectTransform, 0.24f, 0.42f, 0.76f, 0.80f);

        view.TimerLabel = CreateText(topDynamicRoot, "TimerLabel", "03:00", 18, Color.white, TextAnchor.MiddleCenter, font);
        Stretch(view.TimerLabel.rectTransform, 0.40f, 0.18f, 0.60f, 0.46f);

        var humanPowerBack = CreatePanel(topPanel.transform, "HumanPowerBack", new Color(0.06f, 0.12f, 0.18f, 1f));
        Stretch(humanPowerBack.rectTransform, 0.03f, 0.03f, 0.47f, 0.17f);

        view.HumanPowerFill = CreatePanel(topDynamicRoot, "HumanPowerFill", new Color(0.24f, 0.70f, 1f, 1f));
        ConfigureHorizontalFill(view.HumanPowerFill, 0);
        Stretch(view.HumanPowerFill.rectTransform, 0.03f, 0.03f, 0.47f, 0.17f);

        var monsterPowerBack = CreatePanel(topPanel.transform, "MonsterPowerBack", new Color(0.18f, 0.08f, 0.07f, 1f));
        Stretch(monsterPowerBack.rectTransform, 0.53f, 0.03f, 0.97f, 0.17f);

        view.MonsterPowerFill = CreatePanel(topDynamicRoot, "MonsterPowerFill", giantColor);
        ConfigureHorizontalFill(view.MonsterPowerFill, 1);
        Stretch(view.MonsterPowerFill.rectTransform, 0.53f, 0.03f, 0.97f, 0.17f);
        view.HpFill = view.MonsterPowerFill;

        view.HumanLabel = CreateText(topDynamicRoot, "HumanLabel", "Force 0/0", 12, Color.white, TextAnchor.MiddleLeft, font);
        Stretch(view.HumanLabel.rectTransform, 0.03f, 0.18f, 0.34f, 0.36f);

        view.GiantLabel = CreateText(topDynamicRoot, "GiantLabel", "丧尸 HP 0", 12, Color.white, TextAnchor.MiddleRight, font);
        Stretch(view.GiantLabel.rectTransform, 0.66f, 0.18f, 0.97f, 0.36f);

        var bottomPanel = CreatePanel(view.StaticHudRoot, "LiveBottomPanel", new Color(0.025f, 0.03f, 0.04f, 0.84f));
        Stretch(bottomPanel.rectTransform, 0.035f, 0.050f, 0.965f, 0.158f);

        var bottomDynamicRoot = CreateStretchRoot(view.DynamicHudRoot, "BottomDynamicRoot");
        Stretch(bottomDynamicRoot, 0.035f, 0.050f, 0.965f, 0.158f);

        view.BottomTickerLabel = CreateText(bottomDynamicRoot, "BottomTickerLabel", "Barrage connected", 14, new Color(0.94f, 0.97f, 1f, 1f), TextAnchor.MiddleLeft, font);
        Stretch(view.BottomTickerLabel.rectTransform, 0.03f, 0.56f, 0.72f, 0.88f);

        view.GiftFeedLabel = CreateText(bottomDynamicRoot, "GiftFeedLabel", "Gift heat 0", 13, new Color(1f, 0.83f, 0.38f, 1f), TextAnchor.MiddleLeft, font);
        Stretch(view.GiftFeedLabel.rectTransform, 0.03f, 0.24f, 0.62f, 0.56f);

        view.StatusLabel = CreateText(bottomDynamicRoot, "StatusLabel", "Ready", 12, new Color(0.70f, 0.78f, 0.84f, 1f), TextAnchor.MiddleLeft, font);
        Stretch(view.StatusLabel.rectTransform, 0.03f, 0.04f, 0.62f, 0.26f);

        view.SkillCountdownLabel = CreateText(bottomDynamicRoot, "SkillCountdownLabel", "Skill CD 00s", 14, new Color(0.78f, 1f, 0.82f, 1f), TextAnchor.MiddleRight, font);
        Stretch(view.SkillCountdownLabel.rectTransform, 0.66f, 0.18f, 0.97f, 0.84f);

        view.BannerLabel = CreateText(view.DynamicHudRoot, "BannerLabel", string.Empty, 28, new Color(1f, 0.94f, 0.6f, 1f), TextAnchor.MiddleCenter, font);
        Stretch(view.BannerLabel.rectTransform, 0.15f, 0.80f, 0.85f, 0.865f);
        view.BannerLabel.gameObject.SetActive(false);

        view.LoadingPanel = CreatePanel(view.DynamicCanvas.transform, "LoadingPanel", new Color(0.02f, 0.03f, 0.05f, 0.90f));
        Stretch(view.LoadingPanel.rectTransform, 0f, 0f, 1f, 1f);
        view.LoadingPanel.rectTransform.offsetMin = Vector2.zero;
        view.LoadingPanel.rectTransform.offsetMax = Vector2.zero;

        view.LoadingLabel = CreateText(view.LoadingPanel.transform, "LoadingLabel", "Loading 3D models...", 26, Color.white, TextAnchor.MiddleCenter, font);
        Stretch(view.LoadingLabel.rectTransform, 0.15f, 0.45f, 0.85f, 0.58f);

        view.ResolutionStrip = CreatePanel(view.StaticHudRoot, "ResolutionStrip", new Color(0.08f, 0.10f, 0.12f, 0.92f));
        Stretch(view.ResolutionStrip.rectTransform, 0.04f, 0.16f, 0.96f, 0.24f);
        view.ResolutionStrip.gameObject.SetActive(false);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[ApocalypseKing] Created {HudPrefabPath}");
        return prefab != null ? prefab.GetComponent<ApocalypseHudPrefab>() : null;
    }

    private static void AssignSceneReferences(DanmuSpawnMappingConfig mapping, ApocalypseHudPrefab hudPrefab)
    {
        var game = Object.FindObjectOfType<ApocalypseKingUnityGame>();
        if (game == null)
        {
            Debug.LogWarning("[ApocalypseKing] ApocalypseKingUnityGame not found in open scene. Assets were created but scene was not updated.");
            return;
        }

        Undo.RecordObject(game, "Setup Project Assets");
        var so = new SerializedObject(game);
        so.FindProperty("danmuSpawnMappingConfig").objectReferenceValue = mapping;
        so.FindProperty("hudPrefab").objectReferenceValue = hudPrefab;
        so.ApplyModifiedProperties();
        if (game.GetComponent<DanmuWebSocketGateway>() == null)
        {
            Undo.AddComponent<DanmuWebSocketGateway>(game.gameObject);
        }

        EditorUtility.SetDirty(game);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
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

    private static Canvas CreateHudCanvas(Transform parent, string name, int sortingOrder, bool raycaster, Font font)
    {
        var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1170f, 2532f);
        scaler.matchWidthOrHeight = 0.5f;
        if (!raycaster)
        {
            Object.DestroyImmediate(canvasObject.GetComponent<GraphicRaycaster>());
        }

        return canvas;
    }

    private static RectTransform CreateStretchRoot(Transform parent, string name)
    {
        var rootObject = new GameObject(name, typeof(RectTransform));
        rootObject.transform.SetParent(parent, false);
        var rect = rootObject.GetComponent<RectTransform>();
        Stretch(rect, 0f, 0f, 1f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Image CreatePanel(Transform parent, string name, Color color)
    {
        var panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);
        var image = panelObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(Transform parent, string name, string text, int size, Color color, TextAnchor anchor, Font font)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        var label = textObject.GetComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = anchor;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        return label;
    }

    private static void ConfigureHorizontalFill(Image image, int fillOrigin)
    {
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = fillOrigin;
        image.fillAmount = 1f;
    }

    private static void Stretch(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif
