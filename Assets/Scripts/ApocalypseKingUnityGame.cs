using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityGLTF;

public sealed class ApocalypseKingUnityGame : MonoBehaviour
{
    private const int SoldierCount = 100;
    private const int TankT55ACount = 6;
    private const int TankT55AkCount = 10;
    private const int TankCount = TankT55ACount + TankT55AkCount;
    private const int AircraftCount = 3;
    private const int GiantCount = 10;
    private const int MaxProjectiles = 220;
    private const int MaxEffects = 48;
    private const int MaxDeathVisuals = 56;
    private const float TankT55AYawOffset = 0f;
    private const float TankT55AkYawOffset = 0f;
    private const string SoldierResourceModelPath = "Quaternius/ZombieApocalypse/Characters_Sam_SingleWeapon";
    private const string SoldierResourceFolderPath = "Quaternius/ZombieApocalypse";
    private const string TankResourceFolderPath = "Quaternius/AnimatedTankPack";
    private const string TankResourceModelPath = TankResourceFolderPath + "/TankA";
    private const string TankScoutResourceModelPath = TankResourceFolderPath + "/TankB";
    private const string TankAssaultResourceModelPath = TankResourceFolderPath + "/TankC";
    private const string TankHeavyResourceModelPath = TankResourceFolderPath + "/TankD";
    private const string GiantResourceFolderPath = "Quaternius/UltimateMonsters";
    private const string GiantResourceModelPath = GiantResourceFolderPath + "/BlueDemon";
    private const string MedievalVillageResourceFolderPath = "Quaternius/MedievalVillageMegaKit";

    private static readonly string[] GiantResourceVariantModelPaths =
    {
        GiantResourceFolderPath + "/MushroomKing",
        GiantResourceFolderPath + "/Orc",
        GiantResourceFolderPath + "/Yeti",
        GiantResourceFolderPath + "/Dino",
    };

    [Header("Unit Settings")]
    [SerializeField] private UnitConfig soldierConfig;
    [SerializeField] private UnitConfig tankConfig;
    [SerializeField] private UnitConfig aircraftConfig;
    [SerializeField] private UnitConfig giantConfig;


    private const float LogicalToWorld = 0.025f;
    private const float Left = -360f;
    private const float Right = 360f;
    private const float Top = 640f;
    private const float Bottom = -640f;
    private const float GiantGroundY = -228f;

    private static readonly float[] SoldierLanes = { -300f, -252f, -204f, -156f, -108f, -60f, -12f, 36f, 84f, 132f };
    private static readonly float[] TankLanes = { -572f, -486f, -400f };
    private static readonly float[] AirLanes = { 250f, 370f, 490f };
    private static readonly HashSet<string> TankDisplayMaterialNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "display",
        "floor",
        "ground",
        "shadow",
        "smoke",
    };

    private static readonly ResolutionPreset[] ResolutionPresets =
    {
        new ResolutionPreset("720x1280", 720, 1280),
        new ResolutionPreset("1080x1920", 1080, 1920),
        new ResolutionPreset("1080x2400", 1080, 2400),
        new ResolutionPreset("1170x2532", 1170, 2532),
        new ResolutionPreset("1440x3200", 1440, 3200),
    };

    private static readonly Color BackgroundColor = new Color(0.09f, 0.08f, 0.10f, 1f);
    private static readonly Color RoadColor = new Color(0.33f, 0.25f, 0.16f, 1f);
    private static readonly Color RuinColor = new Color(0.11f, 0.12f, 0.15f, 1f);
    private static readonly Color HumanColor = new Color(0.24f, 0.64f, 1f, 1f);
    private static readonly Color GiantColor = new Color(1f, 0.36f, 0.28f, 1f);

    private static readonly Dictionary<UnitKind, ModelPose> Poses = new Dictionary<UnitKind, ModelPose>
    {
        { UnitKind.Soldier, new ModelPose(0.88f, 0f, 90f, 0f, 0f, true) },
        { UnitKind.Tank, new ModelPose(1.08f, 0f, 0f, 0f, 0f, true) },
        { UnitKind.Aircraft, new ModelPose(1.18f, 0f, 108f, 0f, 0.2f, true) },
        { UnitKind.Giant, new ModelPose(3.35f, 0f, -90f, 0f, 0f, false) },
        { UnitKind.Fireball, new ModelPose(1.2f, 0f, 0f, 0f, 0f, false) },
        { UnitKind.Smoke, new ModelPose(1.4f, 0f, 0f, 0f, 0f, false) },
    };

    [SerializeField] private float cameraYaw = 0f;
    [SerializeField] private float cameraPitch = 18f;
    [SerializeField] private float cameraDistance = 88f;

    private Transform worldRoot;
    private Transform decorRoot;
    private Transform unitRoot;
    private Transform projectileRoot;
    private Transform effectRoot;
    private Transform modelCacheRoot;
    private Transform cameraTarget;

    private Camera mainCamera;
    private OrbitTouchCamera orbitCamera;
    private float cameraShakeTime;
    private float cameraShakeDuration;
    private float cameraShakeAmplitude;

    private Canvas canvas;
    private RectTransform hudRoot;
    private Font uiFont;
    private Image loadingPanel;
    private Text loadingLabel;
    private Text bannerLabel;
    private Text timerLabel;
    private Text poolLabel;
    private Text leftTeamLabel;
    private Text rightTeamLabel;
    private Text battlePhaseLabel;
    private Text bottomTickerLabel;
    private Text skillCountdownLabel;
    private Text giftFeedLabel;
    private Text humanLabel;
    private Text giantLabel;
    private Text statusLabel;
    private Button[] resolutionButtons;
    private Image[] resolutionButtonImages;
    private Image hpFill;
    private Image humanPowerFill;
    private Image monsterPowerFill;
    private DanmuCommandQueue danmuQueue;

    private readonly Dictionary<UnitKind, GameObject> modelPrototypes = new Dictionary<UnitKind, GameObject>();
    private GameObject tankT55AkPrototype;
    private readonly List<GameObject> tankVariantPrototypes = new List<GameObject>();
    private readonly List<GameObject> giantVariantPrototypes = new List<GameObject>();
    private readonly List<BattleUnit> soldiers = new List<BattleUnit>(SoldierCount);
    private readonly List<BattleUnit> tanks = new List<BattleUnit>(TankCount);
    private readonly List<BattleUnit> aircraft = new List<BattleUnit>(AircraftCount);
    private readonly List<BattleUnit> giants = new List<BattleUnit>(GiantCount);
    private readonly List<BuildingObstacle> buildingObstacles = new List<BuildingObstacle>();
    private readonly List<ProjectileView> projectiles = new List<ProjectileView>(MaxProjectiles);
    private readonly List<EffectView> effects = new List<EffectView>(MaxEffects);
    private readonly List<DeathVisual> deathVisuals = new List<DeathVisual>(MaxDeathVisuals);
    private readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
    private readonly Dictionary<string, GameObject> medievalVillagePrefabs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

    private bool assetsReady;
    private bool paused;
    private bool ended;
    private float battleTime;
    private float loadingPulseTime;
    private int humanLosses;
    private int nextId = 1;
    private int processedDanmuCommandCount;
    private int medievalVillagePrefabCount;
    private int selectedResolutionIndex;
    private Rect lastSafeArea;
    private Vector2 lastScreenSize;

    public bool DiagnosticsAssetsReady => assetsReady;
    public int DiagnosticsPrototypeCount => modelPrototypes.Count + (tankT55AkPrototype != null ? 1 : 0);
    public bool DiagnosticsUsingFallback { get; private set; }
    public int DiagnosticsActiveUnitCount => CountHumans() + CountActive(giants);
    public float DiagnosticsBattleTime => battleTime;
    public int DiagnosticsTankOverlapCount => CountTankOverlaps();
    public float DiagnosticsMinimumTankGap => GetMinimumTankGap();
    public float DiagnosticsAverageTankHeading => GetAverageHeading(tanks);
    public float DiagnosticsAverageTankMoveSpeed => GetAverageMoveSpeed(tanks);
    public int DiagnosticsTankAnimatorCount => CountAnimatorUnits(tanks);
    public string DiagnosticsFirstTankAnimation => GetFirstAnimatorClipName(tanks);
    public int DiagnosticsMedievalVillagePrefabCount => medievalVillagePrefabCount;
    public int DiagnosticsBuildingObstacleCount => buildingObstacles.Count;
    public int DiagnosticsBuildingOverlapCount => CountBuildingOverlaps();
    public int DiagnosticsSoldierAnimatorCount => CountAnimatorUnits(soldiers);
    public string DiagnosticsFirstSoldierAnimation => GetFirstAnimatorClipName(soldiers);
    public int DiagnosticsGiantAnimatorCount => CountAnimatorUnits(giants);
    public string DiagnosticsFirstGiantAnimation => GetFirstAnimatorClipName(giants);
    public int DiagnosticsGiantCount => CountActive(giants);
    public float DiagnosticsGiantHp => GetGiantHpTotal();
    public float DiagnosticsGiantMaxHp => GetGiantMaxHpTotal();
    public bool DiagnosticsGiantEngaged => assetsReady && FindGiantEngagementTarget() != null;
    public bool DiagnosticsGiantContact => assetsReady && FindGiantContactTarget() != null;
    public float DiagnosticsGiantX => GetActiveGiantCenter().x;
    public float DiagnosticsGiantZ => GetActiveGiantCenter().y;
    public int DiagnosticsDanmuPending => danmuQueue != null ? danmuQueue.PendingCount : 0;
    public int DiagnosticsDanmuAccepted => danmuQueue != null ? danmuQueue.AcceptedCommandCount : 0;
    public int DiagnosticsDanmuDropped => danmuQueue != null ? danmuQueue.DroppedCommandCount : 0;

    private void Awake()
    {
        danmuQueue = GetComponent<DanmuCommandQueue>();
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        ApplyDefaultMobilePresentation();
        CreateCoreScene();
        CreateHud();
        CreateBattlefield();
        CreateUnits();
        EnsureUnitConfigs();
        ShowLoading(true);
    }

    private async void Start()
    {
        try
        {
            await LoadPrototypes();
            AttachPrototypesToUnits();
            assetsReady = true;
            ShowLoading(false);
            ResetBattle();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            DiagnosticsUsingFallback = true;
            assetsReady = true;
            ShowLoading(false);
            AttachFallbackPrototypes();
            ResetBattle();
            ShowBanner("Loaded with fallback geometry", true, 3f);
        }
    }

    private void Update()
    {
        UpdateSafeAreaIfNeeded();

        if (!assetsReady)
        {
            loadingPulseTime += Time.unscaledDeltaTime;
            UpdateLoadingLabel();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) && !ended)
        {
            paused = !paused;
            ShowBanner(paused ? "Paused" : "Resumed", false, 1f);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetBattle();
        }

        UpdateCameraShake(Time.deltaTime);

        if (paused || ended)
        {
            RefreshHud();
            return;
        }

        float dt = Mathf.Min(Time.deltaTime, 0.045f);
        battleTime += dt;
        EnqueueLocalDanmuShortcuts();
        ProcessDanmuCommands();
        UpdateHumans(dt);
        UpdateGiants(dt);
        ResolveUnitOverlaps();
        UpdateProjectiles(dt);
        UpdateEffects(dt);
        UpdateDeathVisuals(dt);
        CleanupProjectiles();
        CheckBattleEnd();
        RefreshHud();
    }

    private void OnDestroy()
    {
        DisposeUnitAnimators(soldiers);
        DisposeUnitAnimators(tanks);
        DisposeUnitAnimators(aircraft);
        DisposeUnitAnimators(giants);
    }

    private static void DisposeUnitAnimators(List<BattleUnit> units)
    {
        if (units == null)
        {
            return;
        }

        for (int i = 0; i < units.Count; i++)
        {
            DisposeUnitAnimator(units[i]);
        }
    }

    private void EnqueueLocalDanmuShortcuts()
    {
        if (danmuQueue == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            danmuQueue.EnqueueRawMessage("local-human", "Local Human", "人族步兵");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            danmuQueue.EnqueueRawMessage("local-orc", "Local Orc", "兽族地狱犬");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            danmuQueue.EnqueueRawMessage("local-skill", "Local Skill", "人族空袭");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            danmuQueue.EnqueueRawMessage("local-rage", "Local Rage", "兽族狂暴");
        }
    }

    private void ProcessDanmuCommands()
    {
        if (danmuQueue == null)
        {
            return;
        }

        int limit = Mathf.Max(1, danmuQueue.MaxCommandsPerFrame);
        for (int i = 0; i < limit; i++)
        {
            DanmuCommand command;
            if (!danmuQueue.TryDequeue(out command))
            {
                return;
            }

            ApplyDanmuCommand(command);
            processedDanmuCommandCount++;
        }
    }

    private void ApplyDanmuCommand(DanmuCommand command)
    {
        switch (command.type)
        {
            case DanmuCommandType.SpawnUnit:
                ApplyDanmuSpawn(command);
                break;
            case DanmuCommandType.CastSkill:
                ApplyDanmuSkill(command);
                break;
            case DanmuCommandType.Heal:
                ApplyDanmuHeal(command);
                break;
            case DanmuCommandType.Buff:
            case DanmuCommandType.AddEnergy:
                ApplyDanmuBuff(command);
                break;
        }
    }

    private void ApplyDanmuSpawn(DanmuCommand command)
    {
        if (command.team == BattleTeam.Human)
        {
            bool spawned = command.key == "tank"
                ? ReviveTankFromDanmu(command)
                : command.key == "medic"
                    ? HealHumanForces(22f)
                    : ReviveSoldierFromDanmu(command);
            if (!spawned)
            {
                HealHumanForces(10f);
            }

            ShowBanner("Danmu human reinforce", false, 0.85f);
            return;
        }

        bool revived = ReviveGiantFromDanmu(command);
        if (!revived)
        {
            HealGiants(90f);
            HastenGiants(0.2f);
        }

        ShowBanner("Danmu monster reinforce", true, 0.85f);
    }

    private void ApplyDanmuSkill(DanmuCommand command)
    {
        if (command.team == BattleTeam.Human)
        {
            Vector2 center = GetActiveGiantCenter();
            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.Play(EffectPlayback.Create(BattleEffectId.HumanAirStrikeWarning, ToWorldPoint(center.x, center.y, 0.05f), Quaternion.identity, null, 2.2f));
                EffectManager.Instance.Play(EffectPlayback.Create(BattleEffectId.ExplosionLarge, ToWorldPoint(center.x, center.y, 0.35f), Quaternion.identity, null, 2.6f));
            }

            DamageGiantsInArea(center.x, center.y, 290f, 330f);
            SpawnEffect(center.x, center.y, 2.8f, EffectKind.Fireball, 0.34f);
            ShowBanner("Danmu air strike", true, 1.1f);
            return;
        }

        HealGiants(170f);
        HastenGiants(0.08f);
        if (EffectManager.Instance != null)
        {
            Vector2 center = GetActiveGiantCenter();
            EffectManager.Instance.Play(EffectPlayback.Create(BattleEffectId.OrcRageBuff, ToWorldPoint(center.x, center.y, 0.25f), Quaternion.identity, null, 2.8f));
        }

        ShowBanner("Danmu monster rage", true, 1.1f);
    }

    private void ApplyDanmuHeal(DanmuCommand command)
    {
        if (command.team == BattleTeam.Human)
        {
            HealHumanForces(36f + command.value * 0.35f);
            ShowBanner("Danmu human heal", false, 0.85f);
            return;
        }

        HealGiants(110f + command.value * 0.6f);
        ShowBanner("Danmu monster heal", true, 0.85f);
    }

    private void ApplyDanmuBuff(DanmuCommand command)
    {
        if (command.team == BattleTeam.Human)
        {
            ReduceHumanCooldowns(0.18f);
            ShowBanner("Danmu focus fire", false, 0.85f);
            return;
        }

        HastenGiants(0.18f);
        ShowBanner("Danmu monster haste", true, 0.85f);
    }

    private void CreateCoreScene()
    {
        CreateEventSystemIfNeeded();

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        mainCamera = cameraObject.AddComponent<Camera>();
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = BackgroundColor;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 180f;
        mainCamera.fieldOfView = 31f;
        mainCamera.transform.position = new Vector3(0f, 22f, -20f);
        mainCamera.transform.rotation = Quaternion.Euler(18f, 0f, 0f);

        orbitCamera = cameraObject.AddComponent<OrbitTouchCamera>();
        orbitCamera.yaw = cameraYaw;
        orbitCamera.pitch = cameraPitch;
        orbitCamera.distance = cameraDistance;
        orbitCamera.minPitch = 12f;
        orbitCamera.maxPitch = 64f;
        orbitCamera.minDistance = 42f;
        orbitCamera.maxDistance = 112f;
        orbitCamera.panXBounds = new Vector2(-10.5f, 10.5f);
        orbitCamera.panZBounds = new Vector2(-14f, 14f);

        cameraTarget = new GameObject("Camera Target").transform;
        cameraTarget.position = new Vector3(1.0f, 1.45f, -5.8f);
        orbitCamera.target = cameraTarget;

        var lightObject = new GameObject("Sun Light");
        lightObject.transform.rotation = Quaternion.Euler(54f, -35f, 0f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.88f, 1f);
        light.intensity = 1.25f;
        light.shadows = LightShadows.Soft;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.44f, 0.51f, 0.61f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = BackgroundColor;
        RenderSettings.fogDensity = 0.012f;

        worldRoot = new GameObject("WorldRoot").transform;
        decorRoot = new GameObject("DecorRoot").transform;
        unitRoot = new GameObject("UnitRoot").transform;
        projectileRoot = new GameObject("ProjectileRoot").transform;
        effectRoot = new GameObject("EffectRoot").transform;
        modelCacheRoot = new GameObject("ModelCacheRoot").transform;

        worldRoot.SetParent(transform, false);
        decorRoot.SetParent(worldRoot, false);
        unitRoot.SetParent(worldRoot, false);
        projectileRoot.SetParent(worldRoot, false);
        effectRoot.SetParent(worldRoot, false);
        modelCacheRoot.SetParent(transform, false);

        cameraObject.transform.SetParent(transform, false);
    }

    private void CreateHud()
    {
        uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720f, 1280f);
        scaler.matchWidthOrHeight = 0.55f;

        var hudRootObject = new GameObject("HudRoot", typeof(RectTransform));
        hudRootObject.transform.SetParent(canvas.transform, false);
        hudRoot = hudRootObject.GetComponent<RectTransform>();
        ApplySafeArea();

        var topPanel = CreatePanel(hudRoot, "TopPanel", new Color(0.03f, 0.035f, 0.045f, 0.88f));
        SetAnchors(topPanel.rectTransform, 0.035f, 0.89f, 0.965f, 0.992f);

        leftTeamLabel = CreateText(topPanel.transform, "LeftTeamLabel", "BLUE FORCE", 15, HumanColor, TextAnchor.MiddleLeft);
        SetAnchors(leftTeamLabel.rectTransform, 0.03f, 0.66f, 0.25f, 0.93f);
        ConfigureTextFit(leftTeamLabel, 10, 15);

        rightTeamLabel = CreateText(topPanel.transform, "RightTeamLabel", "MONSTER", 15, GiantColor, TextAnchor.MiddleRight);
        SetAnchors(rightTeamLabel.rectTransform, 0.75f, 0.66f, 0.97f, 0.93f);
        ConfigureTextFit(rightTeamLabel, 10, 15);

        battlePhaseLabel = CreateText(topPanel.transform, "BattlePhaseLabel", "LIVE BARRAGE WAR", 12, new Color(0.78f, 0.82f, 0.86f, 1f), TextAnchor.MiddleCenter);
        SetAnchors(battlePhaseLabel.rectTransform, 0.30f, 0.72f, 0.70f, 0.94f);
        ConfigureTextFit(battlePhaseLabel, 9, 12);

        poolLabel = CreateText(topPanel.transform, "PoolLabel", "POINT POOL 000,000", 24, new Color(1f, 0.85f, 0.34f, 1f), TextAnchor.MiddleCenter);
        SetAnchors(poolLabel.rectTransform, 0.24f, 0.42f, 0.76f, 0.80f);
        ConfigureTextFit(poolLabel, 16, 24);

        timerLabel = CreateText(topPanel.transform, "TimerLabel", "03:00", 18, Color.white, TextAnchor.MiddleCenter);
        SetAnchors(timerLabel.rectTransform, 0.40f, 0.18f, 0.60f, 0.46f);
        ConfigureTextFit(timerLabel, 13, 18);

        var humanPowerBack = CreatePanel(topPanel.transform, "HumanPowerBack", new Color(0.06f, 0.12f, 0.18f, 1f));
        SetAnchors(humanPowerBack.rectTransform, 0.03f, 0.03f, 0.47f, 0.17f);

        humanPowerFill = CreatePanel(humanPowerBack.transform, "HumanPowerFill", new Color(0.24f, 0.70f, 1f, 1f));
        humanPowerFill.type = Image.Type.Filled;
        humanPowerFill.fillMethod = Image.FillMethod.Horizontal;
        humanPowerFill.fillOrigin = 0;
        SetAnchors(humanPowerFill.rectTransform, 0f, 0f, 1f, 1f);

        var monsterPowerBack = CreatePanel(topPanel.transform, "MonsterPowerBack", new Color(0.18f, 0.08f, 0.07f, 1f));
        SetAnchors(monsterPowerBack.rectTransform, 0.53f, 0.03f, 0.97f, 0.17f);

        monsterPowerFill = CreatePanel(monsterPowerBack.transform, "MonsterPowerFill", GiantColor);
        monsterPowerFill.type = Image.Type.Filled;
        monsterPowerFill.fillMethod = Image.FillMethod.Horizontal;
        monsterPowerFill.fillOrigin = 1;
        SetAnchors(monsterPowerFill.rectTransform, 0f, 0f, 1f, 1f);
        hpFill = monsterPowerFill;

        humanLabel = CreateText(topPanel.transform, "HumanLabel", "Force 0/0", 12, Color.white, TextAnchor.MiddleLeft);
        SetAnchors(humanLabel.rectTransform, 0.03f, 0.18f, 0.34f, 0.36f);
        ConfigureTextFit(humanLabel, 9, 12);

        giantLabel = CreateText(topPanel.transform, "GiantLabel", "Boss HP 0", 12, Color.white, TextAnchor.MiddleRight);
        SetAnchors(giantLabel.rectTransform, 0.66f, 0.18f, 0.97f, 0.36f);
        ConfigureTextFit(giantLabel, 9, 12);

        var bottomPanel = CreatePanel(hudRoot, "LiveBottomPanel", new Color(0.025f, 0.03f, 0.04f, 0.84f));
        SetAnchors(bottomPanel.rectTransform, 0.035f, 0.050f, 0.965f, 0.158f);

        bottomTickerLabel = CreateText(bottomPanel.transform, "BottomTickerLabel", "Barrage connected", 14, new Color(0.94f, 0.97f, 1f, 1f), TextAnchor.MiddleLeft);
        SetAnchors(bottomTickerLabel.rectTransform, 0.03f, 0.56f, 0.72f, 0.88f);
        ConfigureTextFit(bottomTickerLabel, 10, 14);

        giftFeedLabel = CreateText(bottomPanel.transform, "GiftFeedLabel", "Gift heat 0", 13, new Color(1f, 0.83f, 0.38f, 1f), TextAnchor.MiddleLeft);
        SetAnchors(giftFeedLabel.rectTransform, 0.03f, 0.24f, 0.62f, 0.56f);
        ConfigureTextFit(giftFeedLabel, 9, 13);

        statusLabel = CreateText(bottomPanel.transform, "StatusLabel", "Ready", 12, new Color(0.70f, 0.78f, 0.84f, 1f), TextAnchor.MiddleLeft);
        SetAnchors(statusLabel.rectTransform, 0.03f, 0.04f, 0.62f, 0.26f);
        ConfigureTextFit(statusLabel, 8, 12);

        skillCountdownLabel = CreateText(bottomPanel.transform, "SkillCountdownLabel", "Skill CD 00s", 14, new Color(0.78f, 1f, 0.82f, 1f), TextAnchor.MiddleRight);
        SetAnchors(skillCountdownLabel.rectTransform, 0.66f, 0.18f, 0.97f, 0.84f);
        ConfigureTextFit(skillCountdownLabel, 10, 14);

        bannerLabel = CreateText(hudRoot, "BannerLabel", string.Empty, 28, new Color(1f, 0.94f, 0.6f, 1f), TextAnchor.MiddleCenter);
        SetAnchors(bannerLabel.rectTransform, 0.15f, 0.80f, 0.85f, 0.865f);
        ConfigureTextFit(bannerLabel, 18, 28);
        bannerLabel.gameObject.SetActive(false);

        loadingPanel = CreatePanel(canvas.transform, "LoadingPanel", new Color(0.02f, 0.03f, 0.05f, 0.90f));
        SetAnchors(loadingPanel.rectTransform, 0f, 0f, 1f, 1f);
        loadingPanel.rectTransform.offsetMin = Vector2.zero;
        loadingPanel.rectTransform.offsetMax = Vector2.zero;

        loadingLabel = CreateText(loadingPanel.transform, "LoadingLabel", "Loading 3D models...", 26, Color.white, TextAnchor.MiddleCenter);
        SetAnchors(loadingLabel.rectTransform, 0.15f, 0.45f, 0.85f, 0.58f);
        loadingLabel.rectTransform.anchoredPosition = Vector2.zero;

        CreateResolutionControls();
        RefreshResolutionControls();
    }

    private void CreateBattlefield()
    {
        buildingObstacles.Clear();
        CreateGround();
        CreateTerrainDepth();
        CreateVillagePaths();
        CreateMedievalVillage();
        CreateFactionFrontlines();
    }

    private void CreateGround()
    {
        var ground = CreatePrimitive(PrimitiveType.Cube, "TerrainBase", decorRoot);
        ground.transform.localScale = new Vector3(150f, 0.32f, 210f);
        ground.transform.localPosition = new Vector3(0f, -0.18f, 0f);
        ground.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.21f, 0.29f, 0.16f, 1f));
    }

    private void CreateTerrainDepth()
    {
        Color ridgeColor = new Color(0.26f, 0.34f, 0.19f, 1f);
        Color bankColor = new Color(0.16f, 0.24f, 0.14f, 1f);

        CreateBattlefieldBlock("NorthDistantRidge", new Vector3(0f, 0.32f, 56f), new Vector3(110f, 0.55f, 2.2f), ridgeColor);
        CreateBattlefieldBlock("SouthDistantRidge", new Vector3(0f, 0.22f, -62f), new Vector3(116f, 0.38f, 2.4f), bankColor);
        CreateBattlefieldBlock("WestDistantBank", new Vector3(-42f, 0.24f, 0f), new Vector3(2.2f, 0.45f, 126f), ridgeColor);
        CreateBattlefieldBlock("EastDistantBank", new Vector3(44f, 0.24f, 0f), new Vector3(2.0f, 0.42f, 126f), ridgeColor);

    }

    private void CreateVillageLandingFields()
    {
        for (int i = 0; i < AirLanes.Length; i++)
        {
            float x = (Left + 52f + i * 126f) * LogicalToWorld;
            float z = AirLanes[i] * LogicalToWorld;
            var deck = CreateBattlefieldBlock($"VillageLandingField_{i}", new Vector3(x, 0.06f, z), new Vector3(1.72f, 0.07f, 1.22f), new Color(0.31f, 0.24f, 0.15f, 1f));

            var plankA = CreateBattlefieldBlock($"VillageLandingPlankA_{i}", new Vector3(x, 0.12f, z), new Vector3(1.18f, 0.035f, 0.08f), new Color(0.45f, 0.30f, 0.17f, 1f));
            var plankB = CreateBattlefieldBlock($"VillageLandingPlankB_{i}", new Vector3(x, 0.13f, z), new Vector3(0.08f, 0.035f, 0.86f), new Color(0.45f, 0.30f, 0.17f, 1f));
            deck.transform.localRotation = Quaternion.Euler(0f, i * 8f - 8f, 0f);
            plankA.transform.localRotation = deck.transform.localRotation;
            plankB.transform.localRotation = deck.transform.localRotation;
        }
    }

    private GameObject CreateBattlefieldBlock(string name, Vector3 position, Vector3 scale, Color color)
    {
        var block = CreatePrimitive(PrimitiveType.Cube, name, decorRoot);
        block.transform.localScale = scale;
        block.transform.localPosition = position;
        block.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(color);
        return block;
    }

    private void CreateVillagePaths()
    {
        var mainRoad = CreateBattlefieldBlock("VillageDirtRoad_Main", ToWorldPoint(0f, -42f, 0.025f), new Vector3(20.6f, 0.06f, 4.7f), RoadColor);
        mainRoad.transform.localRotation = Quaternion.Euler(0f, -3f, 0f);

        var crossRoad = CreateBattlefieldBlock("VillageDirtRoad_Cross", ToWorldPoint(48f, 104f, 0.035f), new Vector3(4.1f, 0.055f, 18.4f), new Color(0.36f, 0.27f, 0.17f, 1f));
        crossRoad.transform.localRotation = Quaternion.Euler(0f, 8f, 0f);

    }

    private void CreateMedievalVillage()
    {
        if (CreateMegaKitMedievalVillage())
        {
            return;
        }

        CreatePrimitiveMedievalVillage();
    }

    private void CreatePrimitiveMedievalVillage()
    {
        CreateVillageCottage("VillageForge", -224f, -30f, 92f, 76f, 1.00f, new Color(0.50f, 0.36f, 0.23f, 1f), new Color(0.24f, 0.12f, 0.08f, 1f), -7f);
        CreateVillageCottage("VillageTavern", -102f, -214f, 108f, 84f, 1.12f, new Color(0.54f, 0.40f, 0.26f, 1f), new Color(0.31f, 0.15f, 0.08f, 1f), 5f);
        CreateVillageCottage("VillageHouseNorth", -76f, 218f, 96f, 78f, 0.96f, new Color(0.57f, 0.43f, 0.29f, 1f), new Color(0.33f, 0.17f, 0.09f, 1f), -12f);
        CreateVillageCottage("VillageStable", 78f, 322f, 132f, 82f, 0.86f, new Color(0.42f, 0.30f, 0.18f, 1f), new Color(0.28f, 0.14f, 0.07f, 1f), 9f);
        CreateVillageCottage("VillageBarn", 150f, 176f, 120f, 94f, 1.10f, new Color(0.49f, 0.30f, 0.18f, 1f), new Color(0.24f, 0.10f, 0.06f, 1f), -8f);
        CreateVillageCottage("VillageGranary", 278f, 296f, 90f, 70f, 1.26f, new Color(0.58f, 0.45f, 0.28f, 1f), new Color(0.35f, 0.20f, 0.10f, 1f), 13f);

        CreateVillageTower("VillageChapel", 220f, -118f, 82f, 112f, 2.05f, new Color(0.55f, 0.50f, 0.42f, 1f), new Color(0.22f, 0.16f, 0.13f, 1f), 4f);
        CreateVillageWell(44f, 42f);
        CreateMarketStalls();
        CreateVillageFences();
    }

    private bool CreateMegaKitMedievalVillage()
    {
        CacheMedievalVillagePrefabs();
        if (medievalVillagePrefabCount == 0 || LoadMedievalVillagePrefab("Wall_Plaster_Straight") == null || LoadMedievalVillagePrefab("Roof_RoundTiles_4x4") == null)
        {
            return false;
        }

        CreateMegaKitHouse("VillageForge", -224f, -30f, 104f, 84f, -7f, false, 2, 2, "Roof_RoundTiles_4x4", 0.50f, true);
        CreateMegaKitHouse("VillageTavern", -102f, -214f, 132f, 92f, 5f, false, 3, 2, "Roof_RoundTiles_6x4", 0.52f, true);
        CreateMegaKitHouse("VillageHouseNorth", -76f, 218f, 108f, 88f, -12f, true, 2, 2, "Roof_RoundTiles_4x4", 0.50f, false);
        CreateMegaKitHouse("VillageStable", 78f, 322f, 148f, 92f, 9f, false, 3, 2, "Roof_RoundTiles_6x4", 0.52f, false);
        CreateMegaKitHouse("VillageBarn", 150f, 176f, 148f, 122f, -8f, true, 3, 3, "Roof_RoundTiles_6x6", 0.54f, true);
        CreateMegaKitHouse("VillageGranary", 278f, 296f, 104f, 84f, 13f, false, 2, 2, "Roof_RoundTiles_4x4", 0.50f, true);
        CreateMegaKitHouse("VillageGatehouse", 292f, -18f, 112f, 86f, -10f, true, 2, 2, "Roof_RoundTiles_4x4", 0.50f, true);
        CreateMegaKitTower("VillageChapel", 220f, -118f, 92f, 112f, 4f, 0.48f);
        CreateMegaKitMarketProps();
        CreateMegaKitFenceProps();
        return true;
    }

    private void CacheMedievalVillagePrefabs()
    {
        if (medievalVillagePrefabCount > 0)
        {
            return;
        }

        var prefabs = Resources.LoadAll<GameObject>(MedievalVillageResourceFolderPath);
        medievalVillagePrefabCount = prefabs != null ? prefabs.Length : 0;
        if (prefabs == null)
        {
            return;
        }

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null && !medievalVillagePrefabs.ContainsKey(prefabs[i].name))
            {
                medievalVillagePrefabs.Add(prefabs[i].name, prefabs[i]);
            }
        }
    }

    private GameObject LoadMedievalVillagePrefab(string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            return null;
        }

        GameObject prefab;
        if (medievalVillagePrefabs.TryGetValue(assetName, out prefab))
        {
            return prefab;
        }

        prefab = Resources.Load<GameObject>(MedievalVillageResourceFolderPath + "/" + assetName);
        if (prefab != null)
        {
            medievalVillagePrefabs[assetName] = prefab;
        }

        return prefab;
    }

    private void CreateMegaKitHouse(string name, float centerX, float centerZ, float width, float depth, float yaw, bool brick, int widthModules, int depthModules, string roofName, float scale, bool chimney)
    {
        var root = new GameObject(name);
        root.transform.SetParent(decorRoot, false);
        root.transform.localPosition = ToWorldPoint(centerX, centerZ, 0f);
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        root.transform.localScale = Vector3.one * scale;

        float module = 2f;
        float localWidth = Mathf.Max(module, widthModules * module);
        float localDepth = Mathf.Max(module, depthModules * module);
        string wall = brick ? "Wall_UnevenBrick_Straight" : "Wall_Plaster_Straight";
        string door = brick ? "Wall_UnevenBrick_Door_Flat" : "Wall_Plaster_Door_Flat";
        string window = brick ? "Wall_UnevenBrick_Window_Wide_Flat" : "Wall_Plaster_Window_Wide_Flat";
        string corner = brick ? "Corner_Exterior_Brick" : "Corner_Exterior_Wood";

        for (int i = 0; i < widthModules; i++)
        {
            float x = -localWidth * 0.5f + module * 0.5f + i * module;
            string frontAsset = i == widthModules / 2 ? door : window;
            CreateMegaKitModule(frontAsset, root.transform, new Vector3(x, 0f, -localDepth * 0.5f), Quaternion.identity, Vector3.one);
            CreateMegaKitModule(window, root.transform, new Vector3(x, 0f, localDepth * 0.5f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
        }

        for (int i = 0; i < depthModules; i++)
        {
            float z = -localDepth * 0.5f + module * 0.5f + i * module;
            CreateMegaKitModule(wall, root.transform, new Vector3(-localWidth * 0.5f, 0f, z), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            CreateMegaKitModule(wall, root.transform, new Vector3(localWidth * 0.5f, 0f, z), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        }

        CreateMegaKitModule(corner, root.transform, new Vector3(-localWidth * 0.5f, 0f, -localDepth * 0.5f), Quaternion.identity, Vector3.one);
        CreateMegaKitModule(corner, root.transform, new Vector3(localWidth * 0.5f, 0f, -localDepth * 0.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        CreateMegaKitModule(corner, root.transform, new Vector3(localWidth * 0.5f, 0f, localDepth * 0.5f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
        CreateMegaKitModule(corner, root.transform, new Vector3(-localWidth * 0.5f, 0f, localDepth * 0.5f), Quaternion.Euler(0f, 270f, 0f), Vector3.one);

        CreateMegaKitModule(roofName, root.transform, new Vector3(0f, 3.0f, 0f), Quaternion.identity, Vector3.one);
        string roofFront = widthModules >= 3 ? "Roof_Front_Brick6" : "Roof_Front_Brick4";
        CreateMegaKitModule(roofFront, root.transform, new Vector3(0f, 3.0f, -localDepth * 0.5f - 0.02f), Quaternion.identity, Vector3.one);
        CreateMegaKitModule(roofFront, root.transform, new Vector3(0f, 3.0f, localDepth * 0.5f + 0.02f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);

        CreateMegaKitModule("Door_1_Flat", root.transform, new Vector3(0f, 0.02f, -localDepth * 0.5f - 0.07f), Quaternion.identity, Vector3.one);
        CreateMegaKitModule("Stairs_Exterior_Straight_Center", root.transform, new Vector3(0f, 0f, -localDepth * 0.5f - 0.92f), Quaternion.identity, Vector3.one * 0.72f);
        CreateMegaKitModule("Overhang_Plaster_Long", root.transform, new Vector3(0f, 2.35f, -localDepth * 0.5f - 0.16f), Quaternion.identity, Vector3.one);

        if (chimney)
        {
            CreateMegaKitModule("Prop_Chimney", root.transform, new Vector3(localWidth * 0.23f, 3.45f, 0.25f), Quaternion.Euler(0f, 12f, 0f), Vector3.one * 0.92f);
        }

        AddBuildingObstacle(name, centerX, centerZ, width * 0.5f, depth * 0.5f, 2.2f, 10f);
    }

    private void CreateMegaKitTower(string name, float centerX, float centerZ, float width, float depth, float yaw, float scale)
    {
        var root = new GameObject(name);
        root.transform.SetParent(decorRoot, false);
        root.transform.localPosition = ToWorldPoint(centerX, centerZ, 0f);
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        root.transform.localScale = Vector3.one * scale;

        const float localSize = 4f;
        for (int level = 0; level < 2; level++)
        {
            float y = level * 3f;
            string front = level == 0 ? "Wall_UnevenBrick_Door_Round" : "Wall_UnevenBrick_Window_Thin_Round";
            string side = level == 0 ? "Wall_UnevenBrick_Straight" : "Wall_UnevenBrick_Window_Wide_Round";
            CreateMegaKitModule(front, root.transform, new Vector3(0f, y, -localSize * 0.5f), Quaternion.identity, Vector3.one);
            CreateMegaKitModule(side, root.transform, new Vector3(0f, y, localSize * 0.5f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            CreateMegaKitModule(side, root.transform, new Vector3(-localSize * 0.5f, y, 0f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            CreateMegaKitModule(side, root.transform, new Vector3(localSize * 0.5f, y, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            CreateMegaKitModule("Corner_Exterior_Brick", root.transform, new Vector3(-localSize * 0.5f, y, -localSize * 0.5f), Quaternion.identity, Vector3.one);
            CreateMegaKitModule("Corner_Exterior_Brick", root.transform, new Vector3(localSize * 0.5f, y, -localSize * 0.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            CreateMegaKitModule("Corner_Exterior_Brick", root.transform, new Vector3(localSize * 0.5f, y, localSize * 0.5f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            CreateMegaKitModule("Corner_Exterior_Brick", root.transform, new Vector3(-localSize * 0.5f, y, localSize * 0.5f), Quaternion.Euler(0f, 270f, 0f), Vector3.one);
        }

        CreateMegaKitModule("Roof_Tower_RoundTiles", root.transform, new Vector3(0f, 6f, 0f), Quaternion.identity, Vector3.one);
        CreateMegaKitModule("Door_1_Round", root.transform, new Vector3(0f, 0.02f, -localSize * 0.5f - 0.08f), Quaternion.identity, Vector3.one);
        CreateMegaKitModule("Stairs_Exterior_Straight_Center", root.transform, new Vector3(0f, 0f, -localSize * 0.5f - 0.92f), Quaternion.identity, Vector3.one * 0.72f);
        AddBuildingObstacle(name, centerX, centerZ, width * 0.5f, depth * 0.5f, 3.8f, 12f);
    }

    private void CreateMegaKitMarketProps()
    {
        CreateMegaKitPlacedProp("Prop_Wagon", -4f, 142f, -18f, 0.46f);
        CreateMegaKitPlacedProp("Prop_Crate", 28f, 144f, 16f, 0.48f);
        CreateMegaKitPlacedProp("Prop_Crate", 58f, 174f, -9f, 0.48f);
        CreateMegaKitPlacedProp("Balcony_Cross_Straight", -34f, 176f, 7f, 0.38f);
        CreateMegaKitPlacedProp("Prop_MetalFence_Ornament", 88f, 134f, 92f, 0.40f);
    }

    private void CreateMegaKitFenceProps()
    {
        for (int i = 0; i < 12; i++)
        {
            float x = -296f + i * 54f;
            float z = i % 2 == 0 ? 372f : -344f;
            string asset = i % 3 == 0 ? "Prop_WoodenFence_Extension1" : (i % 3 == 1 ? "Prop_WoodenFence_Extension2" : "Prop_WoodenFence_Single");
            CreateMegaKitPlacedProp(asset, x, z, -12f + Noise(i + 701f) * 24f, 0.42f);
        }
    }

    private void CreateMegaKitPlacedProp(string assetName, float centerX, float centerZ, float yaw, float scale)
    {
        var root = new GameObject("Village_" + assetName);
        root.transform.SetParent(decorRoot, false);
        root.transform.localPosition = ToWorldPoint(centerX, centerZ, 0f);
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        root.transform.localScale = Vector3.one * scale;
        CreateMegaKitModule(assetName, root.transform, Vector3.zero, Quaternion.identity, Vector3.one);
    }

    private GameObject CreateMegaKitModule(string assetName, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        var prefab = LoadMedievalVillagePrefab(assetName);
        if (prefab == null)
        {
            return null;
        }

        var instance = Instantiate(prefab, parent, false);
        instance.name = assetName;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = localRotation;
        instance.transform.localScale = localScale;
        ConfigureMedievalVillageInstance(instance);
        return instance;
    }

    private void ConfigureMedievalVillageInstance(GameObject instance)
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

    private void CreateVillageCottage(string name, float centerX, float centerZ, float width, float depth, float bodyHeight, Color wallColor, Color roofColor, float yaw)
    {
        var root = new GameObject(name);
        root.transform.SetParent(decorRoot, false);
        root.transform.localPosition = ToWorldPoint(centerX, centerZ, 0f);
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        var body = CreatePrimitive(PrimitiveType.Cube, $"{name}_Body", root.transform);
        body.transform.localScale = LogicalScale(width, bodyHeight, depth);
        body.transform.localPosition = new Vector3(0f, bodyHeight * 0.5f, 0f);
        body.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(wallColor);

        var roofLeft = CreatePrimitive(PrimitiveType.Cube, $"{name}_RoofLeft", root.transform);
        roofLeft.transform.localScale = new Vector3(width * LogicalToWorld * 0.72f, 0.24f, depth * LogicalToWorld * 1.18f);
        roofLeft.transform.localPosition = new Vector3(-width * LogicalToWorld * 0.18f, bodyHeight + 0.22f, 0f);
        roofLeft.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
        roofLeft.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(roofColor);

        var roofRight = CreatePrimitive(PrimitiveType.Cube, $"{name}_RoofRight", root.transform);
        roofRight.transform.localScale = roofLeft.transform.localScale;
        roofRight.transform.localPosition = new Vector3(width * LogicalToWorld * 0.18f, bodyHeight + 0.22f, 0f);
        roofRight.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
        roofRight.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(roofColor);

        var door = CreatePrimitive(PrimitiveType.Cube, $"{name}_Door", root.transform);
        door.transform.localScale = new Vector3(width * LogicalToWorld * 0.18f, bodyHeight * 0.48f, 0.035f);
        door.transform.localPosition = new Vector3(0f, bodyHeight * 0.26f, -depth * LogicalToWorld * 0.515f);
        door.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.18f, 0.09f, 0.04f, 1f));

        for (int side = -1; side <= 1; side += 2)
        {
            var window = CreatePrimitive(PrimitiveType.Cube, side < 0 ? $"{name}_WindowL" : $"{name}_WindowR", root.transform);
            window.transform.localScale = new Vector3(width * LogicalToWorld * 0.12f, bodyHeight * 0.18f, 0.032f);
            window.transform.localPosition = new Vector3(side * width * LogicalToWorld * 0.28f, bodyHeight * 0.58f, -depth * LogicalToWorld * 0.518f);
            window.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.95f, 0.73f, 0.34f, 1f));
        }

        AddBuildingObstacle(name, centerX, centerZ, width * 0.5f, depth * 0.5f, bodyHeight + 0.6f, 8f);
    }

    private void CreateVillageTower(string name, float centerX, float centerZ, float width, float depth, float bodyHeight, Color wallColor, Color roofColor, float yaw)
    {
        var root = new GameObject(name);
        root.transform.SetParent(decorRoot, false);
        root.transform.localPosition = ToWorldPoint(centerX, centerZ, 0f);
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        var baseBlock = CreatePrimitive(PrimitiveType.Cube, $"{name}_Nave", root.transform);
        baseBlock.transform.localScale = LogicalScale(width, 1.0f, depth);
        baseBlock.transform.localPosition = new Vector3(0f, 0.50f, 0f);
        baseBlock.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(wallColor);

        var tower = CreatePrimitive(PrimitiveType.Cube, $"{name}_Tower", root.transform);
        tower.transform.localScale = LogicalScale(width * 0.48f, bodyHeight, depth * 0.44f);
        tower.transform.localPosition = new Vector3(0f, bodyHeight * 0.5f, -depth * LogicalToWorld * 0.24f);
        tower.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.48f, 0.45f, 0.39f, 1f));

        var roof = CreatePrimitive(PrimitiveType.Cylinder, $"{name}_Spire", root.transform);
        roof.transform.localScale = new Vector3(width * LogicalToWorld * 0.34f, 0.42f, width * LogicalToWorld * 0.34f);
        roof.transform.localPosition = new Vector3(0f, bodyHeight + 0.36f, -depth * LogicalToWorld * 0.24f);
        roof.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(roofColor);

        AddBuildingObstacle(name, centerX, centerZ, width * 0.5f, depth * 0.5f, bodyHeight + 0.8f, 10f);
    }

    private void CreateVillageWell(float centerX, float centerZ)
    {
        var baseRing = CreatePrimitive(PrimitiveType.Cylinder, "VillageWell_StoneRing", decorRoot);
        baseRing.transform.localScale = new Vector3(0.54f, 0.16f, 0.54f);
        baseRing.transform.localPosition = ToWorldPoint(centerX, centerZ, 0.16f);
        baseRing.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.38f, 0.36f, 0.31f, 1f));

        var beam = CreateBattlefieldBlock("VillageWell_CrossBeam", ToWorldPoint(centerX, centerZ, 0.78f), new Vector3(1.12f, 0.10f, 0.12f), new Color(0.30f, 0.17f, 0.08f, 1f));
        beam.transform.localRotation = Quaternion.Euler(0f, -16f, 0f);

        AddBuildingObstacle("VillageWell", centerX, centerZ, 28f, 28f, 0.9f, 6f);
    }

    private void CreateMarketStalls()
    {
        Color clothBlue = new Color(0.20f, 0.38f, 0.56f, 1f);
        Color clothRed = new Color(0.55f, 0.20f, 0.15f, 1f);
        for (int i = 0; i < 4; i++)
        {
            float x = -22f + i * 33f;
            float z = 138f + (i % 2) * 36f;
            var table = CreateBattlefieldBlock($"VillageMarketTable_{i}", ToWorldPoint(x, z, 0.22f), new Vector3(0.76f, 0.18f, 0.44f), new Color(0.33f, 0.19f, 0.09f, 1f));
            table.transform.localRotation = Quaternion.Euler(0f, -18f + i * 9f, 0f);

            var canopy = CreateBattlefieldBlock($"VillageMarketCanopy_{i}", ToWorldPoint(x, z, 0.70f), new Vector3(0.92f, 0.08f, 0.62f), i % 2 == 0 ? clothBlue : clothRed);
            canopy.transform.localRotation = table.transform.localRotation;
        }
    }

    private void CreateVillageFences()
    {
        Color fenceColor = new Color(0.32f, 0.20f, 0.10f, 1f);
        for (int i = 0; i < 18; i++)
        {
            float x = -318f + i * 38f;
            float z = i % 2 == 0 ? 382f : -354f;
            var fence = CreateBattlefieldBlock($"VillageFence_{i}", ToWorldPoint(x, z, 0.24f), new Vector3(0.78f, 0.18f, 0.10f), fenceColor);
            fence.transform.localRotation = Quaternion.Euler(0f, -12f + Noise(i + 701f) * 24f, 0f);
        }
    }

    private Vector3 LogicalScale(float width, float height, float depth)
    {
        return new Vector3(width * LogicalToWorld, height, depth * LogicalToWorld);
    }

    private void AddBuildingObstacle(string name, float centerX, float centerZ, float halfX, float halfZ, float height, float padding)
    {
        buildingObstacles.Add(new BuildingObstacle(name, centerX, centerZ, halfX, halfZ, height, padding));
    }

    private void CreateRoad()
    {
        var road = CreatePrimitive(PrimitiveType.Cube, "Road", decorRoot);
        road.transform.localScale = new Vector3(18.6f, 0.08f, 8.8f);
        road.transform.localPosition = new Vector3(0f, 0.02f, -1.3f);
        road.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(RoadColor);

        for (int i = 0; i < 7; i++)
        {
            var stripe = CreatePrimitive(PrimitiveType.Cube, $"RoadStripe_{i}", decorRoot);
            stripe.transform.localScale = new Vector3(0.9f, 0.02f, 0.08f);
            stripe.transform.localPosition = new Vector3(-7.5f + i * 2.5f, 0.08f, -1.15f);
            stripe.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.55f, 0.59f, 0.55f, 1f));
        }
    }

    private void CreateFactionFrontlines()
    {
        var blueBase = CreateBattlefieldBlock("BlueFrontlineBase", new Vector3(-8.25f, 0.10f, -6.8f), new Vector3(2.4f, 0.14f, 6.4f), new Color(0.09f, 0.22f, 0.31f, 1f));
        blueBase.transform.localRotation = Quaternion.Euler(0f, -7f, 0f);

        var blueMarker = CreateBattlefieldBlock("BlueFrontlineMarker", new Vector3(-9.2f, 0.12f, -5.1f), new Vector3(0.58f, 0.16f, 0.52f), new Color(0.12f, 0.42f, 0.63f, 1f));
        blueMarker.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);

        var redBase = CreateBattlefieldBlock("RedFrontlineBase", new Vector3(8.25f, 0.10f, -6.8f), new Vector3(2.4f, 0.14f, 6.4f), new Color(0.27f, 0.15f, 0.11f, 1f));
        redBase.transform.localRotation = Quaternion.Euler(0f, 11f, 0f);

        var redMarker = CreateBattlefieldBlock("RedFrontlineMarker", new Vector3(9.2f, 0.12f, -4.9f), new Vector3(0.64f, 0.16f, 0.54f), new Color(0.73f, 0.29f, 0.18f, 1f));
        redMarker.transform.localRotation = Quaternion.Euler(0f, -12f, 0f);

        for (int i = 0; i < 3; i++)
        {
            float z = -11.2f + i * 4.6f;
            var blueMark = CreateBattlefieldBlock($"BlueFrontlineMark_{i}", new Vector3(-6.9f + Noise(i + 211f) * 0.45f, 0.05f, z), new Vector3(1.15f, 0.04f, 0.74f), new Color(0.14f, 0.50f, 0.74f, 1f));
            blueMark.transform.localRotation = Quaternion.Euler(0f, -10f + i * 6f, 0f);

            var redMark = CreateBattlefieldBlock($"RedFrontlineMark_{i}", new Vector3(6.9f - Noise(i + 257f) * 0.45f, 0.05f, z + 0.4f), new Vector3(1.15f, 0.04f, 0.74f), new Color(0.78f, 0.31f, 0.20f, 1f));
            redMark.transform.localRotation = Quaternion.Euler(0f, 10f - i * 6f, 0f);
        }
    }

    private void CreateCentralRuinWall()
    {
        Color wallA = new Color(0.31f, 0.25f, 0.20f, 1f);
        Color wallB = new Color(0.20f, 0.19f, 0.18f, 1f);

        for (int i = 0; i < 9; i++)
        {
            float height = 0.95f + Noise(i + 610f) * 3.7f;
            float width = 0.42f + Noise(i + 661f) * 0.3f;
            float depth = 0.78f + Noise(i + 705f) * 0.32f;
            float x = 0.84f + Noise(i + 722f) * 0.72f;
            float z = -12.2f + i * 3.0f + Noise(i + 753f) * 0.35f;
            var block = CreateBattlefieldBlock($"CentralRuinWall_{i}", new Vector3(x, height * 0.5f, z), new Vector3(width, height, depth), i % 2 == 0 ? wallA : wallB);
            block.transform.localRotation = Quaternion.Euler(0f, -16f + Noise(i + 772f) * 30f, Noise(i + 793f) * 6f - 3f);
        }

        for (int i = 0; i < 4; i++)
        {
            float x = 0.38f + i * 0.53f;
            float z = -5.1f + i * 3.1f;
            var slab = CreateBattlefieldBlock($"CentralRuinSlab_{i}", new Vector3(x, 0.18f + i * 0.05f, z), new Vector3(1.15f + i * 0.16f, 0.18f + i * 0.06f, 0.95f), new Color(0.24f, 0.21f, 0.18f, 1f));
            slab.transform.localRotation = Quaternion.Euler(0f, 20f - i * 9f, -6f + i * 2f);
        }
    }

    private void CreateRuinedCity()
    {
        for (int i = 0; i < 12; i++)
        {
            float height = 1.6f + Noise(i + 18f) * 4.8f;
            float width = 0.55f + Noise(i + 7f) * 0.6f;
            float depth = 0.5f + Noise(i + 23f) * 0.7f;
            var tower = CreatePrimitive(PrimitiveType.Cube, $"Ruin_{i}", decorRoot);
            tower.transform.localScale = new Vector3(width, height, depth);
            tower.transform.localPosition = new Vector3(-8.6f + i * 1.05f, height * 0.5f, 2.6f + Noise(i + 11f) * 0.8f);
            tower.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(RuinColor);
        }

        for (int i = 0; i < 8; i++)
        {
            var rib = CreatePrimitive(PrimitiveType.Cube, $"RuinBeam_{i}", decorRoot);
            rib.transform.localScale = new Vector3(0.22f, 0.22f, 1.2f + Noise(i + 51f) * 0.7f);
            rib.transform.localPosition = new Vector3(-7.8f + i * 0.95f, 0.12f + i * 0.04f, 3.9f);
            rib.transform.localRotation = Quaternion.Euler(0f, 20f + Noise(i + 7f) * 30f, 0f);
            rib.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.19f, 0.29f, 0.33f, 1f));
        }
    }

    private void CreateLowVillageDebris()
    {
        Color slabA = new Color(0.34f, 0.32f, 0.27f, 1f);
        Color slabB = new Color(0.24f, 0.31f, 0.17f, 1f);

        for (int i = 0; i < 12; i++)
        {
            float x = -8.8f + (i % 6) * 2.4f + Noise(i + 1201f) * 0.55f;
            float z = -11.2f + (i / 6) * 18.5f + Noise(i + 1229f) * 2.0f;
            float height = 0.08f + Noise(i + 1247f) * 0.16f;
            var slab = CreateBattlefieldBlock($"VillageStonePatch_{i}", new Vector3(x, height * 0.5f + 0.02f, z), new Vector3(1.0f + Noise(i + 1277f) * 1.25f, height, 0.65f + Noise(i + 1301f) * 0.85f), i % 2 == 0 ? slabA : slabB);
            slab.transform.localRotation = Quaternion.Euler(0f, -28f + Noise(i + 1319f) * 56f, 0f);
        }

        for (int i = 0; i < 8; i++)
        {
            float x = -6.8f + i * 1.55f;
            float z = -2.9f + Noise(i + 1409f) * 6.6f;
            var hay = CreateBattlefieldBlock($"VillageHayBale_{i}", new Vector3(x, 0.15f, z), new Vector3(0.48f + Noise(i + 1433f) * 0.35f, 0.25f, 0.34f), new Color(0.68f, 0.52f, 0.20f, 1f));
            hay.transform.localRotation = Quaternion.Euler(0f, -20f + Noise(i + 1451f) * 40f, 0f);
        }
    }

    private void CreateHumanStagingArea()
    {
        for (int i = 0; i < 8; i++)
        {
            var supply = CreatePrimitive(PrimitiveType.Cube, $"VillageSupplyCrate_{i}", decorRoot);
            supply.transform.localScale = new Vector3(0.45f, 0.38f + Noise(i + 13f) * 0.24f, 0.45f);
            supply.transform.localPosition = new Vector3(-9.1f + i * 0.95f, supply.transform.localScale.y * 0.5f, -4.4f - i * 0.08f);
            supply.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.35f, 0.22f, 0.11f, 1f));
        }
    }

    private void CreateGiantEntry()
    {
        for (int i = 0; i < 8; i++)
        {
            var mound = CreatePrimitive(PrimitiveType.Cube, $"ForestMound_{i}", decorRoot);
            mound.transform.localScale = new Vector3(0.45f + i * 0.08f, 0.12f + i * 0.05f, 0.38f + i * 0.05f);
            mound.transform.localPosition = new Vector3(9.2f - i * 0.5f, mound.transform.localScale.y * 0.5f, -4.6f - i * 0.15f);
            mound.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.07f, 0.22f, 0.08f, 1f));
        }
    }

    private void CreateUnits()
    {
        soldiers.Clear();
        tanks.Clear();
        aircraft.Clear();
        giants.Clear();

        for (int i = 0; i < SoldierCount; i++)
        {
            soldiers.Add(CreateUnitShell(UnitKind.Soldier));
        }

        for (int i = 0; i < TankCount; i++)
        {
            TankModelVariant tankModel = i < TankT55ACount ? TankModelVariant.T55A : TankModelVariant.T55AK;
            tanks.Add(CreateUnitShell(UnitKind.Tank, tankModel));
        }

        for (int i = 0; i < AircraftCount; i++)
        {
            aircraft.Add(CreateUnitShell(UnitKind.Aircraft));
        }

        for (int i = 0; i < GiantCount; i++)
        {
            giants.Add(CreateUnitShell(UnitKind.Giant));
        }
    }

    private void EnsureUnitConfigs()
    {
        if (soldierConfig == null)
        {
            soldierConfig = ScriptableObject.CreateInstance<UnitConfig>();
            soldierConfig.Kind = UnitKind.Soldier;
            soldierConfig.MaxHp = 58f;
            soldierConfig.Damage = 5f;
            soldierConfig.MoveSpeed = 68f;
            soldierConfig.Radius = 18f;
            soldierConfig.AttackRange = 260f;
            soldierConfig.AttackInterval = 0.62f;
        }

        if (tankConfig == null)
        {
            tankConfig = ScriptableObject.CreateInstance<UnitConfig>();
            tankConfig.Kind = UnitKind.Tank;
            tankConfig.MaxHp = 270f;
            tankConfig.Damage = 85f;
            tankConfig.MoveSpeed = 34f;
            tankConfig.Radius = 34f;
            tankConfig.AttackRange = 430f;
            tankConfig.AttackInterval = 1.2f;
        }

        if (aircraftConfig == null)
        {
            aircraftConfig = ScriptableObject.CreateInstance<UnitConfig>();
            aircraftConfig.Kind = UnitKind.Aircraft;
            aircraftConfig.MaxHp = 180f;
            aircraftConfig.Damage = 76f;
            aircraftConfig.MoveSpeed = 84f;
            aircraftConfig.Radius = 54f;
            aircraftConfig.AttackRange = 520f;
            aircraftConfig.AttackInterval = 0.95f;
        }

        if (giantConfig == null)
        {
            giantConfig = ScriptableObject.CreateInstance<UnitConfig>();
            giantConfig.Kind = UnitKind.Giant;
            giantConfig.MaxHp = 2600f;
            giantConfig.Damage = 42f;
            giantConfig.MoveSpeed = 25f;
            giantConfig.Radius = 82f;
            giantConfig.AttackRange = 126f;
            giantConfig.AttackInterval = 1.12f;
        }
    }

    private BattleUnit CreateUnitShell(UnitKind kind, TankModelVariant tankModel = TankModelVariant.None)
    {
        var root = new GameObject($"Unit_{kind}_{nextId}");
        root.transform.SetParent(unitRoot, false);

        var shadow = CreatePrimitive(PrimitiveType.Cylinder, "Shadow", root.transform);
        shadow.transform.localScale = new Vector3(kind == UnitKind.Aircraft ? 0.85f : 1.05f, 0.03f, kind == UnitKind.Aircraft ? 0.85f : 1.35f);
        shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        shadow.GetComponent<Renderer>().sharedMaterial = GetTransparentMaterial(new Color(0f, 0f, 0f, kind == UnitKind.Aircraft ? 0.26f : 0.38f));

        var body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);

        Transform tankAimRoot = null;
        Transform tankTurretVisual = null;
        Transform tankBarrelVisual = null;
        Transform tankMuzzleVisual = null;
        if (kind == UnitKind.Tank)
        {
            tankAimRoot = new GameObject("TankAimRoot").transform;
            tankAimRoot.SetParent(body.transform, false);
            tankAimRoot.localPosition = new Vector3(0f, 0.43f, 0f);

            var turret = CreatePrimitive(PrimitiveType.Cylinder, "TankAimTurret", tankAimRoot);
            turret.transform.localScale = new Vector3(0.31f, 0.09f, 0.31f);
            turret.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.22f, 0.25f, 0.22f, 1f));
            tankTurretVisual = turret.transform;

            var barrel = CreatePrimitive(PrimitiveType.Cube, "TankAimBarrel", tankAimRoot);
            barrel.transform.localScale = new Vector3(0.12f, 0.10f, 1.18f);
            barrel.transform.localPosition = new Vector3(0f, 0.08f, 0.62f);
            barrel.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.13f, 0.15f, 0.13f, 1f));
            tankBarrelVisual = barrel.transform;

            var muzzle = CreatePrimitive(PrimitiveType.Sphere, "TankAimMuzzle", tankAimRoot);
            muzzle.transform.localScale = Vector3.one * 0.13f;
            muzzle.transform.localPosition = new Vector3(0f, 0.08f, 1.24f);
            muzzle.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.08f, 0.09f, 0.08f, 1f));
            tankMuzzleVisual = muzzle.transform;
        }

        root.SetActive(false);

        return new BattleUnit
        {
            id = nextId++,
            kind = kind,
            tankModel = tankModel,
            team = kind == UnitKind.Giant ? TeamKind.Giant : TeamKind.Human,
            root = root,
            body = body.transform,
            active = false,
            x = 0f,
            z = 0f,
            baseZ = 0f,
            altitude = kind == UnitKind.Aircraft ? 2.5f : 0f,
            hp = 1f,
            maxHp = 1f,
            damage = 1f,
            speed = 1f,
            radius = 1f,
            attackRange = 1f,
            attackInterval = 1f,
            attackCooldown = 0f,
            attackVisualTimer = 0f,
            seed = Noise(nextId * 17.37f),
            rank = 0,
            facing = 1,
            headingDegrees = kind == UnitKind.Giant ? -90f : 90f,
            turretYawDegrees = 90f,
            modelYawOffset = TankModelYawOffset(tankModel),
            tankAimRoot = tankAimRoot,
            tankTurretVisual = tankTurretVisual,
            tankBarrelVisual = tankBarrelVisual,
            tankMuzzleVisual = tankMuzzleVisual,
        };
    }

    private static float TankModelYawOffset(TankModelVariant tankModel)
    {
        switch (tankModel)
        {
            case TankModelVariant.T55A:
                return TankT55AYawOffset;
            case TankModelVariant.T55AK:
                return TankT55AkYawOffset;
            default:
                return 0f;
        }
    }

    private async Task LoadPrototypes()
    {
        string[] modelPaths =
        {
            ResolveSoldierModelPath(),
            ResolveTankModelPath(),
            "PolyPizza/helicopter.glb",
            ResolveGiantModelPath(),
            "PolyPizza/fireball.glb",
            "PolyPizza/smoke.glb",
        };

        UnitKind[] kinds =
        {
            UnitKind.Soldier,
            UnitKind.Tank,
            UnitKind.Aircraft,
            UnitKind.Giant,
            UnitKind.Fireball,
            UnitKind.Smoke,
        };

        for (int i = 0; i < modelPaths.Length; i++)
        {
            SetLoadingMessage($"Loading {kinds[i]} ({i + 1}/{modelPaths.Length})");
            var tankModel = kinds[i] == UnitKind.Tank ? TankModelVariant.T55A : TankModelVariant.None;
            var prototype = await LoadPrototype(modelPaths[i], kinds[i], tankModel);
            if (prototype == null)
            {
                DiagnosticsUsingFallback = true;
                prototype = CreateFallbackPrototype(kinds[i]);
            }

            modelPrototypes[kinds[i]] = prototype;
        }

        SetLoadingMessage("Loading T55AK Tank (7/7)");
        tankT55AkPrototype = await LoadPrototype(ResolveTankT55AkModelPath(), UnitKind.Tank, TankModelVariant.T55AK);
        if (tankT55AkPrototype == null)
        {
            DiagnosticsUsingFallback = true;
            if (!modelPrototypes.TryGetValue(UnitKind.Tank, out tankT55AkPrototype) || tankT55AkPrototype == null)
            {
                tankT55AkPrototype = CreateFallbackPrototype(UnitKind.Tank);
            }
        }

        ConfigureTankVariantPrototypes();
        ConfigureGiantVariantPrototypes();
    }

    private string ResolveSoldierModelPath()
    {
        string[] candidates =
        {
            "Quaternius/ZombieApocalypse/Characters_Sam_SingleWeapon.gltf",
            "PolyPizza/soldier.glb",
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            string localPath = Path.Combine(Application.streamingAssetsPath, candidates[i].Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(localPath))
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Length - 1];
    }

    private string ResolveTankModelPath()
    {
        string[] candidates =
        {
            "Sketchfab/tank_t-55a.glb",
            "Sketchfab/t55a-tank.glb",
            "Sketchfab/abrams-tank.glb",
            "Sketchfab/merkava-tank.glb",
            "PolyPizza/abrams-tank.glb",
            "PolyPizza/merkava-tank.glb",
            "PolyPizza/tank.glb",
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            string localPath = Path.Combine(Application.streamingAssetsPath, candidates[i].Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(localPath))
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Length - 1];
    }

    private string ResolveTankT55AkModelPath()
    {
        string candidate = "Sketchfab/t-55ak.glb";
        string localPath = Path.Combine(Application.streamingAssetsPath, candidate.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(localPath) ? candidate : ResolveTankModelPath();
    }

    private string ResolveGiantModelPath()
    {
        string[] candidates =
        {
            "Sketchfab/sevens_sin_helldog.glb",
            "PolyPizza/giant.glb",
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            string localPath = Path.Combine(Application.streamingAssetsPath, candidates[i].Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(localPath))
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Length - 1];
    }

    private async Task<GameObject> LoadPrototype(string modelPath, UnitKind kind, TankModelVariant tankModel = TankModelVariant.None)
    {
        if (kind == UnitKind.Soldier)
        {
            var soldierResourcePrototype = LoadSoldierResourcePrototype();
            if (soldierResourcePrototype != null)
            {
                return soldierResourcePrototype;
            }
        }
        else if (kind == UnitKind.Tank)
        {
            var tankResourcePrototype = LoadTankResourcePrototype(tankModel);
            if (tankResourcePrototype != null)
            {
                return tankResourcePrototype;
            }
        }
        else if (kind == UnitKind.Giant)
        {
            var giantResourcePrototype = LoadGiantResourcePrototype(GiantResourceModelPath);
            if (giantResourcePrototype != null)
            {
                return giantResourcePrototype;
            }
        }

        var loaderRoot = new GameObject($"GLTFLoader_{kind}");
        loaderRoot.transform.SetParent(modelCacheRoot, false);
        loaderRoot.hideFlags = HideFlags.HideInHierarchy;

        var gltf = loaderRoot.AddComponent<GLTFComponent>();
        gltf.GLTFUri = modelPath;
        gltf.LoadFromStreamingAssets = true;
        gltf.PlayAnimationOnLoad = kind != UnitKind.Soldier;
        gltf.ImportAnimationMethod = AnimationMethod.Legacy;
        gltf.AnimationLoopTime = true;
        gltf.AnimationLoopPose = false;
        gltf.HideSceneObjDuringLoad = true;
        gltf.loadOnStart = false;
        gltf.Multithreaded = true;
        gltf.Timeout = 12;
        gltf.KeepCPUCopyOfMesh = false;
        gltf.KeepCPUCopyOfTexture = false;
        gltf.ShaderOverride = FindRuntimeShader("RuntimeMaterials/RuntimeGltfPbrMetallicRoughness", "GLTF/PbrMetallicRoughness", "Standard", "Legacy Shaders/Diffuse");

        await gltf.Load();

        var scene = gltf.LastLoadedScene;
        if (scene == null)
        {
            return null;
        }

        scene.name = $"{kind}_Prototype";
        scene.transform.SetParent(loaderRoot.transform, false);
        AttachRuntimeAnimationClips(scene, gltf);
        ConfigureImportedPrototype(scene, kind);
        scene.SetActive(false);
        return scene;
    }

    private GameObject LoadSoldierResourcePrototype()
    {
        var source = Resources.Load<GameObject>(SoldierResourceModelPath);
        if (source == null)
        {
            return null;
        }

        var prototype = Instantiate(source, modelCacheRoot, false);
        prototype.name = $"{UnitKind.Soldier}_Prototype";
        prototype.hideFlags = HideFlags.HideInHierarchy;
        AttachResourceAnimationClips(prototype, SoldierResourceModelPath, SoldierResourceFolderPath);
        ConfigureImportedPrototype(prototype, UnitKind.Soldier);
        prototype.SetActive(false);
        return prototype;
    }

    private GameObject LoadTankResourcePrototype(TankModelVariant tankModel)
    {
        string resourcePath = tankModel == TankModelVariant.T55AK ? TankHeavyResourceModelPath : TankResourceModelPath;
        return LoadTankResourcePrototype(resourcePath);
    }

    private GameObject LoadTankResourcePrototype(string resourcePath)
    {
        var source = Resources.Load<GameObject>(resourcePath);
        if (source == null)
        {
            return null;
        }

        var prototype = Instantiate(source, modelCacheRoot, false);
        prototype.name = $"{UnitKind.Tank}_Prototype";
        prototype.hideFlags = HideFlags.HideInHierarchy;
        AttachResourceAnimationClips(prototype, resourcePath, TankResourceFolderPath);
        ConfigureImportedPrototype(prototype, UnitKind.Tank);
        prototype.SetActive(false);
        return prototype;
    }

    private GameObject LoadGiantResourcePrototype(string resourcePath)
    {
        var source = Resources.Load<GameObject>(resourcePath);
        if (source == null)
        {
            return null;
        }

        var prototype = Instantiate(source, modelCacheRoot, false);
        prototype.name = $"{UnitKind.Giant}_Prototype";
        prototype.hideFlags = HideFlags.HideInHierarchy;
        AttachResourceAnimationClips(prototype, resourcePath, GiantResourceFolderPath);
        ConfigureImportedPrototype(prototype, UnitKind.Giant);
        prototype.SetActive(false);
        return prototype;
    }

    private void ConfigureTankVariantPrototypes()
    {
        tankVariantPrototypes.Clear();

        GameObject standardPrototype;
        if (modelPrototypes.TryGetValue(UnitKind.Tank, out standardPrototype))
        {
            AddTankVariantPrototype(standardPrototype);
        }

        AddTankVariantPrototype(LoadTankResourcePrototype(TankScoutResourceModelPath));
        AddTankVariantPrototype(LoadTankResourcePrototype(TankAssaultResourceModelPath));
        AddTankVariantPrototype(tankT55AkPrototype);
    }

    private void ConfigureGiantVariantPrototypes()
    {
        giantVariantPrototypes.Clear();

        GameObject standardPrototype;
        if (modelPrototypes.TryGetValue(UnitKind.Giant, out standardPrototype))
        {
            AddGiantVariantPrototype(standardPrototype);
        }

        for (int i = 0; i < GiantResourceVariantModelPaths.Length; i++)
        {
            AddGiantVariantPrototype(LoadGiantResourcePrototype(GiantResourceVariantModelPaths[i]));
        }
    }

    private void AddTankVariantPrototype(GameObject prototype)
    {
        if (prototype != null && !tankVariantPrototypes.Contains(prototype))
        {
            tankVariantPrototypes.Add(prototype);
        }
    }

    private void AddGiantVariantPrototype(GameObject prototype)
    {
        if (prototype != null && !giantVariantPrototypes.Contains(prototype))
        {
            giantVariantPrototypes.Add(prototype);
        }
    }

    private static void AttachResourceAnimationClips(GameObject prototype, string resourceModelPath, string resourceFolderPath)
    {
        var clips = Resources.LoadAll<AnimationClip>(resourceModelPath);
        if (clips == null || clips.Length == 0)
        {
            clips = Resources.LoadAll<AnimationClip>(resourceFolderPath);
        }

        if (clips == null || clips.Length == 0)
        {
            return;
        }

        var clipStore = prototype.GetComponent<RuntimeAnimationClipStore>();
        if (clipStore == null)
        {
            clipStore = prototype.AddComponent<RuntimeAnimationClipStore>();
        }

        clipStore.Clips = clips;
        clipStore.AnimatorClips = clips;
        clipStore.AnimatorReady = true;
    }

    private static void AttachRuntimeAnimationClips(GameObject prototype, GLTFComponent gltf)
    {
        if (prototype == null || gltf == null || gltf.CreatedAnimationClips == null || gltf.CreatedAnimationClips.Length == 0)
        {
            return;
        }

        var clipStore = prototype.GetComponent<RuntimeAnimationClipStore>();
        if (clipStore == null)
        {
            clipStore = prototype.AddComponent<RuntimeAnimationClipStore>();
        }

        clipStore.Clips = gltf.CreatedAnimationClips;
        clipStore.AnimatorClips = Array.Empty<AnimationClip>();
        clipStore.AnimatorReady = false;
    }

    private static AnimationClip[] CreateAnimatorCompatibleClips(AnimationClip[] sourceClips)
    {
        if (sourceClips == null || sourceClips.Length == 0)
        {
            return Array.Empty<AnimationClip>();
        }

        var clips = new List<AnimationClip>(sourceClips.Length);
        for (int i = 0; i < sourceClips.Length; i++)
        {
            AddUniqueAnimatorCompatibleClip(clips, sourceClips[i]);
        }

        return clips.ToArray();
    }

    private static AnimationClip CreateAnimatorCompatibleClip(AnimationClip source)
    {
        if (source == null || !source.legacy)
        {
            return source;
        }

        var clone = Instantiate(source);
        clone.name = source.name;
        clone.legacy = false;
        clone.wrapMode = source.wrapMode == WrapMode.Default ? WrapMode.Loop : source.wrapMode;
        return clone;
    }

    private void ConfigureImportedPrototype(GameObject prototype, UnitKind kind)
    {
        var renderers = prototype.GetComponentsInChildren<Renderer>(true);
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

            var skinned = renderer as SkinnedMeshRenderer;
            if (skinned != null)
            {
                skinned.updateWhenOffscreen = true;
            }
        }

        if (kind == UnitKind.Tank)
        {
            RemoveTankDisplayGeometry(prototype);
        }

        NormalizePrototype(prototype, Poses[kind].TargetHeight);
    }

    private void RemoveTankDisplayGeometry(GameObject prototype)
    {
        var renderers = prototype.GetComponentsInChildren<Renderer>(true);
        int removed = 0;
        for (int i = renderers.Length - 1; i >= 0; i--)
        {
            var renderer = renderers[i];
            if (!IsTankDisplayRenderer(renderer))
            {
                continue;
            }

            DestroyImmediate(renderer.gameObject);
            removed++;
        }

        if (removed > 0)
        {
            Debug.Log($"Removed {removed} display renderers from imported tank model.");
        }
    }

    private static bool IsTankDisplayRenderer(Renderer renderer)
    {
        return UsesOnlyDisplayMaterials(renderer) || HasDisplayGeometryName(renderer);
    }

    private static bool HasDisplayGeometryName(Renderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        string name = renderer.gameObject.name;
        return ContainsTankDisplayToken(name) || ContainsTankDisplayToken(renderer.name);
    }

    private static bool UsesOnlyDisplayMaterials(Renderer renderer)
    {
        var materials = renderer.sharedMaterials;
        if (materials.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            if (!IsTankDisplayMaterial(materials[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsTankDisplayMaterial(Material material)
    {
        if (material == null)
        {
            return false;
        }

        string name = material.name;
        int instanceSuffix = name.IndexOf(" (", StringComparison.Ordinal);
        if (instanceSuffix >= 0)
        {
            name = name.Substring(0, instanceSuffix);
        }

        return ContainsTankDisplayToken(name);
    }

    private static bool ContainsTankDisplayToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim();
        if (TankDisplayMaterialNames.Contains(normalized))
        {
            return true;
        }

        foreach (string token in TankDisplayMaterialNames)
        {
            if (normalized.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private void NormalizePrototype(GameObject prototype, float targetHeight)
    {
        var renderers = prototype.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float currentHeight = Mathf.Max(0.001f, bounds.size.y);
        float uniformScale = targetHeight / currentHeight;
        prototype.transform.localScale = prototype.transform.localScale * uniformScale;

        renderers = prototype.GetComponentsInChildren<Renderer>(true);
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float lift = -bounds.min.y;
        prototype.transform.localPosition += new Vector3(0f, lift, 0f);
    }

    private GameObject CreateFallbackPrototype(UnitKind kind)
    {
        var root = new GameObject($"{kind}_Fallback");
        root.transform.SetParent(modelCacheRoot, false);
        var pose = Poses[kind];

        switch (kind)
        {
            case UnitKind.Soldier:
            {
                var body = CreatePrimitive(PrimitiveType.Capsule, "Body", root.transform);
                body.transform.localScale = new Vector3(0.36f, 0.72f, 0.36f);
                body.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.38f, 0.88f, 0.4f, 1f));
                break;
            }
            case UnitKind.Tank:
            {
                var hull = CreatePrimitive(PrimitiveType.Cube, "Hull", root.transform);
                hull.transform.localScale = new Vector3(1.15f, 0.52f, 0.9f);
                hull.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.34f, 0.46f, 0.29f, 1f));

                var turret = CreatePrimitive(PrimitiveType.Cylinder, "Turret", root.transform);
                turret.transform.localScale = new Vector3(0.34f, 0.16f, 0.34f);
                turret.transform.localPosition = new Vector3(0f, 0.42f, 0f);
                turret.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.26f, 0.34f, 0.19f, 1f));
                break;
            }
            case UnitKind.Aircraft:
            {
                var fuselage = CreatePrimitive(PrimitiveType.Cylinder, "Fuselage", root.transform);
                fuselage.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                fuselage.transform.localScale = new Vector3(0.22f, 1.25f, 0.22f);
                fuselage.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.72f, 0.84f, 0.95f, 1f));

                var rotor = CreatePrimitive(PrimitiveType.Cube, "Rotor", root.transform);
                rotor.transform.localScale = new Vector3(1.2f, 0.04f, 0.12f);
                rotor.transform.localPosition = new Vector3(0f, 0.34f, 0f);
                rotor.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.17f, 0.22f, 0.26f, 1f));
                break;
            }
            case UnitKind.Giant:
            {
                var torso = CreatePrimitive(PrimitiveType.Capsule, "Torso", root.transform);
                torso.transform.localScale = new Vector3(0.95f, 1.8f, 0.95f);
                torso.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.28f, 0.84f, 0.36f, 1f));
                break;
            }
            case UnitKind.Fireball:
            {
                var orb = CreatePrimitive(PrimitiveType.Sphere, "Orb", root.transform);
                orb.transform.localScale = Vector3.one * 0.72f;
                orb.GetComponent<Renderer>().sharedMaterial = GetTransparentMaterial(new Color(1f, 0.47f, 0.12f, 0.85f));
                break;
            }
            case UnitKind.Smoke:
            {
                var orb = CreatePrimitive(PrimitiveType.Sphere, "Orb", root.transform);
                orb.transform.localScale = Vector3.one * 0.78f;
                orb.GetComponent<Renderer>().sharedMaterial = GetTransparentMaterial(new Color(0.66f, 0.73f, 0.77f, 0.65f));
                break;
            }
        }

        NormalizePrototype(root, pose.TargetHeight);
        root.SetActive(false);
        return root;
    }

    private void AttachPrototypesToUnits()
    {
        for (int i = 0; i < soldiers.Count; i++)
        {
            AttachUnitModel(soldiers[i]);
        }

        for (int i = 0; i < tanks.Count; i++)
        {
            AttachUnitModel(tanks[i]);
        }

        for (int i = 0; i < aircraft.Count; i++)
        {
            AttachUnitModel(aircraft[i]);
        }

        for (int i = 0; i < giants.Count; i++)
        {
            AttachUnitModel(giants[i]);
        }
    }

    private void AttachFallbackPrototypes()
    {
        if (modelPrototypes.Count == 0)
        {
            DiagnosticsUsingFallback = true;
            modelPrototypes[UnitKind.Soldier] = CreateFallbackPrototype(UnitKind.Soldier);
            modelPrototypes[UnitKind.Tank] = CreateFallbackPrototype(UnitKind.Tank);
            modelPrototypes[UnitKind.Aircraft] = CreateFallbackPrototype(UnitKind.Aircraft);
            modelPrototypes[UnitKind.Giant] = CreateFallbackPrototype(UnitKind.Giant);
            modelPrototypes[UnitKind.Fireball] = CreateFallbackPrototype(UnitKind.Fireball);
            modelPrototypes[UnitKind.Smoke] = CreateFallbackPrototype(UnitKind.Smoke);
        }

        AttachPrototypesToUnits();
    }

    private void AttachUnitModel(BattleUnit unit)
    {
        bool rootWasActive = unit.root.activeSelf;
        if (!rootWasActive)
        {
            unit.root.SetActive(true);
        }

        try
        {
            if (unit.modelInstance != null)
            {
                DisposeUnitAnimator(unit);
                Destroy(unit.modelInstance);
                unit.modelInstance = null;
            }

            if (unit.motionAccessoryRoot != null)
            {
                Destroy(unit.motionAccessoryRoot);
                unit.motionAccessoryRoot = null;
            }

            unit.aircraftRotorRoot = null;
            unit.tankMotionRig = null;

            GameObject prototype = ResolvePrototypeForUnit(unit);
            if (prototype == null)
            {
                return;
            }

            bool usingFallbackPrototype = prototype.name.IndexOf("Fallback", StringComparison.OrdinalIgnoreCase) >= 0;
            var model = Instantiate(prototype, unit.body, false);
            model.name = unit.kind.ToString();
            model.SetActive(true);
            ConfigureRuntimeModel(model, unit.kind);
            if (unit.kind == UnitKind.Tank && unit.tankAimRoot != null)
            {
                unit.tankAimRoot.gameObject.SetActive(usingFallbackPrototype);
            }

            unit.modelInstance = model;
            unit.animations = model.GetComponentsInChildren<Animation>(true);
            unit.baseModelScale = model.transform.localScale;
            unit.baseModelLocalPosition = model.transform.localPosition;
            unit.currentAnimation = string.Empty;
            ConfigureAnimatorPlayback(unit, model);
            ConfigureProceduralMotionRig(unit);
            PlayUnitAnimation(unit, true);
        }
        finally
        {
            if (!rootWasActive)
            {
                unit.root.SetActive(false);
            }
        }
    }

    private void ConfigureAnimatorPlayback(BattleUnit unit, GameObject model)
    {
        unit.animator = null;
        unit.animatorClips = null;
        unit.currentAnimatorClip = string.Empty;

        if ((unit.kind != UnitKind.Soldier && unit.kind != UnitKind.Tank && unit.kind != UnitKind.Giant) || model == null)
        {
            return;
        }

        AnimationClip[] clips = CollectRuntimeAnimationClips(model);
        if (clips.Length == 0)
        {
            return;
        }

        for (int i = 0; i < unit.animations.Length; i++)
        {
            if (unit.animations[i] != null)
            {
                unit.animations[i].enabled = false;
            }
        }

        var animator = model.GetComponent<Animator>();
        if (animator == null)
        {
            animator = model.AddComponent<Animator>();
        }

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        unit.animator = animator;
        unit.animatorClips = clips;
    }

    private static AnimationClip[] CollectRuntimeAnimationClips(GameObject model)
    {
        var clips = new List<AnimationClip>();
        var stores = model.GetComponentsInChildren<RuntimeAnimationClipStore>(true);
        for (int i = 0; i < stores.Length; i++)
        {
            if (!stores[i].AnimatorReady)
            {
                continue;
            }

            var storeClips = stores[i].AnimatorClips;
            if (storeClips == null || storeClips.Length == 0)
            {
                storeClips = stores[i].Clips;
            }

            if (storeClips == null)
            {
                continue;
            }

            for (int c = 0; c < storeClips.Length; c++)
            {
                AddUniqueAnimatorCompatibleClip(clips, storeClips[c]);
            }
        }

        return clips.ToArray();
    }

    private static void AddUniqueAnimatorCompatibleClip(List<AnimationClip> clips, AnimationClip clip)
    {
        if (clip == null || ContainsClip(clips, clip))
        {
            return;
        }

        clips.Add(CreateAnimatorCompatibleClip(clip));
    }

    private static void AddUniqueClip(List<AnimationClip> clips, AnimationClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (ContainsClip(clips, clip))
        {
            return;
        }

        clips.Add(clip);
    }

    private static bool ContainsClip(List<AnimationClip> clips, AnimationClip clip)
    {
        if (clips == null || clip == null)
        {
            return false;
        }

        for (int i = 0; i < clips.Count; i++)
        {
            if (clips[i] == clip || string.Equals(clips[i].name, clip.name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void ConfigureProceduralMotionRig(BattleUnit unit)
    {
        if (unit == null || unit.body == null)
        {
            return;
        }

        if (unit.kind == UnitKind.Aircraft)
        {
            ConfigureAircraftRotorRig(unit);
            return;
        }

        if (unit.kind == UnitKind.Tank)
        {
            ConfigureTankMotionRig(unit, UsesAnimatorPlayback(unit));
        }
    }

    private void ConfigureAircraftRotorRig(BattleUnit unit)
    {
        var root = new GameObject("AircraftRotorMotionRig");
        root.transform.SetParent(unit.body, false);
        root.transform.localPosition = new Vector3(0f, 1.18f, 0f);

        var hub = CreatePrimitive(PrimitiveType.Cylinder, "RotorHub", root.transform);
        hub.transform.localScale = new Vector3(0.09f, 0.05f, 0.09f);
        hub.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.13f, 0.16f, 0.17f, 1f));

        var bladeA = CreatePrimitive(PrimitiveType.Cube, "RotorBladeA", root.transform);
        bladeA.transform.localScale = new Vector3(1.55f, 0.018f, 0.11f);
        bladeA.GetComponent<Renderer>().sharedMaterial = GetTransparentMaterial(new Color(0.68f, 0.82f, 0.92f, 0.38f));

        var bladeB = CreatePrimitive(PrimitiveType.Cube, "RotorBladeB", root.transform);
        bladeB.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        bladeB.transform.localScale = new Vector3(1.55f, 0.018f, 0.11f);
        bladeB.GetComponent<Renderer>().sharedMaterial = GetTransparentMaterial(new Color(0.68f, 0.82f, 0.92f, 0.30f));

        unit.motionAccessoryRoot = root;
        unit.aircraftRotorRoot = root.transform;
    }

    private void ConfigureTankMotionRig(BattleUnit unit, bool animatorDrivenTracks)
    {
        var rig = new TankMotionRig();
        CollectTankAimParts(unit, rig);

        if (!animatorDrivenTracks)
        {
            CollectTankMotionParts(unit, rig);

            if (rig.wheelTransforms.Count < 4 || rig.trackMaterials.Count == 0)
            {
                AddTankHelperTracks(unit, rig);
            }
        }

        unit.tankMotionRig = rig;
    }

    private void CollectTankAimParts(BattleUnit unit, TankMotionRig rig)
    {
        if (unit.modelInstance == null || rig == null)
        {
            return;
        }

        var transforms = unit.modelInstance.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            var part = transforms[i];
            if (part == null || part == unit.modelInstance.transform || !IsTankAimTransform(part))
            {
                continue;
            }

            if (!rig.aimTransforms.Contains(part))
            {
                rig.aimTransforms.Add(part);
                rig.aimBaseRotations.Add(part.localRotation);
            }
        }
    }

    private void CollectTankMotionParts(BattleUnit unit, TankMotionRig rig)
    {
        if (unit.modelInstance == null || rig == null)
        {
            return;
        }

        var transforms = unit.modelInstance.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            var part = transforms[i];
            if (part == null || part == unit.modelInstance.transform || !IsTankWheelTransform(part))
            {
                continue;
            }

            if (!rig.wheelTransforms.Contains(part))
            {
                rig.wheelTransforms.Add(part);
                rig.wheelBaseRotations.Add(part.localRotation);
            }
        }

        var renderers = unit.modelInstance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            bool rendererLooksLikeTrack = IsTankTrackName(renderer.name) || IsTankTrackName(renderer.gameObject.name);
            var materials = renderer.materials;
            for (int m = 0; m < materials.Length; m++)
            {
                var material = materials[m];
                if (material == null)
                {
                    continue;
                }

                if (rendererLooksLikeTrack || IsTankTrackName(material.name))
                {
                    if (!rig.trackMaterials.Contains(material))
                    {
                        rig.trackMaterials.Add(material);
                    }
                }
            }
        }
    }

    private void AddTankHelperTracks(BattleUnit unit, TankMotionRig rig)
    {
        var root = new GameObject("TankTrackMotionRig");
        root.transform.SetParent(unit.body, false);
        root.transform.localPosition = new Vector3(0f, 0.10f, 0f);

        Material beltMaterial = GetOpaqueMaterial(new Color(0.055f, 0.06f, 0.055f, 1f));
        Material wheelMaterial = GetOpaqueMaterial(new Color(0.17f, 0.18f, 0.16f, 1f));
        float[] wheelXs = { -0.62f, -0.34f, -0.06f, 0.22f, 0.50f, 0.78f };

        for (int side = -1; side <= 1; side += 2)
        {
            float z = side * 0.41f;
            var belt = CreatePrimitive(PrimitiveType.Cube, side < 0 ? "LeftTrackBelt" : "RightTrackBelt", root.transform);
            belt.transform.localPosition = new Vector3(0.08f, 0.08f, z);
            belt.transform.localScale = new Vector3(1.62f, 0.055f, 0.16f);
            belt.GetComponent<Renderer>().sharedMaterial = beltMaterial;

            for (int i = 0; i < wheelXs.Length; i++)
            {
                var wheel = CreatePrimitive(PrimitiveType.Cylinder, side < 0 ? $"LeftRoadWheel_{i}" : $"RightRoadWheel_{i}", root.transform);
                wheel.transform.localPosition = new Vector3(wheelXs[i], 0.12f, z);
                wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                wheel.transform.localScale = new Vector3(0.13f, 0.038f, 0.13f);
                wheel.GetComponent<Renderer>().sharedMaterial = wheelMaterial;
                rig.wheelTransforms.Add(wheel.transform);
                rig.wheelBaseRotations.Add(wheel.transform.localRotation);
            }
        }

        rig.helperRoot = root.transform;
        unit.motionAccessoryRoot = root;
    }

    private static bool IsTankWheelTransform(Transform part)
    {
        if (part == null)
        {
            return false;
        }

        string name = part.name;
        return ContainsNameToken(name, "wheel")
            || ContainsNameToken(name, "tire")
            || ContainsNameToken(name, "tyre")
            || ContainsNameToken(name, "sprocket")
            || ContainsNameToken(name, "idler");
    }

    private static bool IsTankAimTransform(Transform part)
    {
        if (part == null)
        {
            return false;
        }

        string name = part.name;
        return ContainsNameToken(name, "turret")
            || ContainsNameToken(name, "barrel")
            || string.Equals(name, "Tank_Gun", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Gun", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTankTrackName(string name)
    {
        return ContainsNameToken(name, "track")
            || ContainsNameToken(name, "tread")
            || ContainsNameToken(name, "crawler");
    }

    private static bool ContainsNameToken(string value, string token)
    {
        return !string.IsNullOrEmpty(value)
            && value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private GameObject ResolvePrototypeForUnit(BattleUnit unit)
    {
        if (unit.kind == UnitKind.Tank && tankVariantPrototypes.Count > 0)
        {
            int variantIndex = Mathf.Abs(unit.rank) % tankVariantPrototypes.Count;
            return tankVariantPrototypes[variantIndex];
        }

        if (unit.kind == UnitKind.Giant && giantVariantPrototypes.Count > 0)
        {
            int variantIndex = Mathf.Abs(unit.id + unit.rank) % giantVariantPrototypes.Count;
            return giantVariantPrototypes[variantIndex];
        }

        if (unit.kind == UnitKind.Tank && unit.tankModel == TankModelVariant.T55AK && tankT55AkPrototype != null)
        {
            return tankT55AkPrototype;
        }

        GameObject prototype;
        return modelPrototypes.TryGetValue(unit.kind, out prototype) ? prototype : null;
    }

    private void ConfigureRuntimeModel(GameObject model, UnitKind kind)
    {
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            var skinned = renderer as SkinnedMeshRenderer;
            if (skinned != null)
            {
                skinned.updateWhenOffscreen = true;
            }
        }

        var pose = Poses[kind];
        FitModelToHeight(model, pose.TargetHeight);
    }

    private void FitModelToHeight(GameObject model, float targetHeight)
    {
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float currentHeight = Mathf.Max(0.001f, bounds.size.y);
        float uniformScale = targetHeight / currentHeight;
        model.transform.localScale = model.transform.localScale * uniformScale;

        renderers = model.GetComponentsInChildren<Renderer>(true);
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        model.transform.localPosition += new Vector3(0f, -bounds.min.y, 0f);
    }

    private void ResetBattle()
    {
        if (!assetsReady)
        {
            return;
        }

        paused = false;
        ended = false;
        battleTime = 0f;
        humanLosses = 0;
        loadingPulseTime = 0f;

        for (int i = 0; i < projectiles.Count; i++)
        {
            if (projectiles[i].root != null)
            {
                projectiles[i].root.SetActive(false);
            }
            projectiles[i].active = false;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i].root != null)
            {
                effects[i].root.SetActive(false);
            }
            effects[i].active = false;
        }

        for (int i = 0; i < deathVisuals.Count; i++)
        {
            if (deathVisuals[i].root != null)
            {
                deathVisuals[i].root.SetActive(false);
            }
            deathVisuals[i].active = false;
        }

        ResetUnitState(soldiers, SoldierLanes, 1);
        ResetTanks();
        ResetAircraft();
        ResetGiants();
        RefreshHud();
        ShowBanner("Battle start", false, 1.8f);
    }

    private void ResetUnitState(List<BattleUnit> units, float[] lanes, int facing)
    {
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            int lane = i % lanes.Length;
            int rank = i / lanes.Length;
            float x = Left + 190f + rank * 38f + Noise(i + 3f) * 5f;
            float z = lanes[lane] + (Noise(i + 19f) - 0.5f) * 8f;
            ActivateUnit(unit, x, z, soldierConfig.MaxHp, soldierConfig.Damage, soldierConfig.MoveSpeed + Noise(i + 73f) * 18f, soldierConfig.Radius, soldierConfig.AttackRange + Noise(i + 101f) * 34f, soldierConfig.AttackInterval + Noise(i + 131f) * 0.22f, rank, facing, 0f);
        }
    }

    private void ResetTanks()
    {
        for (int i = 0; i < tanks.Count; i++)
        {
            int lane = i % TankLanes.Length;
            int rank = i / TankLanes.Length;
            float x = Left + 40f + rank * 94f;
            float z = TankLanes[lane] + rank * 10f;
            ActivateUnit(tanks[i], x, z, tankConfig.MaxHp, tankConfig.Damage, tankConfig.MoveSpeed + Noise(i + 401f) * 8f, tankConfig.Radius, tankConfig.AttackRange, tankConfig.AttackInterval + Noise(i + 503f) * 0.3f, i, 1, 0f);
        }
    }

    private void ResetAircraft()
    {
        for (int i = 0; i < aircraft.Count; i++)
        {
            float x = Left + 52f + i * 126f;
            float z = AirLanes[i % AirLanes.Length];
            ActivateUnit(aircraft[i], x, z, aircraftConfig.MaxHp, aircraftConfig.Damage, aircraftConfig.MoveSpeed + i * 9f, aircraftConfig.Radius, aircraftConfig.AttackRange, aircraftConfig.AttackInterval + i * 0.12f, i, 1, 2.5f);
        }
    }

    private void ResetGiants()
    {
        for (int i = 0; i < giants.Count; i++)
        {
            int lane = i % 5;
            int rank = i / 5;
            float x = Right + 72f + rank * 120f;
            float z = -460f + lane * 230f + rank * 24f;
            ActivateUnit(giants[i], x, z, giantConfig.MaxHp, giantConfig.Damage, giantConfig.MoveSpeed + Noise(i + 207f) * 4f, giantConfig.Radius, giantConfig.AttackRange, giantConfig.AttackInterval + Noise(i + 307f) * 0.18f, i, -1, 0f);
            giants[i].attackCooldown = 2.2f + Noise(i + 907f) * 1.4f;
        }
    }

    private void ActivateUnit(BattleUnit unit, float x, float z, float hp, float damage, float speed, float radius, float range, float interval, int rank, int facing, float altitude)
    {
        unit.active = true;
        unit.root.SetActive(true);
        unit.x = x;
        unit.z = z;
        unit.baseZ = z;
        unit.hp = hp;
        unit.maxHp = hp;
        unit.damage = damage;
        unit.speed = speed;
        unit.radius = radius;
        unit.attackRange = range;
        unit.attackInterval = interval;
        unit.attackCooldown = Noise(unit.id + rank * 19.3f) * interval;
        unit.attackVisualTimer = 0f;
        unit.hitFlashTimer = 0f;
        unit.rank = rank;
        unit.facing = facing;
        unit.animTimer = Noise(unit.id * 11.3f) * 2f;
        unit.altitude = altitude;
        unit.headingDegrees = facing < 0 ? -90f : 90f;
        unit.turretYawDegrees = unit.headingDegrees;
        unit.moveSpeed = 0f;
        unit.rotorSpinDegrees = Noise(unit.id + rank * 7.1f) * 360f;
        unit.wheelSpinDegrees = Noise(unit.id + rank * 5.3f) * 360f;
        unit.trackScroll = 0f;
        UpdateUnitTransform(unit, 0f);
        PlayUnitAnimation(unit, false);
    }

    private bool ReviveSoldierFromDanmu(DanmuCommand command)
    {
        var unit = FindInactiveUnit(soldiers);
        if (unit == null)
        {
            return false;
        }

        int lane = processedDanmuCommandCount % SoldierLanes.Length;
        int rank = Mathf.Max(0, CountActive(soldiers) / SoldierLanes.Length);
        float x = Left + 126f + Noise(processedDanmuCommandCount + 17f) * 36f;
        float z = SoldierLanes[lane] + (Noise(processedDanmuCommandCount + 29f) - 0.5f) * 12f;
        ActivateUnit(unit, x, z, soldierConfig.MaxHp + 4f, soldierConfig.Damage + 1f, soldierConfig.MoveSpeed + 8f, soldierConfig.Radius, soldierConfig.AttackRange + 26f, soldierConfig.AttackInterval - 0.08f, rank, 1, 0f);
        PlayDanmuSpawnEffect(BattleEffectId.HumanSummon, x, z, 1.2f);
        return true;
    }

    private bool ReviveTankFromDanmu(DanmuCommand command)
    {
        var unit = FindInactiveUnit(tanks);
        if (unit == null)
        {
            return false;
        }

        int lane = processedDanmuCommandCount % TankLanes.Length;
        float x = Left + 54f + Noise(processedDanmuCommandCount + 41f) * 42f;
        float z = TankLanes[lane] + (Noise(processedDanmuCommandCount + 43f) - 0.5f) * 18f;
        ActivateUnit(unit, x, z, tankConfig.MaxHp + 40f, tankConfig.Damage + 7f, tankConfig.MoveSpeed + 2f, tankConfig.Radius, tankConfig.AttackRange + 20f, tankConfig.AttackInterval - 0.1f, processedDanmuCommandCount, 1, 0f);
        PlayDanmuSpawnEffect(BattleEffectId.HumanSummon, x, z, 1.6f);
        return true;
    }

    private bool ReviveGiantFromDanmu(DanmuCommand command)
    {
        var unit = FindInactiveUnit(giants);
        if (unit == null)
        {
            return false;
        }

        int lane = processedDanmuCommandCount % 5;
        float x = Right + 88f + Noise(processedDanmuCommandCount + 71f) * 36f;
        float z = -460f + lane * 230f + (Noise(processedDanmuCommandCount + 83f) - 0.5f) * 34f;
        ActivateUnit(unit, x, z, giantConfig.MaxHp, giantConfig.Damage, giantConfig.MoveSpeed + 3f, giantConfig.Radius, giantConfig.AttackRange, giantConfig.AttackInterval, processedDanmuCommandCount, -1, 0f);
        unit.attackCooldown = 0.3f;
        PlayDanmuSpawnEffect(BattleEffectId.OrcSummon, x, z, 2.0f);
        return true;
    }

    private BattleUnit FindInactiveUnit(List<BattleUnit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (!units[i].active)
            {
                return units[i];
            }
        }

        return null;
    }

    private bool HealHumanForces(float amount)
    {
        bool changed = false;
        changed |= HealUnitGroup(soldiers, amount);
        changed |= HealUnitGroup(tanks, amount * 2.4f);
        changed |= HealUnitGroup(aircraft, amount * 1.8f);
        return changed;
    }

    private bool HealUnitGroup(List<BattleUnit> units, float amount)
    {
        bool changed = false;
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (!unit.active)
            {
                continue;
            }

            float before = unit.hp;
            unit.hp = Mathf.Min(unit.maxHp, unit.hp + amount);
            changed |= unit.hp > before;
            unit.attackVisualTimer = Mathf.Max(unit.attackVisualTimer, 0.12f);
        }

        return changed;
    }

    private void HealGiants(float amount)
    {
        for (int i = 0; i < giants.Count; i++)
        {
            var unit = giants[i];
            if (!unit.active)
            {
                continue;
            }

            unit.hp = Mathf.Min(unit.maxHp, unit.hp + amount);
            unit.hitFlashTimer = Mathf.Max(unit.hitFlashTimer, 0.08f);
        }
    }

    private void HastenGiants(float cooldown)
    {
        for (int i = 0; i < giants.Count; i++)
        {
            var unit = giants[i];
            if (!unit.active)
            {
                continue;
            }

            unit.attackCooldown = Mathf.Min(unit.attackCooldown, cooldown);
            unit.attackVisualTimer = Mathf.Max(unit.attackVisualTimer, 0.36f);
        }
    }

    private void ReduceHumanCooldowns(float cooldown)
    {
        ReduceUnitCooldowns(soldiers, cooldown);
        ReduceUnitCooldowns(tanks, cooldown);
        ReduceUnitCooldowns(aircraft, cooldown);
    }

    private void ReduceUnitCooldowns(List<BattleUnit> units, float cooldown)
    {
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (!unit.active)
            {
                continue;
            }

            unit.attackCooldown = Mathf.Min(unit.attackCooldown, cooldown);
            unit.attackVisualTimer = Mathf.Max(unit.attackVisualTimer, 0.18f);
        }
    }

    private void PlayDanmuSpawnEffect(BattleEffectId effectId, float x, float z, float scale)
    {
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.Play(EffectPlayback.Create(effectId, ToWorldPoint(x, z, 0.12f), Quaternion.identity, null, scale));
        }

        SpawnEffect(x, z + 18f, scale, EffectKind.Smoke, 0.32f);
    }

    private void UpdateHumans(float dt)
    {
        for (int i = 0; i < soldiers.Count; i++)
        {
            UpdateHumanUnit(soldiers[i], dt);
        }

        for (int i = 0; i < tanks.Count; i++)
        {
            UpdateHumanUnit(tanks[i], dt);
        }

        for (int i = 0; i < aircraft.Count; i++)
        {
            UpdateHumanUnit(aircraft[i], dt);
        }
    }

    private void UpdateHumanUnit(BattleUnit unit, float dt)
    {
        var target = FindNearestGiant(unit);
        if (!unit.active || target == null)
        {
            return;
        }

        unit.animTimer += dt;
        unit.attackCooldown = Mathf.Max(0f, unit.attackCooldown - dt);
        unit.attackVisualTimer = Mathf.Max(0f, unit.attackVisualTimer - dt);

        float previousX = unit.x;
        float previousZ = unit.z;
        if (unit.kind == UnitKind.Tank)
        {
            UpdateTankAiming(unit, target, dt);
        }

        float dx = target.x - unit.x;
        float dz = target.z - unit.z;
        float distance = Mathf.Sqrt(dx * dx + dz * dz);
        bool canFire = distance <= unit.attackRange + target.radius * 0.55f;

        if (canFire && unit.attackCooldown <= 0f)
        {
            FireHumanWeapon(unit, target);
        }

        float desiredX = HumanHoldX(unit, target);
        float nextX = unit.x;
        if (unit.x < desiredX)
        {
            nextX = Mathf.Min(desiredX, unit.x + unit.speed * dt);
        }

        float desiredZ = HumanHoldZ(unit);
        float nextZ = unit.z + (desiredZ - unit.z) * dt * 0.45f;

        if (unit.kind == UnitKind.Aircraft)
        {
            nextZ = desiredZ + Mathf.Sin(battleTime * 2.1f + unit.seed * 9f) * 13f;
        }

        MoveUnitToAvoidingBuildings(unit, nextX, nextZ);
        unit.x = Mathf.Clamp(unit.x, Left - 190f, Right - 48f);
        if (unit.kind == UnitKind.Tank)
        {
            float moveX = unit.x - previousX;
            float moveZ = unit.z - previousZ;
            if (Mathf.Abs(moveX) + Mathf.Abs(moveZ) > 0.01f)
            {
                unit.headingDegrees = DirectionYawDegrees(moveX, moveZ, unit.headingDegrees);
            }
        }

        RecordUnitMovement(unit, previousX, previousZ, dt);
        UpdateUnitTransform(unit, dt);
    }

    private float HumanHoldX(BattleUnit unit, BattleUnit target)
    {
        float gap = HumanEngagementGap(unit.kind);
        return Mathf.Clamp(target.x - gap, Left + 58f, Right - 48f);
    }

    private float HumanEngagementGap(UnitKind kind)
    {
        switch (kind)
        {
            case UnitKind.Aircraft:
                return GiantMeleeOffset(kind);
            case UnitKind.Tank:
                return GiantMeleeOffset(kind);
            default:
                return GiantMeleeOffset(kind);
        }
    }

    private float HumanHoldZ(BattleUnit unit)
    {
        if (unit.kind == UnitKind.Aircraft)
        {
            return unit.baseZ;
        }

        return unit.baseZ + Mathf.Sin(battleTime * 1.7f + unit.seed * 6f) * 5f;
    }

    private BattleUnit FindNearestGiant(BattleUnit origin)
    {
        if (origin == null)
        {
            return null;
        }

        BattleUnit best = null;
        float bestScore = float.PositiveInfinity;
        for (int i = 0; i < giants.Count; i++)
        {
            var candidate = giants[i];
            if (candidate == null || !candidate.active)
            {
                continue;
            }

            float score = DistanceSq(origin.x, origin.z, candidate.x, candidate.z);
            if (score < bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    private void UpdateTankAiming(BattleUnit unit, BattleUnit target, float dt)
    {
        if (unit == null || target == null)
        {
            return;
        }

        float aimYaw = DirectionYawDegrees(target.x - unit.x, target.z - unit.z, unit.turretYawDegrees);
        unit.turretYawDegrees = Mathf.LerpAngle(unit.turretYawDegrees, aimYaw, Mathf.Clamp01(dt * 7.2f));
    }

    private void ResolveUnitOverlaps()
    {
        bool changed = false;
        for (int pass = 0; pass < 4; pass++)
        {
            changed |= ResolveWithinGroup(giants, 1.05f);
            changed |= ResolveWithinGroup(tanks, 1.18f);
            changed |= ResolveBetweenGroups(tanks, soldiers, 1.10f);
            changed |= ResolveAwayFromGiant(tanks, 1.12f);
            changed |= ResolveAwayFromGiant(soldiers, 1.02f);
            changed |= ResolveAwayFromBuildings(soldiers);
            changed |= ResolveAwayFromBuildings(tanks);
            changed |= ResolveAwayFromBuildings(aircraft);
            changed |= ResolveAwayFromBuildings(giants);
        }

        if (!changed)
        {
            return;
        }

        UpdateActiveTransforms(tanks);
        UpdateActiveTransforms(soldiers);
        UpdateActiveTransforms(aircraft);
        UpdateActiveTransforms(giants);
    }

    private bool ResolveWithinGroup(List<BattleUnit> units, float padding)
    {
        bool changed = false;
        for (int i = 0; i < units.Count; i++)
        {
            for (int j = i + 1; j < units.Count; j++)
            {
                changed |= ResolvePair(units[i], units[j], padding);
            }
        }

        return changed;
    }

    private bool ResolveBetweenGroups(List<BattleUnit> first, List<BattleUnit> second, float padding)
    {
        bool changed = false;
        for (int i = 0; i < first.Count; i++)
        {
            for (int j = 0; j < second.Count; j++)
            {
                changed |= ResolvePair(first[i], second[j], padding);
            }
        }

        return changed;
    }

    private bool ResolveAwayFromGiant(List<BattleUnit> units, float padding)
    {
        bool changed = false;
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit == null || !unit.active || unit.kind == UnitKind.Aircraft)
            {
                continue;
            }

            for (int g = 0; g < giants.Count; g++)
            {
                var giant = giants[g];
                if (giant == null || !giant.active)
                {
                    continue;
                }

                float zReach = GiantMeleeZReach(unit.kind, false) * padding;
                if (Mathf.Abs(unit.z - giant.z) > zReach)
                {
                    continue;
                }

                float stopX = HumanHoldX(unit, giant);
                float guard = 4f + padding * 2f;
                if (unit.x > stopX + guard)
                {
                    unit.x = stopX + guard;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private bool ResolveAwayFromBuildings(List<BattleUnit> units)
    {
        bool changed = false;
        for (int i = 0; i < units.Count; i++)
        {
            changed |= ResolveBuildingCollision(units[i]);
        }

        return changed;
    }

    private void MoveUnitToAvoidingBuildings(BattleUnit unit, float targetX, float targetZ)
    {
        if (unit == null)
        {
            return;
        }

        Vector2 steered = ApplyBuildingAvoidanceSteering(unit, new Vector2(targetX, targetZ));
        float dx = steered.x - unit.x;
        float dz = steered.y - unit.z;
        float distance = Mathf.Sqrt(dx * dx + dz * dz);
        if (distance <= 0.001f)
        {
            ClampUnitPosition(unit);
            return;
        }

        int steps = Mathf.Clamp(Mathf.CeilToInt(distance / 28f), 1, 8);
        float stepX = dx / steps;
        float stepZ = dz / steps;
        for (int i = 0; i < steps; i++)
        {
            unit.x += stepX;
            unit.z += stepZ;
            ClampUnitPosition(unit);
        }
    }

    private Vector2 ApplyBuildingAvoidanceSteering(BattleUnit unit, Vector2 target)
    {
        if (!AvoidsBuildings(unit) || buildingObstacles.Count == 0)
        {
            return target;
        }

        Vector2 from = new Vector2(unit.x, unit.z);
        Vector2 travel = target - from;
        if (travel.sqrMagnitude <= 0.01f)
        {
            return target;
        }

        float bestT = float.PositiveInfinity;
        BuildingObstacle best = null;
        float radius = BuildingAvoidanceRadius(unit);
        for (int i = 0; i < buildingObstacles.Count; i++)
        {
            float t;
            if (SegmentIntersectsBuilding(from, target, buildingObstacles[i], radius, out t) && t < bestT)
            {
                bestT = t;
                best = buildingObstacles[i];
            }
        }

        if (best == null)
        {
            return target;
        }

        float side = BuildingBypassSide(unit, best, target);
        float expandedZ = best.HalfZ + best.Padding + radius + 18f;
        target.y = best.CenterZ + side * expandedZ;
        return target;
    }

    private static bool SegmentIntersectsBuilding(Vector2 from, Vector2 to, BuildingObstacle obstacle, float radius, out float t)
    {
        float expandedHalfX = obstacle.HalfX + obstacle.Padding + radius;
        float expandedHalfZ = obstacle.HalfZ + obstacle.Padding + radius;
        float minX = obstacle.CenterX - expandedHalfX;
        float maxX = obstacle.CenterX + expandedHalfX;
        float minZ = obstacle.CenterZ - expandedHalfZ;
        float maxZ = obstacle.CenterZ + expandedHalfZ;

        float tMin = 0f;
        float tMax = 1f;
        if (!IntersectSegmentAxis(from.x, to.x, minX, maxX, ref tMin, ref tMax)
            || !IntersectSegmentAxis(from.y, to.y, minZ, maxZ, ref tMin, ref tMax))
        {
            t = 0f;
            return false;
        }

        t = tMin;
        return true;
    }

    private static bool IntersectSegmentAxis(float from, float to, float min, float max, ref float tMin, ref float tMax)
    {
        float delta = to - from;
        if (Mathf.Abs(delta) <= 0.001f)
        {
            return from >= min && from <= max;
        }

        float inv = 1f / delta;
        float t1 = (min - from) * inv;
        float t2 = (max - from) * inv;
        if (t1 > t2)
        {
            float swap = t1;
            t1 = t2;
            t2 = swap;
        }

        tMin = Mathf.Max(tMin, t1);
        tMax = Mathf.Min(tMax, t2);
        return tMin <= tMax;
    }

    private float BuildingBypassSide(BattleUnit unit, BuildingObstacle obstacle, Vector2 target)
    {
        float[] preferences =
        {
            unit.z - obstacle.CenterZ,
            unit.baseZ - obstacle.CenterZ,
            target.y - obstacle.CenterZ,
        };

        for (int i = 0; i < preferences.Length; i++)
        {
            if (Mathf.Abs(preferences[i]) > 3f)
            {
                return Mathf.Sign(preferences[i]);
            }
        }

        return Noise(unit.id * 19.7f + obstacle.CenterX * 0.031f + obstacle.CenterZ * 0.017f) > 0.5f ? 1f : -1f;
    }

    private bool ResolveBuildingCollision(BattleUnit unit)
    {
        if (!AvoidsBuildings(unit) || buildingObstacles.Count == 0)
        {
            return false;
        }

        bool changed = false;
        float radius = BuildingAvoidanceRadius(unit);
        for (int i = 0; i < buildingObstacles.Count; i++)
        {
            var obstacle = buildingObstacles[i];
            float expandedHalfX = obstacle.HalfX + obstacle.Padding + radius;
            float expandedHalfZ = obstacle.HalfZ + obstacle.Padding + radius;
            float dx = unit.x - obstacle.CenterX;
            float dz = unit.z - obstacle.CenterZ;
            float absX = Mathf.Abs(dx);
            float absZ = Mathf.Abs(dz);
            if (absX >= expandedHalfX || absZ >= expandedHalfZ)
            {
                continue;
            }

            float signX = absX > 0.001f ? Mathf.Sign(dx) : (unit.facing >= 0 ? -1f : 1f);
            float signZ = absZ > 0.001f ? Mathf.Sign(dz) : BuildingBypassSide(unit, obstacle, new Vector2(unit.x, unit.z));
            float penetrationX = expandedHalfX - absX;
            float penetrationZ = expandedHalfZ - absZ;
            if (penetrationX < penetrationZ)
            {
                unit.x = obstacle.CenterX + signX * expandedHalfX;
            }
            else
            {
                unit.z = obstacle.CenterZ + signZ * expandedHalfZ;
            }

            changed = true;
        }

        return changed;
    }

    private static bool AvoidsBuildings(BattleUnit unit)
    {
        return unit != null
            && (unit.kind == UnitKind.Soldier
                || unit.kind == UnitKind.Tank
                || unit.kind == UnitKind.Aircraft
                || unit.kind == UnitKind.Giant);
    }

    private float BuildingAvoidanceRadius(BattleUnit unit)
    {
        switch (unit.kind)
        {
            case UnitKind.Soldier:
                return 16f;
            case UnitKind.Tank:
                return 48f;
            case UnitKind.Aircraft:
                return 52f;
            case UnitKind.Giant:
                return 76f;
            default:
                return Mathf.Max(10f, unit.radius);
        }
    }

    private bool ResolvePair(BattleUnit first, BattleUnit second, float padding)
    {
        if (first == null || second == null || !first.active || !second.active || first == second)
        {
            return false;
        }

        if (first.kind == UnitKind.Aircraft || second.kind == UnitKind.Aircraft)
        {
            return false;
        }

        float dx = first.x - second.x;
        float dz = first.z - second.z;
        float distanceSq = dx * dx + dz * dz;
        float minimum = (SeparationRadius(first) + SeparationRadius(second)) * padding;
        if (distanceSq >= minimum * minimum)
        {
            return false;
        }

        float distance = Mathf.Sqrt(Mathf.Max(0.0001f, distanceSq));
        if (distance < 0.1f)
        {
            float angle = Noise(first.id * 23.7f + second.id * 11.9f) * Mathf.PI * 2f;
            dx = Mathf.Cos(angle);
            dz = Mathf.Sin(angle);
            distance = 1f;
        }

        float nx = dx / distance;
        float nz = dz / distance;
        float push = (minimum - distance) + 0.25f;
        float firstWeight = PushWeight(first);
        float secondWeight = PushWeight(second);
        float totalWeight = firstWeight + secondWeight;
        if (totalWeight <= 0.001f)
        {
            return false;
        }

        float firstPush = push * (firstWeight / totalWeight);
        float secondPush = push * (secondWeight / totalWeight);

        first.x += nx * firstPush;
        first.z += nz * firstPush;
        second.x -= nx * secondPush;
        second.z -= nz * secondPush;
        ClampUnitPosition(first);
        ClampUnitPosition(second);
        return true;
    }

    private float SeparationRadius(BattleUnit unit)
    {
        switch (unit.kind)
        {
            case UnitKind.Tank:
                return 56f;
            case UnitKind.Giant:
                return 96f;
            case UnitKind.Soldier:
                return 18f;
            default:
                return unit.radius;
        }
    }

    private float PushWeight(BattleUnit unit)
    {
        switch (unit.kind)
        {
            case UnitKind.Giant:
                return 0.65f;
            case UnitKind.Tank:
                return 0.55f;
            case UnitKind.Soldier:
                return 1.4f;
            default:
                return 1f;
        }
    }

    private void ClampUnitPosition(BattleUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        float minX = unit.kind == UnitKind.Tank ? Left - 76f : unit.kind == UnitKind.Giant ? Left - 180f : Left - 150f;
        float maxX = unit.kind == UnitKind.Tank ? Right - 160f : unit.kind == UnitKind.Giant ? Right + 260f : Right - 48f;
        unit.x = Mathf.Clamp(unit.x, minX, maxX);
        unit.z = Mathf.Clamp(unit.z, Bottom + 44f, Top - 70f);
        ResolveBuildingCollision(unit);
        unit.x = Mathf.Clamp(unit.x, minX, maxX);
        unit.z = Mathf.Clamp(unit.z, Bottom + 44f, Top - 70f);
    }

    private void UpdateActiveTransforms(List<BattleUnit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].active)
            {
                UpdateUnitTransform(units[i], 0f);
            }
        }
    }

    private void FireHumanWeapon(BattleUnit unit, BattleUnit target)
    {
        unit.attackCooldown = unit.attackInterval * (0.9f + Noise(battleTime * 31f + unit.id) * 0.22f);
        unit.attackVisualTimer = unit.kind == UnitKind.Soldier ? 0.18f : 0.42f;

        Vector2 aim = DirectionTo(unit.x, unit.z, target.x, target.z, unit.turretYawDegrees);

        if (unit.kind == UnitKind.Soldier)
        {
            float muzzleX = unit.x + aim.x * 20f;
            float muzzleZ = unit.z + aim.y * 20f;
            PlayBattleEffect(BattleEffectId.MuzzleRifle, muzzleX, muzzleZ, 0.92f, 0.82f, RotationFromDirection(aim));
            PlayBattleAudio(BattleAudioCueId.RifleShot, muzzleX, muzzleZ, 0.92f);
            SpawnProjectile(ProjectileKind.Bullet, ProjectileTarget.Giant, muzzleX, muzzleZ, 0.95f, target.x - aim.x * 24f, target.z - aim.y * 24f, 1.9f, unit.damage, 0f, 760f, new Color(0.85f, 0.96f, 1f, 1f));
            return;
        }

        if (unit.kind == UnitKind.Tank)
        {
            Vector2 muzzle = TankMuzzlePoint(unit);
            Vector2 barrelAim = DirectionFromYaw(unit.turretYawDegrees);
            PlayBattleEffect(BattleEffectId.MuzzleTank, muzzle.x, muzzle.y, 0.78f, 1.0f, RotationFromDirection(barrelAim));
            PlayBattleEffect(BattleEffectId.ShellLaunchSmoke, muzzle.x, muzzle.y, 0.72f, 1.0f, RotationFromDirection(barrelAim));
            PlayBattleAudio(BattleAudioCueId.TankShot, muzzle.x, muzzle.y, 0.82f);
            TriggerCameraShake(0.08f, 0.035f);
            SpawnProjectile(ProjectileKind.Shell, ProjectileTarget.Giant, muzzle.x, muzzle.y, 0.82f, target.x - barrelAim.x * 24f, target.z - barrelAim.y * 24f, 2.35f, unit.damage, 52f, 520f, new Color(1f, 0.76f, 0.42f, 1f));
            return;
        }

        float bombX = unit.x + aim.x * 18f;
        float bombZ = unit.z + aim.y * 18f;
        PlayBattleEffect(BattleEffectId.MuzzleAircraft, bombX, bombZ, 2.35f, 0.85f, RotationFromDirection(aim));
        PlayBattleEffect(BattleEffectId.BombDropTrail, bombX, bombZ, 2.3f, 0.75f, Quaternion.identity);
        SpawnProjectile(ProjectileKind.Bomb, ProjectileTarget.Giant, bombX, bombZ, 2.35f, target.x - aim.x * 12f, target.z - aim.y * 12f, 0.18f, unit.damage, 76f, 430f, new Color(0.42f, 0.50f, 0.48f, 1f));
    }

    private void UpdateGiants(float dt)
    {
        for (int i = 0; i < giants.Count; i++)
        {
            UpdateGiantUnit(giants[i], dt);
        }
    }

    private void UpdateGiantUnit(BattleUnit giant, float dt)
    {
        if (giant == null || !giant.active)
        {
            return;
        }

        giant.animTimer += dt;
        giant.attackCooldown = Mathf.Max(0f, giant.attackCooldown - dt);
        giant.attackVisualTimer = Mathf.Max(0f, giant.attackVisualTimer - dt);
        giant.hitFlashTimer = Mathf.Max(0f, giant.hitFlashTimer - dt);

        var chaseTarget = FindNearestHuman(giant, true);
        var contactTarget = FindGiantContactTarget(giant);
        var engagementTarget = contactTarget ?? FindGiantEngagementTarget(giant);
        float rage = giant.hp / giant.maxHp < 0.45f ? 1.18f : 1f;
        float baseGiantSpeed = giantConfig != null ? giantConfig.MoveSpeed : Mathf.Max(1f, giant.speed);
        giant.speed = baseGiantSpeed * rage;
        float previousX = giant.x;
        float previousZ = giant.z;

        var faceTarget = contactTarget ?? engagementTarget ?? chaseTarget;
        if (faceTarget != null)
        {
            float targetYaw = DirectionYawDegrees(faceTarget.x - giant.x, faceTarget.z - giant.z, giant.headingDegrees);
            giant.headingDegrees = Mathf.LerpAngle(giant.headingDegrees, targetYaw, Mathf.Clamp01(dt * 4.6f));
        }

        if (engagementTarget != null && giant.attackCooldown <= 0f)
        {
            PerformGiantMeleeAttack(giant, engagementTarget);
        }

        if (contactTarget == null && chaseTarget != null)
        {
            float formationZ = Mathf.Clamp(chaseTarget.z + GiantFormationZOffset(giant), Bottom + 62f, Top - 88f);
            Vector2 chase = DirectionTo(giant.x, giant.z, chaseTarget.x, formationZ, giant.headingDegrees);
            MoveUnitToAvoidingBuildings(giant, giant.x + chase.x * giant.speed * dt, giant.z + chase.y * giant.speed * dt);
        }

        RecordUnitMovement(giant, previousX, previousZ, dt);
        UpdateUnitTransform(giant, dt);
    }

    private float GiantFormationZOffset(BattleUnit giant)
    {
        if (giant == null)
        {
            return 0f;
        }

        int lane = giant.rank % 5;
        int rank = giant.rank / 5;
        return (lane - 2) * 42f + rank * 18f;
    }

    private BattleUnit FindGiantContactTarget()
    {
        for (int i = 0; i < giants.Count; i++)
        {
            var target = FindGiantContactTarget(giants[i]);
            if (target != null)
            {
                return target;
            }
        }

        return null;
    }

    private BattleUnit FindGiantContactTarget(BattleUnit giant)
    {
        BattleUnit best = null;
        float bestScore = float.PositiveInfinity;
        ConsiderGiantContact(giant, soldiers, true, ref best, ref bestScore);
        ConsiderGiantContact(giant, tanks, true, ref best, ref bestScore);
        ConsiderGiantContact(giant, aircraft, true, ref best, ref bestScore);
        return best;
    }

    private BattleUnit FindGiantEngagementTarget()
    {
        for (int i = 0; i < giants.Count; i++)
        {
            var target = FindGiantEngagementTarget(giants[i]);
            if (target != null)
            {
                return target;
            }
        }

        return null;
    }

    private BattleUnit FindGiantEngagementTarget(BattleUnit giant)
    {
        BattleUnit best = null;
        float bestScore = float.PositiveInfinity;
        ConsiderGiantContact(giant, soldiers, false, ref best, ref bestScore);
        ConsiderGiantContact(giant, tanks, false, ref best, ref bestScore);
        ConsiderGiantContact(giant, aircraft, false, ref best, ref bestScore);
        return best;
    }

    private void ConsiderGiantContact(BattleUnit giant, List<BattleUnit> units, bool contactOnly, ref BattleUnit best, ref float bestScore)
    {
        if (giant == null || !giant.active)
        {
            return;
        }

        for (int i = 0; i < units.Count; i++)
        {
            var candidate = units[i];
            if (!candidate.active || !IsTargetInGiantMeleeRange(giant, candidate, contactOnly))
            {
                continue;
            }

            float score = DistanceSq(giant.x, giant.z, candidate.x, candidate.z);
            if (score < bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }
    }

    private bool IsTargetInGiantMeleeRange(BattleUnit giant, BattleUnit target, bool contactOnly)
    {
        if (target == null || giant == null || !target.active || !giant.active)
        {
            return false;
        }

        float reach = GiantMeleeDistance(target.kind, contactOnly);
        return DistanceSq(giant.x, giant.z, target.x, target.z) <= reach * reach;
    }

    private float GiantMeleeOffset(UnitKind kind)
    {
        switch (kind)
        {
            case UnitKind.Aircraft:
                return 76f;
            case UnitKind.Tank:
                return 104f;
            default:
                return 82f;
        }
    }

    private float GiantMeleeXReach(UnitKind kind, bool contactOnly)
    {
        switch (kind)
        {
            case UnitKind.Aircraft:
                return contactOnly ? 26f : 42f;
            case UnitKind.Tank:
                return contactOnly ? 24f : 40f;
            default:
                return contactOnly ? 18f : 32f;
        }
    }

    private float GiantMeleeZReach(UnitKind kind, bool contactOnly)
    {
        switch (kind)
        {
            case UnitKind.Aircraft:
                return contactOnly ? 760f : 800f;
            case UnitKind.Tank:
                return contactOnly ? 360f : 400f;
            default:
                return contactOnly ? 180f : 220f;
        }
    }

    private float GiantMeleeDistance(UnitKind kind, bool contactOnly)
    {
        switch (kind)
        {
            case UnitKind.Aircraft:
                return contactOnly ? 168f : 214f;
            case UnitKind.Tank:
                return contactOnly ? 212f : 252f;
            default:
                return contactOnly ? 98f : 132f;
        }
    }

    private BattleUnit FindNearestHuman(BattleUnit origin, bool includeAircraft)
    {
        BattleUnit best = null;
        float bestScore = float.PositiveInfinity;

        ConsiderNearest(soldiers, origin, includeAircraft, ref best, ref bestScore);
        ConsiderNearest(tanks, origin, includeAircraft, ref best, ref bestScore);
        ConsiderNearest(aircraft, origin, includeAircraft, ref best, ref bestScore);
        return best;
    }

    private void ConsiderNearest(List<BattleUnit> units, BattleUnit origin, bool includeAircraft, ref BattleUnit best, ref float bestScore)
    {
        for (int i = 0; i < units.Count; i++)
        {
            var candidate = units[i];
            if (!candidate.active)
            {
                continue;
            }

            if (!includeAircraft && candidate.kind == UnitKind.Aircraft)
            {
                continue;
            }

            float airPenalty = candidate.kind == UnitKind.Aircraft ? 1.35f : 1f;
            float score = DistanceSq(origin.x, origin.z, candidate.x, candidate.z) * airPenalty;
            if (score < bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }
    }

    private void PerformGiantSmash(BattleUnit giant, BattleUnit target)
    {
        if (giant == null || target == null)
        {
            return;
        }

        giant.attackCooldown = giant.attackInterval * (giant.hp / giant.maxHp < 0.45f ? 0.78f : 1f);
        giant.attackVisualTimer = 0.58f;

        float impactX = Mathf.Min(giant.x - 62f, target.x + 16f);
        float impactZ = target.z;
        SpawnEffect(impactX, impactZ + 24f, 1.75f, EffectKind.Fireball, 0.32f);
        ApplyAreaDamageToHumans(impactX, impactZ, 162f, giant.damage, true, 44f);
        ShowBanner("Giant smash", true, 0.95f);
    }

    private void PerformGiantMeleeAttack(BattleUnit giant, BattleUnit target)
    {
        if (giant == null || target == null)
        {
            return;
        }

        giant.attackCooldown = giant.attackInterval * (giant.hp / giant.maxHp < 0.45f ? 0.72f : 0.92f);
        giant.attackVisualTimer = 0.66f;

        var contactTarget = FindGiantContactTarget(giant);
        var visualTarget = contactTarget ?? target;
        Vector2 attackDir = DirectionTo(giant.x, giant.z, target.x, target.z, giant.headingDegrees);
        float impactX;
        float impactZ;
        if (contactTarget != null)
        {
            impactX = visualTarget.x - attackDir.x * 10f;
            impactZ = visualTarget.z - attackDir.y * 10f;
        }
        else
        {
            float whiffDistance = GiantMeleeDistance(target.kind, true) * 0.92f;
            impactX = giant.x + attackDir.x * whiffDistance;
            impactZ = giant.z + attackDir.y * whiffDistance;
        }

        BattleEffectId impactEffect = target.kind == UnitKind.Tank
            ? BattleEffectId.MonsterHammerImpact
            : target.kind == UnitKind.Aircraft
                ? BattleEffectId.ClawHit
                : BattleEffectId.MonsterStompDust;
        PlayBattleEffect(impactEffect, impactX, impactZ, target.kind == UnitKind.Aircraft ? 2.4f : 0.16f, target.kind == UnitKind.Tank ? 1.55f : 1.15f, Quaternion.identity);
        if (target.kind != UnitKind.Aircraft)
        {
            PlayBattleEffect(BattleEffectId.MonsterShockwave, impactX, impactZ, 0.08f, target.kind == UnitKind.Tank ? 1.2f : 0.9f, Quaternion.identity);
        }

        PlayBattleAudio(BattleAudioCueId.CreatureHit, impactX, impactZ, target.kind == UnitKind.Aircraft ? 2.2f : 0.2f);
        TriggerCameraShake(target.kind == UnitKind.Tank ? 0.20f : 0.14f, target.kind == UnitKind.Tank ? 0.13f : 0.08f);
        ApplyGiantContactDamage(giant);
        ShowBanner(target.kind == UnitKind.Aircraft ? "Giant swat" : target.kind == UnitKind.Tank ? "Giant hammer" : "Giant stomp", true, 0.85f);
    }

    private void ApplyGiantContactDamage(BattleUnit giant)
    {
        DamageGiantContactGroup(giant, soldiers);
        DamageGiantContactGroup(giant, tanks);
        DamageGiantContactGroup(giant, aircraft);
    }

    private void DamageGiantContactGroup(BattleUnit giant, List<BattleUnit> units)
    {
        if (giant == null || !giant.active)
        {
            return;
        }

        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (!unit.active || !IsTargetInGiantMeleeRange(giant, unit, true))
            {
                continue;
            }

            float reach = Mathf.Max(1f, GiantMeleeDistance(unit.kind, true));
            float pct = 1f - Mathf.Clamp01(Distance(giant.x, giant.z, unit.x, unit.z) / reach);
            float damage = unit.kind == UnitKind.Aircraft ? giant.damage * 0.88f : giant.damage;
            unit.hp -= damage * (0.76f + pct * 0.45f);

            if (unit.hp <= 0f)
            {
                DeactivateHumanUnit(unit);
            }
        }
    }

    private void ThrowGiantRock(BattleUnit giant, BattleUnit target)
    {
        if (giant == null || target == null)
        {
            return;
        }

        giant.attackCooldown = giant.attackInterval * 1.15f;
        giant.attackVisualTimer = 0.45f;
        SpawnProjectile(ProjectileKind.Rock, ProjectileTarget.Human, giant.x - 70f, giant.z + 128f, 4.6f, target.x, target.z + 8f, 0.75f, 116f, 76f, 470f, new Color(0.72f, 1f, 0.52f, 1f));
    }

    private void SpawnProjectile(ProjectileKind kind, ProjectileTarget target, float fromX, float fromZ, float fromHeight, float toX, float toZ, float toHeight, float damage, float radius, float speed, Color color)
    {
        if (projectiles.Count >= MaxProjectiles)
        {
            return;
        }

        ProjectileView projectile = null;
        for (int i = 0; i < projectiles.Count; i++)
        {
            if (!projectiles[i].active)
            {
                projectile = projectiles[i];
                break;
            }
        }

        if (projectile == null)
        {
            projectile = CreateProjectileView(kind, color);
            projectiles.Add(projectile);
        }

        projectile.kind = kind;
        projectile.target = target;
        projectile.fromX = fromX;
        projectile.fromZ = fromZ;
        projectile.toX = toX;
        projectile.toZ = toZ;
        projectile.fromHeight = fromHeight;
        projectile.toHeight = toHeight;
        projectile.damage = damage;
        projectile.radius = radius;
        projectile.speed = speed;
        projectile.color = color;
        ConfigureProjectileVisual(projectile, kind, color);
        projectile.duration = Mathf.Max(0.04f, Distance(fromX, fromZ, toX, toZ) / speed);
        projectile.progress = 0f;
        projectile.trailTimer = 0f;
        projectile.lastWorldPosition = ToWorldPoint(fromX, fromZ, fromHeight);
        projectile.worldPosition = projectile.lastWorldPosition;
        projectile.active = true;
        projectile.root.SetActive(true);
        UpdateProjectileVisual(projectile, 0f);
    }

    private ProjectileView CreateProjectileView(ProjectileKind kind, Color color)
    {
        var root = new GameObject($"{kind}_Projectile");
        root.transform.SetParent(projectileRoot, false);

        var lineObject = new GameObject("Trail");
        lineObject.transform.SetParent(root.transform, false);
        var line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.startWidth = kind == ProjectileKind.Bullet ? 0.03f : 0.08f;
        line.endWidth = kind == ProjectileKind.Bullet ? 0.03f : 0.06f;
        line.material = GetUnlitMaterial(color);
        line.startColor = color;
        line.endColor = color;

        var head = CreatePrimitive(PrimitiveType.Sphere, "Head", root.transform);
        head.transform.localScale = Vector3.one * (kind == ProjectileKind.Bullet ? 0.08f : 0.18f);
        head.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(color);

        root.SetActive(false);
        return new ProjectileView
        {
            root = root,
            line = line,
            head = head.transform,
            active = false,
        };
    }

    private void ConfigureProjectileVisual(ProjectileView projectile, ProjectileKind kind, Color color)
    {
        if (projectile == null)
        {
            return;
        }

        if (projectile.line != null)
        {
            float startWidth = kind == ProjectileKind.Bullet ? 0.03f : kind == ProjectileKind.Bomb ? 0.10f : 0.08f;
            float endWidth = kind == ProjectileKind.Bullet ? 0.03f : kind == ProjectileKind.Bomb ? 0.12f : 0.06f;
            projectile.line.startWidth = startWidth;
            projectile.line.endWidth = endWidth;
            projectile.line.material = GetUnlitMaterial(color);
            projectile.line.startColor = color;
            projectile.line.endColor = new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a * 0.55f));
        }

        if (projectile.head != null)
        {
            float scale = ProjectileHeadScale(kind);
            projectile.head.localScale = new Vector3(scale * 0.7f, scale * 1.2f, scale * 0.7f);
            var renderer = projectile.head.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetOpaqueMaterial(color);
            }
        }
    }

    private void UpdateProjectiles(float dt)
    {
        for (int i = 0; i < projectiles.Count; i++)
        {
            var shot = projectiles[i];
            if (!shot.active)
            {
                continue;
            }

            shot.progress += dt / Mathf.Max(0.04f, shot.duration);
            float t = Mathf.Clamp01(shot.progress);
            float arc = shot.kind == ProjectileKind.Bomb
                ? Mathf.Sin(t * Mathf.PI) * 0.35f - t * t * 1.45f
                : shot.kind == ProjectileKind.Shell || shot.kind == ProjectileKind.Rock
                    ? Mathf.Sin(t * Mathf.PI) * 1.45f
                    : 0f;
            shot.lastWorldPosition = shot.worldPosition;
            shot.worldPosition = ToWorldPoint(Mathf.Lerp(shot.fromX, shot.toX, t), Mathf.Lerp(shot.fromZ, shot.toZ, t), Mathf.Lerp(shot.fromHeight, shot.toHeight, t) + arc);
            if (shot.kind == ProjectileKind.Bomb)
            {
                shot.trailTimer -= dt;
                if (shot.trailTimer <= 0f)
                {
                    shot.trailTimer = 0.08f;
                    PlayBattleEffect(BattleEffectId.BombDropTrail, shot.worldPosition, 0.42f, Quaternion.identity);
                }
            }
            UpdateProjectileVisual(shot, t);

            if (t >= 1f)
            {
                ResolveProjectileImpact(shot);
            }
        }
    }

    private void UpdateProjectileVisual(ProjectileView shot, float t)
    {
        if (!shot.active || shot.root == null)
        {
            return;
        }

        shot.line.SetPosition(0, shot.lastWorldPosition);
        shot.line.SetPosition(1, shot.worldPosition);
        shot.head.position = shot.worldPosition;
        Vector3 direction = shot.worldPosition - shot.lastWorldPosition;
        if (direction.sqrMagnitude > 0.0001f)
        {
            shot.head.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        float pulse = shot.kind == ProjectileKind.Bullet ? 1f : 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
        float scale = ProjectileHeadScale(shot.kind) * pulse;
        shot.head.localScale = shot.kind == ProjectileKind.Bomb
            ? new Vector3(scale * 0.75f, scale * 1.55f, scale * 0.75f)
            : Vector3.one * scale;
    }

    private void ResolveProjectileImpact(ProjectileView shot)
    {
        shot.active = false;
        shot.root.SetActive(false);

        if (shot.target == ProjectileTarget.Giant)
        {
            if (shot.radius > 0f)
            {
                DamageGiantsInArea(shot.toX, shot.toZ, shot.radius, shot.damage);
            }
            else
            {
                DamageGiantAt(shot.toX, shot.toZ, shot.damage);
            }

            if (shot.kind == ProjectileKind.Shell || shot.kind == ProjectileKind.Rocket || shot.kind == ProjectileKind.Bomb)
            {
                BattleEffectId impact = shot.kind == ProjectileKind.Bomb ? BattleEffectId.BombExplosion : BattleEffectId.ShellExplosionLarge;
                float scale = shot.kind == ProjectileKind.Bomb ? 1.7f : shot.kind == ProjectileKind.Rocket ? 1.55f : 1.25f;
                PlayBattleEffect(impact, shot.toX, shot.toZ, 0.18f, scale, Quaternion.identity);
                PlayBattleAudio(shot.kind == ProjectileKind.Bomb ? BattleAudioCueId.ExplosionLarge : BattleAudioCueId.ExplosionSmall, shot.toX, shot.toZ, 0.2f);
                TriggerCameraShake(shot.kind == ProjectileKind.Bomb ? 0.18f : 0.12f, shot.kind == ProjectileKind.Bomb ? 0.11f : 0.07f);
            }
            else if (Noise(battleTime * 25f + shot.toX) > 0.68f)
            {
                PlayBattleEffect(BattleEffectId.BulletHitMetal, shot.toX, shot.toZ, 1.7f, 0.65f, Quaternion.identity);
            }

            return;
        }

        PlayBattleEffect(shot.kind == ProjectileKind.Rock ? BattleEffectId.MonsterHammerImpact : BattleEffectId.ShellExplosionSmall, shot.toX, shot.toZ, 0.12f, shot.kind == ProjectileKind.Rock ? 1.35f : 1.0f, Quaternion.identity);
        PlayBattleAudio(BattleAudioCueId.ExplosionSmall, shot.toX, shot.toZ, 0.12f);
        TriggerCameraShake(0.12f, 0.08f);
        ApplyAreaDamageToHumans(shot.toX, shot.toZ, shot.radius, shot.damage, false, 36f);
    }

    private static float ProjectileHeadScale(ProjectileKind kind)
    {
        switch (kind)
        {
            case ProjectileKind.Bullet:
                return 0.08f;
            case ProjectileKind.Bomb:
                return 0.24f;
            case ProjectileKind.Rock:
                return 0.26f;
            default:
                return 0.18f;
        }
    }

    private void CleanupProjectiles()
    {
        for (int i = 0; i < projectiles.Count; i++)
        {
            if (projectiles[i].active)
            {
                continue;
            }
        }
    }

    private void ApplyAreaDamageToHumans(float x, float z, float radius, float damage, bool groundOnly, float knockback)
    {
        float radiusSq = radius * radius;
        DamageHumanGroup(soldiers, x, z, radius, radiusSq, damage, groundOnly, knockback);
        DamageHumanGroup(tanks, x, z, radius, radiusSq, damage, groundOnly, knockback);
        DamageHumanGroup(aircraft, x, z, radius, radiusSq, damage, groundOnly, knockback);
    }

    private void DamageHumanGroup(List<BattleUnit> units, float x, float z, float radius, float radiusSq, float damage, bool groundOnly, float knockback)
    {
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (!unit.active)
            {
                continue;
            }

            if (groundOnly && unit.kind == UnitKind.Aircraft)
            {
                continue;
            }

            float dSq = DistanceSq(unit.x, unit.z, x, z);
            if (dSq > radiusSq)
            {
                continue;
            }

            float pct = 1f - Mathf.Sqrt(dSq) / Mathf.Max(1f, radius);
            unit.hp -= damage * (0.62f + pct * 0.55f);
            unit.x -= knockback * (0.35f + pct);

            if (unit.hp <= 0f)
            {
                DeactivateHumanUnit(unit);
            }
        }
    }

    private void DeactivateHumanUnit(BattleUnit unit)
    {
        if (!unit.active)
        {
            return;
        }

        unit.active = false;
        unit.root.SetActive(false);
        humanLosses++;

        switch (unit.kind)
        {
            case UnitKind.Tank:
                SpawnDeathVisual(unit);
                PlayBattleEffect(BattleEffectId.TankDeathExplosion, unit.x, unit.z, 0.35f, 1.45f, Quaternion.identity);
                PlayBattleEffect(BattleEffectId.TankWreckSmoke, unit.x, unit.z, 0.25f, 1.1f, Quaternion.identity);
                PlayBattleAudio(BattleAudioCueId.ExplosionLarge, unit.x, unit.z, 0.35f);
                TriggerCameraShake(0.22f, 0.15f);
                break;
            case UnitKind.Aircraft:
                SpawnDeathVisual(unit);
                PlayBattleEffect(BattleEffectId.AircraftDeathExplosion, unit.x, unit.z, 2.45f, 1.5f, Quaternion.identity);
                PlayBattleEffect(BattleEffectId.AircraftCrashSmoke, unit.x, unit.z, 1.2f, 1.0f, Quaternion.identity);
                PlayBattleAudio(BattleAudioCueId.ExplosionLarge, unit.x, unit.z, 2.2f);
                TriggerCameraShake(0.18f, 0.12f);
                break;
            default:
                SpawnDeathVisual(unit);
                PlayBattleEffect(BattleEffectId.SoldierDeath, unit.x, unit.z, 0.08f, 0.75f, Quaternion.identity);
                break;
        }
    }

    private void DamageGiantAt(float x, float z, float amount)
    {
        var giant = FindNearestActiveGiant(x, z);
        if (giant == null)
        {
            return;
        }

        giant.hp = Mathf.Max(0f, giant.hp - amount);
        giant.hitFlashTimer = 0.055f;
        if (giant.hp <= 0f)
        {
            DefeatGiant(giant);
        }
    }

    private void DamageGiantsInArea(float x, float z, float radius, float amount)
    {
        float radiusSq = radius * radius;
        for (int i = 0; i < giants.Count; i++)
        {
            var giant = giants[i];
            if (giant == null || !giant.active)
            {
                continue;
            }

            float distanceSq = DistanceSq(x, z, giant.x, giant.z);
            if (distanceSq > radiusSq)
            {
                continue;
            }

            float pct = 1f - Mathf.Clamp01(Mathf.Sqrt(distanceSq) / Mathf.Max(1f, radius));
            giant.hp = Mathf.Max(0f, giant.hp - amount * (0.55f + pct * 0.65f));
            giant.hitFlashTimer = 0.09f;
            if (giant.hp <= 0f)
            {
                DefeatGiant(giant);
            }
        }
    }

    private BattleUnit FindNearestActiveGiant(float x, float z)
    {
        BattleUnit best = null;
        float bestScore = float.PositiveInfinity;

        for (int i = 0; i < giants.Count; i++)
        {
            var candidate = giants[i];
            if (candidate == null || !candidate.active)
            {
                continue;
            }

            float score = DistanceSq(x, z, candidate.x, candidate.z);
            if (score < bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    private void DefeatGiant(BattleUnit giant)
    {
        if (giant == null || !giant.active)
        {
            return;
        }

        giant.active = false;
        giant.root.SetActive(false);
        SpawnDeathVisual(giant);
        PlayBattleEffect(BattleEffectId.MonsterDeathExplosion, giant.x - 38f, giant.z + 80f, 0.8f, 1.5f, Quaternion.identity);
        PlayBattleEffect(BattleEffectId.MonsterDeathDust, giant.x + 32f, giant.z + 128f, 0.18f, 1.55f, Quaternion.identity);
        PlayBattleEffect(BattleEffectId.MonsterDeathExplosion, giant.x - 6f, giant.z + 34f, 0.45f, 1.35f, Quaternion.identity);
        PlayBattleAudio(BattleAudioCueId.ExplosionLarge, giant.x, giant.z, 0.4f);
        TriggerCameraShake(0.32f, 0.24f);
        if (CountActive(giants) <= 0)
        {
            ended = true;
            ShowBanner("Humans win", true, 4f);
        }

        RefreshHud();
    }

    private void SpawnEffect(float x, float z, float size, EffectKind kind, float duration)
    {
        EffectView effect = null;
        for (int i = 0; i < effects.Count; i++)
        {
            if (!effects[i].active)
            {
                effect = effects[i];
                break;
            }
        }

        if (effect == null)
        {
            effect = CreateEffectView();
            effects.Add(effect);
        }

        effect.kind = kind;
        effect.life = duration;
        effect.maxLife = duration;
        effect.active = true;
        effect.root.SetActive(true);
        effect.root.transform.position = ToWorldPoint(x, z, 0f);
        effect.baseScale = size;
        ConfigureEffectVisual(effect, kind);
        UpdateEffectVisual(effect, 0f);
    }

    private EffectView CreateEffectView()
    {
        var root = new GameObject("Effect");
        root.transform.SetParent(effectRoot, false);

        var orb = CreatePrimitive(PrimitiveType.Sphere, "Orb", root.transform);
        orb.transform.localScale = Vector3.one * 0.6f;
        orb.GetComponent<Renderer>().sharedMaterial = GetTransparentMaterial(new Color(1f, 0.56f, 0.14f, 0.8f));

        var light = root.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 8f;
        light.intensity = 2.5f;
        light.color = new Color(1f, 0.64f, 0.34f, 1f);

        root.SetActive(false);
        return new EffectView
        {
            root = root,
            orb = orb.transform,
            light = light,
            active = false,
        };
    }

    private void ConfigureEffectVisual(EffectView effect, EffectKind kind)
    {
        var renderer = effect.orb.GetComponent<Renderer>();
        if (kind == EffectKind.Fireball)
        {
            renderer.sharedMaterial = GetTransparentMaterial(new Color(1f, 0.48f, 0.14f, 0.84f));
            effect.light.color = new Color(1f, 0.68f, 0.32f, 1f);
            effect.light.intensity = 2.7f;
            effect.light.range = 8f;
        }
        else
        {
            renderer.sharedMaterial = GetTransparentMaterial(new Color(0.72f, 0.78f, 0.82f, 0.6f));
            effect.light.color = new Color(0.72f, 0.78f, 0.82f, 1f);
            effect.light.intensity = 0.9f;
            effect.light.range = 5f;
        }
    }

    private void UpdateEffects(float dt)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            if (!effect.active)
            {
                continue;
            }

            effect.life -= dt;
            float t = 1f - effect.life / Mathf.Max(0.001f, effect.maxLife);
            UpdateEffectVisual(effect, t);

            if (effect.life <= 0f)
            {
                effect.active = false;
                effect.root.SetActive(false);
            }
        }
    }

    private void SpawnDeathVisual(BattleUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        var visual = GetOrCreateDeathVisual(unit.kind);
        if (visual == null)
        {
            return;
        }

        visual.kind = unit.kind;
        visual.life = DeathVisualLifetime(unit.kind);
        visual.maxLife = visual.life;
        visual.active = true;
        visual.crashTriggered = false;
        visual.smokeTimer = unit.kind == UnitKind.Tank ? 0.35f : 0f;
        visual.root.SetActive(true);
        visual.root.transform.position = ToWorldPoint(unit.x, unit.z, unit.kind == UnitKind.Aircraft ? 2.25f : 0.04f);

        float yaw = unit.kind == UnitKind.Tank || unit.kind == UnitKind.Giant ? unit.headingDegrees : unit.facing < 0 ? -90f : 90f;
        switch (unit.kind)
        {
            case UnitKind.Soldier:
                visual.root.transform.rotation = Quaternion.Euler(0f, yaw, 82f);
                visual.root.transform.localScale = Vector3.one * 0.9f;
                visual.velocity = Vector3.zero;
                break;
            case UnitKind.Tank:
                visual.root.transform.rotation = Quaternion.Euler(0f, yaw + Noise(unit.id * 0.31f) * 18f - 9f, 0f);
                visual.root.transform.localScale = Vector3.one * 1.0f;
                visual.velocity = Vector3.zero;
                break;
            case UnitKind.Aircraft:
                visual.root.transform.rotation = Quaternion.Euler(-12f, yaw, 28f);
                visual.root.transform.localScale = Vector3.one * 0.95f;
                visual.velocity = new Vector3(unit.facing * 0.9f, -2.6f, -0.35f + Noise(unit.id + 71f) * 0.7f);
                break;
            case UnitKind.Giant:
                visual.root.transform.rotation = Quaternion.Euler(0f, yaw - 18f, 74f);
                visual.root.transform.localScale = Vector3.one * 1.0f;
                visual.velocity = Vector3.zero;
                break;
            default:
                visual.root.transform.rotation = Quaternion.identity;
                visual.root.transform.localScale = Vector3.one;
                visual.velocity = Vector3.zero;
                break;
        }
    }

    private DeathVisual GetOrCreateDeathVisual(UnitKind kind)
    {
        for (int i = 0; i < deathVisuals.Count; i++)
        {
            var visual = deathVisuals[i];
            if (!visual.active && visual.kind == kind)
            {
                return visual;
            }
        }

        if (deathVisuals.Count < MaxDeathVisuals)
        {
            var visual = CreateDeathVisual(kind);
            deathVisuals.Add(visual);
            return visual;
        }

        DeathVisual oldest = null;
        for (int i = 0; i < deathVisuals.Count; i++)
        {
            if (oldest == null || deathVisuals[i].life < oldest.life)
            {
                oldest = deathVisuals[i];
            }
        }

        return oldest != null && oldest.kind == kind ? oldest : null;
    }

    private DeathVisual CreateDeathVisual(UnitKind kind)
    {
        var root = new GameObject($"{kind}_DeathVisual");
        root.transform.SetParent(effectRoot, false);

        switch (kind)
        {
            case UnitKind.Soldier:
                CreateDeathVisualPart(root.transform, PrimitiveType.Capsule, "FallenSoldierBody", new Vector3(0f, 0.16f, 0f), new Vector3(0.12f, 0.32f, 0.12f), Quaternion.Euler(0f, 0f, 90f), new Color(0.13f, 0.20f, 0.24f, 1f));
                CreateDeathVisualPart(root.transform, PrimitiveType.Sphere, "FallenSoldierHead", new Vector3(0.22f, 0.18f, 0f), new Vector3(0.09f, 0.09f, 0.09f), Quaternion.identity, new Color(0.72f, 0.58f, 0.42f, 1f));
                break;
            case UnitKind.Tank:
                CreateDeathVisualPart(root.transform, PrimitiveType.Cube, "TankWreckHull", new Vector3(0f, 0.18f, 0f), new Vector3(0.72f, 0.24f, 0.48f), Quaternion.Euler(0f, 0f, -4f), new Color(0.10f, 0.11f, 0.10f, 1f));
                CreateDeathVisualPart(root.transform, PrimitiveType.Cylinder, "TankWreckTurret", new Vector3(0.05f, 0.35f, 0.02f), new Vector3(0.30f, 0.13f, 0.30f), Quaternion.Euler(0f, 28f, 0f), new Color(0.12f, 0.12f, 0.11f, 1f));
                CreateDeathVisualPart(root.transform, PrimitiveType.Cube, "TankWreckBarrel", new Vector3(0.40f, 0.36f, 0.02f), new Vector3(0.58f, 0.055f, 0.055f), Quaternion.Euler(0f, 84f, 0f), new Color(0.08f, 0.08f, 0.08f, 1f));
                break;
            case UnitKind.Aircraft:
                CreateDeathVisualPart(root.transform, PrimitiveType.Capsule, "AircraftWreckBody", new Vector3(0f, 0f, 0f), new Vector3(0.18f, 0.54f, 0.18f), Quaternion.Euler(90f, 0f, 0f), new Color(0.08f, 0.12f, 0.13f, 1f));
                CreateDeathVisualPart(root.transform, PrimitiveType.Cube, "AircraftWreckWing", new Vector3(0f, 0f, 0f), new Vector3(0.90f, 0.035f, 0.18f), Quaternion.identity, new Color(0.10f, 0.15f, 0.16f, 1f));
                break;
            case UnitKind.Giant:
                CreateDeathVisualPart(root.transform, PrimitiveType.Capsule, "MonsterFallenBody", new Vector3(0f, 0.42f, 0f), new Vector3(0.82f, 1.35f, 0.82f), Quaternion.Euler(0f, 0f, 90f), new Color(0.20f, 0.12f, 0.10f, 1f));
                CreateDeathVisualPart(root.transform, PrimitiveType.Sphere, "MonsterFallenHead", new Vector3(0.92f, 0.46f, 0f), new Vector3(0.44f, 0.44f, 0.44f), Quaternion.identity, new Color(0.28f, 0.16f, 0.12f, 1f));
                CreateDeathVisualPart(root.transform, PrimitiveType.Cube, "MonsterFallenArm", new Vector3(0.05f, 0.22f, 0.52f), new Vector3(1.05f, 0.18f, 0.24f), Quaternion.Euler(0f, 0f, 15f), new Color(0.18f, 0.10f, 0.08f, 1f));
                break;
            default:
                CreateDeathVisualPart(root.transform, PrimitiveType.Cube, "DeathMarker", new Vector3(0f, 0.08f, 0f), new Vector3(0.3f, 0.12f, 0.3f), Quaternion.identity, new Color(0.10f, 0.10f, 0.10f, 1f));
                break;
        }

        root.SetActive(false);
        return new DeathVisual
        {
            kind = kind,
            root = root,
            active = false,
        };
    }

    private GameObject CreateDeathVisualPart(Transform parent, PrimitiveType primitive, string name, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Color color)
    {
        var part = CreatePrimitive(primitive, name, parent);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = localRotation;
        part.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(color);
        return part;
    }

    private void UpdateDeathVisuals(float dt)
    {
        for (int i = 0; i < deathVisuals.Count; i++)
        {
            var visual = deathVisuals[i];
            if (!visual.active)
            {
                continue;
            }

            visual.life -= dt;
            if (visual.kind == UnitKind.Aircraft)
            {
                UpdateAircraftDeathVisual(visual, dt);
            }
            else if (visual.kind == UnitKind.Tank)
            {
                visual.smokeTimer -= dt;
                if (visual.smokeTimer <= 0f)
                {
                    visual.smokeTimer = 1.05f;
                    Vector3 position = visual.root.transform.position;
                    PlayBattleEffect(BattleEffectId.TankWreckSmoke, position, 0.75f, Quaternion.identity);
                }
            }

            float t = 1f - Mathf.Clamp01(visual.life / Mathf.Max(0.001f, visual.maxLife));
            float shrink = visual.kind == UnitKind.Soldier ? Mathf.Lerp(1f, 0.75f, Mathf.Clamp01((t - 0.55f) / 0.45f)) : 1f;
            visual.root.transform.localScale = Vector3.one * shrink;

            if (visual.life <= 0f)
            {
                visual.active = false;
                visual.root.SetActive(false);
            }
        }
    }

    private void UpdateAircraftDeathVisual(DeathVisual visual, float dt)
    {
        visual.velocity += Vector3.down * dt * 2.8f;
        visual.root.transform.position += visual.velocity * dt;
        visual.root.transform.Rotate(52f * dt, 28f * dt, 114f * dt, Space.Self);
        if (!visual.crashTriggered && visual.root.transform.position.y <= 0.14f)
        {
            visual.crashTriggered = true;
            visual.velocity = Vector3.zero;
            Vector3 position = visual.root.transform.position;
            position.y = 0.08f;
            visual.root.transform.position = position;
            PlayBattleEffect(BattleEffectId.AircraftCrashSmoke, position, 1.0f, Quaternion.identity);
            PlayBattleEffect(BattleEffectId.ShellExplosionSmall, position, 0.9f, Quaternion.identity);
            TriggerCameraShake(0.10f, 0.07f);
        }
    }

    private static float DeathVisualLifetime(UnitKind kind)
    {
        switch (kind)
        {
            case UnitKind.Soldier:
                return 1.4f;
            case UnitKind.Aircraft:
                return 2.4f;
            case UnitKind.Giant:
                return 3.2f;
            case UnitKind.Tank:
                return 5.8f;
            default:
                return 1.5f;
        }
    }

    private void UpdateEffectVisual(EffectView effect, float t)
    {
        if (!effect.active)
        {
            return;
        }

        float rise = effect.kind == EffectKind.Smoke ? 0.85f : 0.4f;
        float pulse = effect.kind == EffectKind.Smoke ? 0.8f + t * 1.1f : 0.76f + Mathf.Sin(t * Mathf.PI) * 0.58f;
        effect.root.transform.localScale = Vector3.one * effect.baseScale * pulse;
        effect.root.transform.position += new Vector3(0f, rise * Time.deltaTime * (0.25f + t), 0f);
        effect.orb.localRotation = Quaternion.Euler(0f, effect.kind == EffectKind.Smoke ? t * 50f : t * 180f, 0f);
        var renderer = effect.orb.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
        {
            var color = renderer.sharedMaterial.color;
            color.a = Mathf.Clamp01(effect.kind == EffectKind.Smoke ? 0.6f * (1f - t * 0.75f) : 0.9f * (1f - t));
            renderer.sharedMaterial.color = color;
        }

        effect.light.intensity = effect.kind == EffectKind.Smoke ? 0.9f * (1f - t) : 2.7f * (1f - t * 0.8f);
    }

    private void RecordUnitMovement(BattleUnit unit, float previousX, float previousZ, float dt)
    {
        if (unit == null || dt <= 0f)
        {
            return;
        }

        float dx = unit.x - previousX;
        float dz = unit.z - previousZ;
        unit.moveSpeed = Mathf.Sqrt(dx * dx + dz * dz) / Mathf.Max(0.001f, dt);
    }

    private void UpdateUnitTransform(BattleUnit unit, float dt)
    {
        if (unit.root == null)
        {
            return;
        }

        unit.root.transform.position = ToWorldPoint(unit.x, unit.z, 0f);

        bool animatorMotion = UsesAnimatorPlayback(unit);
        float moveFactor = Mathf.Clamp01(unit.moveSpeed / Mathf.Max(1f, unit.speed * 0.75f));
        float cycle = unit.animTimer * MotionCycleSpeed(unit.kind, moveFactor) + unit.seed * Mathf.PI * 2f;
        float bob = unit.kind == UnitKind.Aircraft
            ? Mathf.Sin(battleTime * 4.8f + unit.seed * 12f) * 0.16f
            : unit.kind == UnitKind.Soldier && !animatorMotion
                ? Mathf.Abs(Mathf.Sin(cycle)) * 0.045f * moveFactor
                : unit.kind == UnitKind.Giant
                    ? Mathf.Abs(Mathf.Sin(cycle)) * 0.12f * moveFactor
                    : unit.kind == UnitKind.Tank
                        ? Mathf.Sin(cycle * 0.45f) * 0.018f * moveFactor
                        : 0f;

        unit.body.localPosition = new Vector3(0f, unit.altitude + bob, 0f);

        if (unit.tankAimRoot != null)
        {
            unit.tankAimRoot.localRotation = Quaternion.Euler(0f, unit.turretYawDegrees, 0f);
            float recoil = unit.attackVisualTimer > 0f ? -0.12f * Mathf.Sin(unit.attackVisualTimer * 24f) : 0f;
            if (unit.tankBarrelVisual != null)
            {
                unit.tankBarrelVisual.localPosition = new Vector3(0f, 0.08f, 0.62f + recoil);
            }

            if (unit.tankMuzzleVisual != null)
            {
                unit.tankMuzzleVisual.localPosition = new Vector3(0f, 0.08f, 1.24f + recoil);
            }
        }

        if (unit.modelInstance != null)
        {
            var pose = Poses[unit.kind];
            float mirrorYaw = pose.MirrorWithFacing && unit.facing < 0 ? 180f : 0f;
            float wobble = unit.kind == UnitKind.Aircraft ? Mathf.Sin(battleTime * 3.2f + unit.seed * 11f) * 3f : 0f;
            float hitBoost = unit.kind == UnitKind.Giant && unit.hitFlashTimer > 0f ? 1.08f : 1f;
            float attackBoost = unit.attackVisualTimer > 0f ? (unit.kind == UnitKind.Giant ? 1.04f : 1.02f) : 1f;
            float modelYaw = UsesDynamicHeading(unit.kind)
                ? pose.Yaw + (unit.headingDegrees - DefaultHeadingYaw(unit.kind)) + wobble
                : pose.Yaw + mirrorYaw + wobble;
            if (unit.kind == UnitKind.Tank)
            {
                modelYaw += unit.modelYawOffset;
            }

            Vector3 modelLocalPosition = unit.baseModelLocalPosition;
            Quaternion modelRotation = Quaternion.Euler(pose.Pitch, modelYaw, pose.Roll);
            if (!animatorMotion)
            {
                ApplyProceduralModelMotion(unit, cycle, moveFactor, ref modelLocalPosition, ref modelRotation);
            }
            unit.modelInstance.transform.localScale = unit.baseModelScale * hitBoost * attackBoost;
            unit.modelInstance.transform.localPosition = modelLocalPosition;
            unit.modelInstance.transform.localRotation = modelRotation;
            UpdateProceduralMotionRig(unit, dt, moveFactor);
            PlayUnitAnimation(unit, unit.attackVisualTimer > 0f);
        }
        else if (dt <= 0f)
        {
            AttachUnitModel(unit);
        }
    }

    private float MotionCycleSpeed(UnitKind kind, float moveFactor)
    {
        switch (kind)
        {
            case UnitKind.Soldier:
                return Mathf.Lerp(5.8f, 10.8f, moveFactor);
            case UnitKind.Giant:
                return Mathf.Lerp(2.2f, 4.1f, moveFactor);
            case UnitKind.Tank:
                return Mathf.Lerp(2.2f, 6.0f, moveFactor);
            default:
                return 1f;
        }
    }

    private void ApplyProceduralModelMotion(BattleUnit unit, float cycle, float moveFactor, ref Vector3 localPosition, ref Quaternion localRotation)
    {
        if (unit == null || moveFactor <= 0.001f)
        {
            return;
        }

        switch (unit.kind)
        {
            case UnitKind.Soldier:
            {
                float stride = Mathf.Sin(cycle);
                float footfall = Mathf.Abs(stride);
                localPosition += new Vector3(0f, footfall * 0.018f, Mathf.Sin(cycle * 0.5f + unit.seed) * 0.012f * moveFactor);
                localRotation *= Quaternion.Euler(-3.6f * moveFactor + footfall * 1.1f, 0f, stride * 4.8f * moveFactor);
                break;
            }
            case UnitKind.Giant:
            {
                float stride = Mathf.Sin(cycle);
                localPosition += new Vector3(0f, Mathf.Abs(stride) * 0.055f, 0f);
                localRotation *= Quaternion.Euler(stride * 2.2f * moveFactor, 0f, Mathf.Sin(cycle * 0.5f) * 4.2f * moveFactor);
                break;
            }
            case UnitKind.Tank:
            {
                localRotation *= Quaternion.Euler(Mathf.Sin(cycle * 0.55f) * 0.45f * moveFactor, 0f, Mathf.Sin(cycle) * 0.65f * moveFactor);
                break;
            }
            case UnitKind.Aircraft:
            {
                localRotation *= Quaternion.Euler(
                    Mathf.Sin(battleTime * 2.8f + unit.seed * 6f) * 1.4f,
                    0f,
                    Mathf.Sin(battleTime * 3.5f + unit.seed * 5f) * 3.4f);
                break;
            }
        }
    }

    private void UpdateProceduralMotionRig(BattleUnit unit, float dt, float moveFactor)
    {
        if (unit == null || dt <= 0f)
        {
            return;
        }

        if (unit.aircraftRotorRoot != null)
        {
            unit.rotorSpinDegrees = Mathf.Repeat(unit.rotorSpinDegrees + dt * (1680f + moveFactor * 620f), 360f);
            unit.aircraftRotorRoot.localRotation = Quaternion.Euler(0f, unit.rotorSpinDegrees, 0f);
        }

        var rig = unit.tankMotionRig;
        if (rig == null)
        {
            return;
        }

        if (rig.helperRoot != null)
        {
            rig.helperRoot.localRotation = Quaternion.Euler(0f, unit.headingDegrees - 90f, 0f);
        }

        float turretRelativeYaw = Mathf.DeltaAngle(unit.headingDegrees, unit.turretYawDegrees);
        for (int i = 0; i < rig.aimTransforms.Count && i < rig.aimBaseRotations.Count; i++)
        {
            var aim = rig.aimTransforms[i];
            if (aim != null)
            {
                aim.localRotation = Quaternion.Euler(0f, turretRelativeYaw, 0f) * rig.aimBaseRotations[i];
            }
        }

        float spinSign = unit.facing >= 0 ? -1f : 1f;
        unit.wheelSpinDegrees = Mathf.Repeat(unit.wheelSpinDegrees + spinSign * dt * Mathf.Max(0f, unit.moveSpeed) * 16f, 360f);
        for (int i = 0; i < rig.wheelTransforms.Count && i < rig.wheelBaseRotations.Count; i++)
        {
            var wheel = rig.wheelTransforms[i];
            if (wheel != null)
            {
                wheel.localRotation = rig.wheelBaseRotations[i] * Quaternion.Euler(0f, unit.wheelSpinDegrees, 0f);
            }
        }

        unit.trackScroll = Mathf.Repeat(unit.trackScroll + spinSign * dt * moveFactor * 2.4f, 1f);
        for (int i = 0; i < rig.trackMaterials.Count; i++)
        {
            SetTrackMaterialOffset(rig.trackMaterials[i], unit.trackScroll);
        }
    }

    private static void SetTrackMaterialOffset(Material material, float offset)
    {
        if (material == null)
        {
            return;
        }

        Vector2 textureOffset = new Vector2(-offset, 0f);
        if (material.HasProperty("_MainTex"))
        {
            material.SetTextureOffset("_MainTex", textureOffset);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTextureOffset("_BaseMap", textureOffset);
        }

        if (material.HasProperty("_BaseColorMap"))
        {
            material.SetTextureOffset("_BaseColorMap", textureOffset);
        }
    }

    private bool UsesDynamicHeading(UnitKind kind)
    {
        return kind == UnitKind.Tank || kind == UnitKind.Giant;
    }

    private float DefaultHeadingYaw(UnitKind kind)
    {
        return kind == UnitKind.Giant ? -90f : 90f;
    }

    private void PlayUnitAnimation(BattleUnit unit, bool attacking)
    {
        if (TryPlayAnimatorAnimation(unit, attacking))
        {
            return;
        }

        if (unit.animations == null || unit.animations.Length == 0)
        {
            return;
        }

        string desired = GetAnimationName(unit.kind, attacking);
        if (desired == unit.currentAnimation)
        {
            return;
        }

        for (int i = 0; i < unit.animations.Length; i++)
        {
            var animation = unit.animations[i];
            if (animation == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(desired) && animation.GetClip(desired) != null)
            {
                animation.Play(desired);
                unit.currentAnimation = desired;
                return;
            }

            // Fallback: substring matching
            string[] keywords = attacking ? new[] { "Attack", "Shoot", "Fire" } : new[] { "Walk", "Run", "Forward" };
            foreach (AnimationState state in animation)
            {
                if (state != null && state.clip != null)
                {
                    string clipName = state.clip.name;
                    foreach (string kw in keywords)
                    {
                        if (clipName.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            animation.Play(clipName);
                            unit.currentAnimation = desired;
                            return;
                        }
                    }
                }
            }

            if (animation.clip != null && string.IsNullOrEmpty(unit.currentAnimation))
            {
                animation.Play();
                unit.currentAnimation = animation.clip.name;
            }
        }
    }

    private bool TryPlayAnimatorAnimation(BattleUnit unit, bool attacking)
    {
        if (!UsesAnimatorPlayback(unit))
        {
            return false;
        }

        AnimationClip clip = SelectAnimatorClip(unit, attacking);
        if (clip == null)
        {
            return false;
        }

        if (!unit.animationGraph.IsValid())
        {
            unit.animationGraph = PlayableGraph.Create($"UnitAnimator_{unit.kind}_{unit.id}");
            unit.animationGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            unit.animationOutput = AnimationPlayableOutput.Create(unit.animationGraph, "Animation", unit.animator);
            unit.animationGraph.Play();
        }

        if (!unit.animationPlayable.IsValid() || !string.Equals(unit.currentAnimatorClip, clip.name, StringComparison.Ordinal))
        {
            if (unit.animationPlayable.IsValid())
            {
                unit.animationPlayable.Destroy();
            }

            unit.animationPlayable = AnimationClipPlayable.Create(unit.animationGraph, clip);
            unit.animationPlayable.SetApplyFootIK(false);
            unit.animationPlayable.SetApplyPlayableIK(false);
            unit.animationOutput.SetSourcePlayable(unit.animationPlayable);
            unit.currentAnimatorClip = clip.name;
        }

        if (unit.animationPlayable.IsValid())
        {
            unit.animationPlayable.SetSpeed(GetAnimatorClipSpeed(unit, clip));
        }

        return true;
    }

    private AnimationClip SelectAnimatorClip(BattleUnit unit, bool attacking)
    {
        if (unit.kind == UnitKind.Tank)
        {
            return SelectTankAnimatorClip(unit);
        }

        if (unit.kind == UnitKind.Giant)
        {
            return SelectGiantAnimatorClip(unit, attacking);
        }

        if (attacking)
        {
            return FindAnimatorClip(unit, "Idle_Gun", "Attack", "Shoot", "Fire", "Punch", "Idle");
        }

        bool moving = unit.moveSpeed > 1f;
        if (!moving)
        {
            return FindAnimatorClip(unit, "Idle_Gun", "Idle", "Walk_Gun", "Walk");
        }

        bool running = unit.moveSpeed > unit.speed * 1.08f;
        return running
            ? FindAnimatorClip(unit, "Run_Gun", "Run", "Walk_Gun", "Walk")
            : FindAnimatorClip(unit, "Walk_Gun", "Walk", "Run_Gun", "Run");
    }

    private AnimationClip SelectTankAnimatorClip(BattleUnit unit)
    {
        bool moving = unit.moveSpeed > 1f;
        if (!moving)
        {
            return FindAnimatorClip(unit, "Tank_Forward", "Forward", "Tank_TurningRight", "TurningRight");
        }

        return FindAnimatorClip(unit, "Tank_Forward", "Forward", "Tank_TurningRight", "TurningRight", "Tank_TurningLeft", "TurningLeft");
    }

    private AnimationClip SelectGiantAnimatorClip(BattleUnit unit, bool attacking)
    {
        if (attacking)
        {
            return FindAnimatorClip(unit, "Punch", "Headbutt", "Bite", "Attack", "Weapon", "HitReact", "Idle");
        }

        bool moving = unit.moveSpeed > 1f;
        if (!moving)
        {
            return FindAnimatorClip(unit, "Idle", "Walk", "Run");
        }

        bool running = unit.moveSpeed > unit.speed * 1.08f;
        return running
            ? FindAnimatorClip(unit, "Run", "Walk", "Idle")
            : FindAnimatorClip(unit, "Walk", "Run", "Idle");
    }

    private static AnimationClip FindAnimatorClip(BattleUnit unit, params string[] namesOrKeywords)
    {
        if (unit.animatorClips == null)
        {
            return null;
        }

        for (int i = 0; i < namesOrKeywords.Length; i++)
        {
            string candidate = namesOrKeywords[i];
            for (int c = 0; c < unit.animatorClips.Length; c++)
            {
                var clip = unit.animatorClips[c];
                if (clip != null && string.Equals(clip.name, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }
        }

        for (int i = 0; i < namesOrKeywords.Length; i++)
        {
            string keyword = namesOrKeywords[i];
            for (int c = 0; c < unit.animatorClips.Length; c++)
            {
                var clip = unit.animatorClips[c];
                if (clip != null && clip.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return clip;
                }
            }
        }

        return unit.animatorClips.Length > 0 ? unit.animatorClips[0] : null;
    }

    private static float GetAnimatorClipSpeed(BattleUnit unit, AnimationClip clip)
    {
        if (unit == null || clip == null)
        {
            return 1f;
        }

        float normalizedSpeed = Mathf.Clamp(unit.moveSpeed / Mathf.Max(1f, unit.speed), 0.55f, 1.45f);
        if (unit.kind == UnitKind.Tank)
        {
            return Mathf.Clamp(normalizedSpeed, 0.6f, 1.55f);
        }

        if (unit.kind == UnitKind.Giant)
        {
            return Mathf.Clamp(normalizedSpeed, 0.7f, 1.3f);
        }

        if (clip.name.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return Mathf.Clamp(normalizedSpeed / 0.72f, 0.75f, 1.35f);
        }

        if (clip.name.IndexOf("Run", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return Mathf.Clamp(normalizedSpeed, 0.8f, 1.35f);
        }

        return 1f;
    }

    private static bool UsesAnimatorPlayback(BattleUnit unit)
    {
        return unit != null
            && unit.animator != null
            && unit.animatorClips != null
            && unit.animatorClips.Length > 0;
    }

    private static void DisposeUnitAnimator(BattleUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        if (unit.animationGraph.IsValid())
        {
            unit.animationGraph.Destroy();
        }

        unit.animationPlayable = default;
        unit.animationOutput = default;
        unit.animator = null;
        unit.animatorClips = null;
        unit.currentAnimatorClip = string.Empty;
    }

    private string GetAnimationName(UnitKind kind, bool attacking)
    {
        switch (kind)
        {
            case UnitKind.Soldier:
                return attacking ? "CharacterArmature|Idle_Shoot" : "CharacterArmature|Run_Gun";
            case UnitKind.Tank:
                return attacking ? "TankArmature|Tank_TurningRight" : "TankArmature|Tank_Forward";
            case UnitKind.Giant:
                return attacking ? "EnemyArmature|EnemyArmature|EnemyArmature|Attack" : "EnemyArmature|EnemyArmature|EnemyArmature|Walk";
            default:
                return string.Empty;
        }
    }

    private void CheckBattleEnd()
    {
        if (ended)
        {
            return;
        }

        for (int i = 0; i < giants.Count; i++)
        {
            var unit = giants[i];
            if (unit != null && unit.active && unit.x < Left + 46f)
            {
                ended = true;
                ShowBanner("Human line broken", true, 4f);
                return;
            }
        }

        if (CountActive(giants) <= 0)
        {
            ended = true;
            ShowBanner("Humans win", true, 4f);
            return;
        }

        if (CountHumans() <= 0)
        {
            ended = true;
            ShowBanner("All human forces lost", true, 4f);
        }
    }

    private int CountHumans()
    {
        int alive = 0;
        CountActive(soldiers, ref alive);
        CountActive(tanks, ref alive);
        CountActive(aircraft, ref alive);
        return alive;
    }

    private void CountActive(List<BattleUnit> units, ref int alive)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].active)
            {
                alive++;
            }
        }
    }

    private int CountTankOverlaps()
    {
        int overlaps = 0;
        for (int i = 0; i < tanks.Count; i++)
        {
            var first = tanks[i];
            if (!first.active)
            {
                continue;
            }

            for (int j = i + 1; j < tanks.Count; j++)
            {
                var second = tanks[j];
                if (!second.active)
                {
                    continue;
                }

                float minimum = SeparationRadius(first) + SeparationRadius(second);
                if (Distance(first.x, first.z, second.x, second.z) < minimum)
                {
                    overlaps++;
                }
            }
        }

        return overlaps;
    }

    private float GetMinimumTankGap()
    {
        float minimumGap = float.PositiveInfinity;
        for (int i = 0; i < tanks.Count; i++)
        {
            var first = tanks[i];
            if (!first.active)
            {
                continue;
            }

            for (int j = i + 1; j < tanks.Count; j++)
            {
                var second = tanks[j];
                if (!second.active)
                {
                    continue;
                }

                float gap = Distance(first.x, first.z, second.x, second.z) - SeparationRadius(first) - SeparationRadius(second);
                minimumGap = Mathf.Min(minimumGap, gap);
            }
        }

        return float.IsPositiveInfinity(minimumGap) ? 0f : minimumGap;
    }

    private float GetAverageHeading(List<BattleUnit> units)
    {
        float sin = 0f;
        float cos = 0f;
        int active = 0;

        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit == null || !unit.active)
            {
                continue;
            }

            float radians = unit.headingDegrees * Mathf.Deg2Rad;
            sin += Mathf.Sin(radians);
            cos += Mathf.Cos(radians);
            active++;
        }

        return active <= 0 ? 0f : Mathf.Atan2(sin / active, cos / active) * Mathf.Rad2Deg;
    }

    private float GetAverageMoveSpeed(List<BattleUnit> units)
    {
        float total = 0f;
        int active = 0;
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit == null || !unit.active)
            {
                continue;
            }

            total += unit.moveSpeed;
            active++;
        }

        return active <= 0 ? 0f : total / active;
    }

    private Vector2 GetActiveGiantCenter()
    {
        float x = 0f;
        float z = 0f;
        int active = 0;

        for (int i = 0; i < giants.Count; i++)
        {
            var unit = giants[i];
            if (unit == null || !unit.active)
            {
                continue;
            }

            x += unit.x;
            z += unit.z;
            active++;
        }

        return active > 0 ? new Vector2(x / active, z / active) : Vector2.zero;
    }

    private float GetGiantHpTotal()
    {
        float total = 0f;
        for (int i = 0; i < giants.Count; i++)
        {
            var unit = giants[i];
            if (unit != null && unit.active)
            {
                total += Mathf.Max(0f, unit.hp);
            }
        }

        return total;
    }

    private float GetGiantMaxHpTotal()
    {
        float total = 0f;
        for (int i = 0; i < giants.Count; i++)
        {
            var unit = giants[i];
            if (unit != null)
            {
                total += Mathf.Max(0f, unit.maxHp);
            }
        }

        return total > 0f ? total : (giantConfig != null ? giantConfig.MaxHp : 2600f) * GiantCount;
    }

    private void RefreshHud()
    {
        int soldierAlive = CountActive(soldiers);
        int tankAlive = CountActive(tanks);
        int airAlive = CountActive(aircraft);
        int humanAlive = soldierAlive + tankAlive + airAlive;
        int humanTotal = SoldierCount + TankCount + AircraftCount;
        int giantAlive = CountActive(giants);
        float giantHp = Mathf.Ceil(GetGiantHpTotal());
        float giantMax = GetGiantMaxHpTotal();
        float hpPct = Mathf.Clamp01(giantHp / Mathf.Max(1f, giantMax));
        float humanPct = Mathf.Clamp01(humanAlive / Mathf.Max(1f, (float)humanTotal));
        int pool = 380000 + Mathf.FloorToInt(battleTime * 8200f) + humanLosses * 2600;
        float remaining = Mathf.Max(0f, 180f - battleTime);
        float skillCooldown = 9f - battleTime % 9f;

        if (leftTeamLabel != null)
        {
            leftTeamLabel.text = $"BLUE FORCE {humanAlive}";
        }

        if (rightTeamLabel != null)
        {
            rightTeamLabel.text = giantAlive > 0 ? "MONSTER SIDE" : "MONSTER DOWN";
        }

        if (battlePhaseLabel != null)
        {
            battlePhaseLabel.text = ended ? "RESULT" : paused ? "PAUSED" : "LIVE BARRAGE WAR";
        }

        if (poolLabel != null)
        {
            poolLabel.text = $"POINT POOL {pool:N0}";
        }

        if (timerLabel != null)
        {
            timerLabel.text = FormatTime(remaining);
        }

        humanLabel.text = $"Force {humanAlive}/{humanTotal}  Tanks {tankAlive}";
        giantLabel.text = $"Boss {giantAlive}/{GiantCount} HP {giantHp:0}";

        if (statusLabel != null)
        {
            statusLabel.text = paused ? "Paused" : ended ? "Battle over" : $"Battle {FormatTime(battleTime)}  Losses {humanLosses}";
        }

        if (bottomTickerLabel != null)
        {
            bottomTickerLabel.text = BuildTickerMessage(soldierAlive, tankAlive, airAlive, giantHp);
        }

        if (giftFeedLabel != null)
        {
            giftFeedLabel.text = $"Gift heat +{pool - 380000:N0}  Barrage combo x{1 + Mathf.FloorToInt(battleTime * 0.45f) % 9}";
        }

        if (skillCountdownLabel != null)
        {
            skillCountdownLabel.text = $"Barrage skill CD {Mathf.CeilToInt(skillCooldown)}s";
        }

        if (humanPowerFill != null)
        {
            humanPowerFill.fillAmount = humanPct;
            humanPowerFill.color = humanPct > 0.28f ? HumanColor : new Color(1f, 0.63f, 0.26f, 1f);
        }

        if (monsterPowerFill != null)
        {
            monsterPowerFill.fillAmount = hpPct;
            monsterPowerFill.color = hpPct > 0.35f ? GiantColor : new Color(1f, 0.82f, 0.24f, 1f);
        }

        if (hpFill != null && hpFill != monsterPowerFill)
        {
            hpFill.fillAmount = hpPct;
            hpFill.color = hpPct > 0.35f ? GiantColor : new Color(1f, 0.82f, 0.24f, 1f);
        }
    }

    private string BuildTickerMessage(int soldierAlive, int tankAlive, int airAlive, float giantHp)
    {
        int index = Mathf.Abs(Mathf.FloorToInt(battleTime * 0.7f)) % 5;
        switch (index)
        {
            case 0:
                return $"Barrage: blue force focus fire  soldiers {soldierAlive}/{SoldierCount}";
            case 1:
                return $"Barrage: tank line spacing stable  {tankAlive}/{TankCount} online";
            case 2:
                return $"Barrage: air support suppressing  helicopters {airAlive}/{AircraftCount}";
            case 3:
                return $"Barrage: boss HP {giantHp:0}  breach contested";
            default:
                return $"Barrage: drag to inspect the battlefield  losses {humanLosses}";
        }
    }

    private string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int total = Mathf.FloorToInt(seconds);
        int minutes = total / 60;
        int secs = total % 60;
        return $"{minutes:00}:{secs:00}";
    }

    private int CountActive(List<BattleUnit> units)
    {
        int total = 0;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].active)
            {
                total++;
            }
        }
        return total;
    }

    private int CountAnimatorUnits(List<BattleUnit> units)
    {
        int total = 0;
        for (int i = 0; i < units.Count; i++)
        {
            if (UsesAnimatorPlayback(units[i]))
            {
                total++;
            }
        }

        return total;
    }

    private int CountBuildingOverlaps()
    {
        int total = 0;
        total += CountBuildingOverlaps(soldiers);
        total += CountBuildingOverlaps(tanks);
        total += CountBuildingOverlaps(aircraft);
        total += CountBuildingOverlaps(giants);
        return total;
    }

    private int CountBuildingOverlaps(List<BattleUnit> units)
    {
        int total = 0;
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit != null && unit.active && IsInsideAnyBuildingObstacle(unit))
            {
                total++;
            }
        }

        return total;
    }

    private bool IsInsideAnyBuildingObstacle(BattleUnit unit)
    {
        if (!AvoidsBuildings(unit))
        {
            return false;
        }

        float radius = BuildingAvoidanceRadius(unit);
        for (int i = 0; i < buildingObstacles.Count; i++)
        {
            var obstacle = buildingObstacles[i];
            float expandedHalfX = obstacle.HalfX + obstacle.Padding + radius;
            float expandedHalfZ = obstacle.HalfZ + obstacle.Padding + radius;
            if (Mathf.Abs(unit.x - obstacle.CenterX) < expandedHalfX
                && Mathf.Abs(unit.z - obstacle.CenterZ) < expandedHalfZ)
            {
                return true;
            }
        }

        return false;
    }

    private string GetFirstAnimatorClipName(List<BattleUnit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (UsesAnimatorPlayback(unit) && !string.IsNullOrEmpty(unit.currentAnimatorClip))
            {
                return unit.currentAnimatorClip;
            }
        }

        return string.Empty;
    }

    private void ShowLoading(bool visible)
    {
        if (loadingPanel != null)
        {
            loadingPanel.gameObject.SetActive(visible);
        }
    }

    private void UpdateLoadingLabel()
    {
        if (loadingLabel == null || assetsReady)
        {
            return;
        }

        int dots = ((int)(loadingPulseTime * 3f) % 3) + 1;
        loadingLabel.text = $"Loading Poly Pizza 3D models{new string('.', dots)}";
    }

    private void SetLoadingMessage(string text)
    {
        if (loadingLabel != null)
        {
            loadingLabel.text = text;
        }
    }

    private void ShowBanner(string text, bool urgent, float duration)
    {
        if (bannerLabel == null)
        {
            return;
        }

        bannerLabel.gameObject.SetActive(true);
        bannerLabel.text = text;
        bannerLabel.color = urgent ? new Color(1f, 0.87f, 0.44f, 1f) : new Color(0.96f, 0.98f, 1f, 1f);
        CancelInvoke(nameof(HideBanner));
        Invoke(nameof(HideBanner), duration);
    }

    private void HideBanner()
    {
        if (bannerLabel != null)
        {
            bannerLabel.gameObject.SetActive(false);
        }
    }

    private Material GetOpaqueMaterial(Color color)
    {
        string key = $"opaque:{color.r:F3}:{color.g:F3}:{color.b:F3}:{color.a:F3}";
        Material material;
        if (materialCache.TryGetValue(key, out material))
        {
            return material;
        }

        var shader = FindRuntimeShader("RuntimeMaterials/RuntimeOpaque", "Standard", "Legacy Shaders/Diffuse", "Unlit/Color", "Sprites/Default");
        material = new Material(shader);
        material.color = color;
        material.SetFloat("_Glossiness", 0.12f);
        ApplyOpaqueDoubleSided(material);
        materialCache[key] = material;
        return material;
    }

    private Material GetTransparentMaterial(Color color)
    {
        string key = $"transparent:{color.r:F3}:{color.g:F3}:{color.b:F3}:{color.a:F3}";
        Material material;
        if (materialCache.TryGetValue(key, out material))
        {
            return material;
        }

        Shader shader = FindRuntimeShader("RuntimeMaterials/RuntimeTransparent", "Legacy Shaders/Transparent/Diffuse", "Standard", "Sprites/Default");

        material = new Material(shader);
        material.color = color;
        material.renderQueue = 3000;
        ApplyTransparentDoubleSided(material);
        materialCache[key] = material;
        return material;
    }

    private Material GetUnlitMaterial(Color color)
    {
        string key = $"unlit:{color.r:F3}:{color.g:F3}:{color.b:F3}:{color.a:F3}";
        Material material;
        if (materialCache.TryGetValue(key, out material))
        {
            return material;
        }

        Shader shader = FindRuntimeShader("RuntimeMaterials/RuntimeUnlit", "Sprites/Default", "Unlit/Color", "Standard");

        material = new Material(shader);
        material.color = color;
        material.renderQueue = 3000;
        materialCache[key] = material;
        return material;
    }

    private Shader FindRuntimeShader(string resourceMaterialPath, params string[] shaderNames)
    {
        var resourceMaterial = Resources.Load<Material>(resourceMaterialPath);
        if (resourceMaterial != null && resourceMaterial.shader != null)
        {
            return resourceMaterial.shader;
        }

        for (int i = 0; i < shaderNames.Length; i++)
        {
            var shader = Shader.Find(shaderNames[i]);
            if (shader != null)
            {
                return shader;
            }
        }

        var errorShader = Shader.Find("Hidden/InternalErrorShader");
        if (errorShader != null)
        {
            return errorShader;
        }

        throw new InvalidOperationException("No usable Unity shader could be found for runtime materials.");
    }

    private void ApplyOpaqueDoubleSided(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Color"))
        {
            var color = material.color;
            color.a = 1f;
            material.color = color;
        }

        if (material.HasProperty("_BaseColor"))
        {
            var color = material.GetColor("_BaseColor");
            color.a = 1f;
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", (int)CullMode.Off);
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 0f);
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 0f);
        }

        if (material.HasProperty("_SurfaceType"))
        {
            material.SetFloat("_SurfaceType", 0f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetInt("_SrcBlend", (int)BlendMode.One);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
        }

        if (material.HasProperty("_BUILTIN_SrcBlend"))
        {
            material.SetInt("_BUILTIN_SrcBlend", (int)BlendMode.One);
        }

        if (material.HasProperty("_BUILTIN_DstBlend"))
        {
            material.SetInt("_BUILTIN_DstBlend", (int)BlendMode.Zero);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 1f);
        }

        if (material.HasProperty("_BUILTIN_ZWrite"))
        {
            material.SetFloat("_BUILTIN_ZWrite", 1f);
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", 0f);
        }

        if (material.HasProperty("_BUILTIN_AlphaClip"))
        {
            material.SetFloat("_BUILTIN_AlphaClip", 0f);
        }

        if (material.HasProperty("_AlphaToMask"))
        {
            material.SetFloat("_AlphaToMask", 0f);
        }

        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_BUILTIN_AlphaClip");
        material.SetOverrideTag("RenderType", "Opaque");
        material.renderQueue = (int)RenderQueue.Geometry;
    }

    private void ApplyTransparentDoubleSided(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", (int)CullMode.Off);
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 2f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetInt("_ZWrite", 0);
        }

        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private GameObject CreatePrimitive(PrimitiveType primitiveType, string name, Transform parent)
    {
        var go = GameObject.CreatePrimitive(primitiveType);
        go.name = name;
        go.transform.SetParent(parent, false);
        var collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        return go;
    }

    private void SetAnchors(RectTransform rectTransform, float minX, float minY, float maxX, float maxY)
    {
        rectTransform.anchorMin = new Vector2(minX, minY);
        rectTransform.anchorMax = new Vector2(maxX, maxY);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void ApplyDefaultMobilePresentation()
    {
        if (!Application.isMobilePlatform)
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(720, 1280, FullScreenMode.Windowed);
        }

        Screen.orientation = ScreenOrientation.Portrait;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
    }

    private void CreateEventSystemIfNeeded()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(transform, false);
    }

    private void ApplySafeArea()
    {
        if (hudRoot == null)
        {
            return;
        }

        Rect safeArea = Screen.safeArea;
        if (safeArea.width <= 0f || safeArea.height <= 0f)
        {
            safeArea = new Rect(0f, 0f, Screen.width, Screen.height);
        }

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2(Screen.width, Screen.height);

        Vector2 min = safeArea.position;
        Vector2 max = safeArea.position + safeArea.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        hudRoot.anchorMin = min;
        hudRoot.anchorMax = max;
        hudRoot.offsetMin = Vector2.zero;
        hudRoot.offsetMax = Vector2.zero;
    }

    private void UpdateSafeAreaIfNeeded()
    {
        if (hudRoot == null)
        {
            return;
        }

        if (Screen.width != (int)lastScreenSize.x || Screen.height != (int)lastScreenSize.y || Screen.safeArea != lastSafeArea)
        {
            ApplySafeArea();
        }
    }

    private void CreateResolutionControls()
    {
        var strip = CreatePanel(hudRoot, "ResolutionStrip", new Color(0.03f, 0.04f, 0.05f, 0.82f));
        SetAnchors(strip.rectTransform, 0.035f, 0.006f, 0.965f, 0.046f);

        resolutionButtons = new Button[ResolutionPresets.Length];
        resolutionButtonImages = new Image[ResolutionPresets.Length];

        const float buttonWidth = 0.19f;
        const float gap = 0.01f;

        for (int i = 0; i < ResolutionPresets.Length; i++)
        {
            float minX = 0.01f + i * (buttonWidth + gap);
            float maxX = minX + buttonWidth;
            var button = CreateResolutionButton(strip.transform, $"Resolution_{i}", ResolutionPresets[i].Label, minX, maxX);
            int index = i;
            button.onClick.AddListener(() => ApplyResolutionPreset(index));
            resolutionButtons[i] = button;
            resolutionButtonImages[i] = button.GetComponent<Image>();
        }
    }

    private Button CreateResolutionButton(Transform parent, string name, string text, float minX, float maxX)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(minX, 0.12f);
        rect.anchorMax = new Vector2(maxX, 0.88f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.color = new Color(0.12f, 0.16f, 0.2f, 0.96f);

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;

        var label = CreateText(go.transform, "Label", text, 13, Color.white, TextAnchor.MiddleCenter);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 9;
        label.resizeTextMaxSize = 13;
        SetAnchors(label.rectTransform, 0f, 0f, 1f, 1f);
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;

        return button;
    }

    private void ApplyResolutionPreset(int index)
    {
        index = Mathf.Clamp(index, 0, ResolutionPresets.Length - 1);
        selectedResolutionIndex = index;

        var preset = ResolutionPresets[index];
        Screen.SetResolution(preset.Width, preset.Height, FullScreenMode.Windowed);
        ApplySafeArea();
        RefreshResolutionControls();
        ShowBanner(preset.Label, false, 1.2f);
    }

    private void RefreshResolutionControls()
    {
        if (resolutionButtons == null || resolutionButtonImages == null)
        {
            return;
        }

        for (int i = 0; i < resolutionButtons.Length; i++)
        {
            var selected = i == selectedResolutionIndex;
            if (resolutionButtonImages[i] != null)
            {
                resolutionButtonImages[i].color = selected
                    ? new Color(0.18f, 0.5f, 0.76f, 1f)
                    : new Color(0.12f, 0.16f, 0.2f, 0.96f);
            }
        }
    }

    private Image CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private Text CreateText(Transform parent, string name, string value, int fontSize, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = uiFont;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private void ConfigureTextFit(Text text, int minSize, int maxSize)
    {
        if (text == null)
        {
            return;
        }

        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = minSize;
        text.resizeTextMaxSize = maxSize;
    }

    private Vector3 ToWorldPoint(float x, float z, float height)
    {
        return new Vector3(x * LogicalToWorld, height, z * LogicalToWorld);
    }

    private void PlayBattleEffect(BattleEffectId id, float x, float z, float height, float scale, Quaternion rotation)
    {
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.Play(EffectPlayback.Create(id, ToWorldPoint(x, z, height), rotation, null, scale));
            return;
        }

        EffectKind fallback = IsSmokeFallback(id) ? EffectKind.Smoke : EffectKind.Fireball;
        SpawnEffect(x, z, Mathf.Max(0.1f, scale), fallback, IsSmokeFallback(id) ? 0.55f : 0.28f);
    }

    private void PlayBattleEffect(BattleEffectId id, Vector3 worldPosition, float scale, Quaternion rotation)
    {
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.Play(EffectPlayback.Create(id, worldPosition, rotation, null, scale));
            return;
        }

        SpawnEffect(worldPosition.x / LogicalToWorld, worldPosition.z / LogicalToWorld, Mathf.Max(0.1f, scale), IsSmokeFallback(id) ? EffectKind.Smoke : EffectKind.Fireball, IsSmokeFallback(id) ? 0.55f : 0.28f);
    }

    private void PlayBattleAudio(BattleAudioCueId id, float x, float z, float height)
    {
        if (BattleAudioManager.Instance != null)
        {
            BattleAudioManager.Instance.Play(id, ToWorldPoint(x, z, height));
        }
    }

    private void TriggerCameraShake(float duration, float amplitude)
    {
        cameraShakeDuration = Mathf.Max(cameraShakeDuration, duration);
        cameraShakeTime = Mathf.Max(cameraShakeTime, duration);
        cameraShakeAmplitude = Mathf.Max(cameraShakeAmplitude, amplitude);
    }

    private void UpdateCameraShake(float dt)
    {
        if (orbitCamera == null)
        {
            return;
        }

        if (cameraShakeTime <= 0f)
        {
            orbitCamera.shakeOffset = Vector3.zero;
            cameraShakeAmplitude = 0f;
            cameraShakeDuration = 0f;
            return;
        }

        cameraShakeTime = Mathf.Max(0f, cameraShakeTime - dt);
        float normalized = cameraShakeDuration > 0.001f ? cameraShakeTime / cameraShakeDuration : 0f;
        float falloff = normalized * normalized;
        float x = (Noise(battleTime * 91.7f + cameraShakeTime * 13.1f) - 0.5f) * 2f;
        float y = (Noise(battleTime * 73.3f + cameraShakeTime * 17.7f) - 0.5f) * 2f;
        orbitCamera.shakeOffset = new Vector3(x, y * 0.45f, 0f) * cameraShakeAmplitude * falloff;
    }

    private static Quaternion RotationFromDirection(Vector2 direction)
    {
        Vector3 forward = new Vector3(direction.x, 0f, direction.y);
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return Quaternion.identity;
        }

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private static bool IsSmokeFallback(BattleEffectId id)
    {
        return id == BattleEffectId.BulletHitDirt
            || id == BattleEffectId.BombDropTrail
            || id == BattleEffectId.ShellLaunchSmoke
            || id == BattleEffectId.TankWreckSmoke
            || id == BattleEffectId.AircraftCrashSmoke
            || id == BattleEffectId.MonsterDeathDust
            || id == BattleEffectId.SoldierDeath;
    }

    private float Distance(float ax, float az, float bx, float bz)
    {
        return Mathf.Sqrt(DistanceSq(ax, az, bx, bz));
    }

    private float DistanceSq(float ax, float az, float bx, float bz)
    {
        float dx = ax - bx;
        float dz = az - bz;
        return dx * dx + dz * dz;
    }

    private Vector2 DirectionTo(float fromX, float fromZ, float toX, float toZ, float fallbackYaw)
    {
        float dx = toX - fromX;
        float dz = toZ - fromZ;
        float length = Mathf.Sqrt(dx * dx + dz * dz);
        if (length <= 0.001f)
        {
            return DirectionFromYaw(fallbackYaw);
        }

        return new Vector2(dx / length, dz / length);
    }

    private float DirectionYawDegrees(float dx, float dz, float fallbackYaw)
    {
        if (Mathf.Abs(dx) + Mathf.Abs(dz) <= 0.001f)
        {
            return fallbackYaw;
        }

        return Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
    }

    private Vector2 DirectionFromYaw(float yawDegrees)
    {
        float radians = yawDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
    }

    private Vector2 TankMuzzlePoint(BattleUnit unit)
    {
        Vector2 direction = DirectionFromYaw(unit.turretYawDegrees);
        return new Vector2(unit.x + direction.x * 49f, unit.z + direction.y * 49f);
    }

    private float Noise(float seed)
    {
        float value = Mathf.Sin(seed * 127.1f + 311.7f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }

    private readonly struct ResolutionPreset
    {
        public readonly string Label;
        public readonly int Width;
        public readonly int Height;

        public ResolutionPreset(string label, int width, int height)
        {
            Label = label;
            Width = width;
            Height = height;
        }
    }


    private enum TankModelVariant
    {
        None,
        T55A,
        T55AK,
    }

    private enum TeamKind
    {
        Human,
        Giant,
    }

    private enum ProjectileKind
    {
        Bullet,
        Shell,
        Rocket,
        Rock,
        Bomb,
    }

    private enum ProjectileTarget
    {
        Giant,
        Human,
    }

    private enum EffectKind
    {
        Fireball,
        Smoke,
    }

    private sealed class ModelPose
    {
        public readonly float TargetHeight;
        public readonly float Pitch;
        public readonly float Yaw;
        public readonly float Roll;
        public readonly float BodyOffset;
        public readonly bool MirrorWithFacing;

        public ModelPose(float targetHeight, float pitch, float yaw, float roll, float bodyOffset, bool mirrorWithFacing)
        {
            TargetHeight = targetHeight;
            Pitch = pitch;
            Yaw = yaw;
            Roll = roll;
            BodyOffset = bodyOffset;
            MirrorWithFacing = mirrorWithFacing;
        }
    }

    private sealed class BattleUnit
    {
        public int id;
        public UnitKind kind;
        public TankModelVariant tankModel;
        public TeamKind team;
        public GameObject root;
        public Transform body;
        public GameObject modelInstance;
        public Animation[] animations;
        public Animator animator;
        public AnimationClip[] animatorClips;
        public PlayableGraph animationGraph;
        public AnimationClipPlayable animationPlayable;
        public AnimationPlayableOutput animationOutput;
        public string currentAnimation;
        public string currentAnimatorClip;
        public bool active;
        public Transform tankAimRoot;
        public Transform tankTurretVisual;
        public Transform tankBarrelVisual;
        public Transform tankMuzzleVisual;
        public float x;
        public float z;
        public float baseZ;
        public float altitude;
        public float hp;
        public float maxHp;
        public float damage;
        public float speed;
        public float radius;
        public float attackRange;
        public float attackInterval;
        public float attackCooldown;
        public float attackVisualTimer;
        public float hitFlashTimer;
        public float seed;
        public int rank;
        public int facing;
        public float headingDegrees;
        public float turretYawDegrees;
        public float modelYawOffset;
        public float animTimer;
        public float moveSpeed;
        public float rotorSpinDegrees;
        public float wheelSpinDegrees;
        public float trackScroll;
        public Vector3 baseModelScale;
        public Vector3 baseModelLocalPosition;
        public Transform aircraftRotorRoot;
        public TankMotionRig tankMotionRig;
        public GameObject motionAccessoryRoot;
    }

    private sealed class TankMotionRig
    {
        public readonly List<Transform> wheelTransforms = new List<Transform>();
        public readonly List<Quaternion> wheelBaseRotations = new List<Quaternion>();
        public readonly List<Material> trackMaterials = new List<Material>();
        public readonly List<Transform> aimTransforms = new List<Transform>();
        public readonly List<Quaternion> aimBaseRotations = new List<Quaternion>();
        public Transform helperRoot;
    }

    private sealed class BuildingObstacle
    {
        public readonly string Name;
        public readonly float CenterX;
        public readonly float CenterZ;
        public readonly float HalfX;
        public readonly float HalfZ;
        public readonly float Height;
        public readonly float Padding;

        public BuildingObstacle(string name, float centerX, float centerZ, float halfX, float halfZ, float height, float padding)
        {
            Name = name;
            CenterX = centerX;
            CenterZ = centerZ;
            HalfX = halfX;
            HalfZ = halfZ;
            Height = height;
            Padding = padding;
        }
    }

    private sealed class ProjectileView
    {
        public ProjectileKind kind;
        public ProjectileTarget target;
        public GameObject root;
        public LineRenderer line;
        public Transform head;
        public bool active;
        public float fromX;
        public float fromZ;
        public float toX;
        public float toZ;
        public float fromHeight;
        public float toHeight;
        public float progress;
        public float duration;
        public float trailTimer;
        public float damage;
        public float radius;
        public float speed;
        public Color color;
        public Vector3 lastWorldPosition;
        public Vector3 worldPosition;
    }

    private sealed class EffectView
    {
        public bool active;
        public GameObject root;
        public Transform orb;
        public Light light;
        public EffectKind kind;
        public float baseScale;
        public float life;
        public float maxLife;
    }

    private sealed class DeathVisual
    {
        public UnitKind kind;
        public GameObject root;
        public bool active;
        public float life;
        public float maxLife;
        public float smokeTimer;
        public bool crashTriggered;
        public Vector3 velocity;
    }
}
