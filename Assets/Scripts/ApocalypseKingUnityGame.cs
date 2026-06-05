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

public sealed partial class ApocalypseKingUnityGame : MonoBehaviour
{
    private const int SoldierCount = 50;
    private const int TankT55ACount = 25;
    private const int TankT55AkCount = 25;
    private const int TankCount = 50;
    private const int AircraftCount = 20;
    private const int PterosaurCount = 20;
    private const int RocketTruckCount = 18;
    private const float RocketTruckMoveSpeedRatio = 0.5f;
    private const int BaseGiantCount = 200;
    private const int RocketGiantCount = 20;
    private const int GiantCount = BaseGiantCount + RocketGiantCount;
    private const int MaxSoldierCount = 50;
    private const int MaxTankCount = TankCount + RocketTruckCount;
    private const int MaxAircraftCount = 20;
    private const int MaxPterosaurCount = 20;
    private const int MaxGiantCount = GiantCount;
    /// <summary>直升机与翼龙共用巡航高度（逻辑单位）。</summary>
    private const float SharedAirUnitDefaultAltitude = 7.2f;
    /// <summary>与直升机同高度带，避免飞得过高在默认镜头外。</summary>
    private const float PterosaurDefaultAltitude = SharedAirUnitDefaultAltitude + 0.6f;
    /// <summary>空中单位巡航时机身底部相对 body 原点的高度（翼龙/直升机共用底线）。</summary>
    private const float AirUnitCruiseBottomY = 0.5f;
    private const float AirUnitAltitudeBobAmplitude = 0.22f;
    private const float PterosaurModelTargetHeight = 4.2f;
    /// <summary>翼龙飞行动画播放倍率（GLB 含飞行动画时使用）。</summary>
    private const float PterosaurWingFlapAnimSpeed = 1.95f;
    /// <summary>无骨骼飞行动画时，程序化扇翼频率（Hz）。</summary>
    private const float PterosaurWingFlapFrequencyHz = 4.2f;
    private const float PterosaurWingFlapDegrees = 26f;
    /// <summary>GLB 机头沿 +X 时，原型已 -90° 对齐 +Z；此处仅做微调。</summary>
    private const float PterosaurMeshYawOffset = 0f;
    private const float PterosaurGlbBindYawDegrees = -90f;
    private const float RocketTruckModelTargetHeight = 2.8f;
    /// <summary>火箭炮车 GLB 机头沿 +X 时用 -90° 对齐游戏 +Z 前向。</summary>
    private const float RocketTruckMeshYawOffset = 0f;
    private const float RocketTruckAttackRangeBonus = 620f;
    private const float RocketTruckSiegeStandoffX = 420f;
    private const float RocketTruckBehindTankGapX = 132f;
    private const float RocketTruckMaxForwardFromSpawnX = 28f;
    private const float TankFormationSiegeStandoffX = 92f;
    private const float SoldierFormationSiegeStandoffX = 168f;
    private const float SpecialUnitBattleCenterX = 72f;
    private const string PterosaurResourceModelPath = "Monsters/Pterosaur/Pterosaur";
    private const string PterosaurPteranodonResourceModelPath = "Monsters/Pterosaur/Pteranodon";
    private const string PterosaurTextureResourcePath = "Monsters/Pterosaur/Textures";
    private const string RocketTruckResourceModelPath = "Vehicles/RocketTruck/RocketTruck";
    private const int MaxProjectiles = 220;
    private const int MaxEffects = 48;
    private const int MaxDeathVisuals = 56;
    private const int PrewarmBulletProjectiles = 80;
    private const int PrewarmShellProjectiles = 28;
    private const int PrewarmBombProjectiles = 10;
    private const int PrewarmRockProjectiles = 10;
    private const int PrewarmFallbackEffects = MaxEffects;
    private const bool ShowResolutionDebugControls = false;
    /// <summary>Quaternius tank GLB needs -90 degrees on Y to align mesh forward with game heading.</summary>
    /// <summary>Mesh forward vs game heading; realistic T-55 OBJ needs +90° from legacy Quaternius (-90).</summary>
    private const float TankT55AYawOffset = 0f;
    private const float TankT55AkYawOffset = 0f;
    private const float TankLowPolyYawOffset = -90f;
    private const float SoldierDefaultMoveSpeed = 36f;
    private const float TankDefaultMoveSpeed = 54f;
    private const float AircraftDefaultMoveSpeed = 100f;
    private const float TankModelTargetHeight = 4.2f;
    private const float TankHarmonizeDisplayBoost = 1.1f;
    /// <summary>Soldier display height as a fraction of tank (lower = infantry looks smaller vs armor).</summary>
    private const float SoldierToTankDisplayRatio = 0.50f / 1.5f;
    private const float SoldierModelTargetHeight = TankModelTargetHeight * SoldierToTankDisplayRatio;
    /// <summary>直升机显示高度：与坦克同量级后再放大，避免旋翼包围盒把机身缩得过小。</summary>
    private const float AircraftModelTargetHeight = TankModelTargetHeight * TankHarmonizeDisplayBoost * AircraftDisplayScaleBoost;
    private const float AircraftDefaultAltitude = SharedAirUnitDefaultAltitude;
    private const float AircraftVisualScale = 0.95f;
    /// <summary>低模直升机 FBX 机身沿 Y 竖起时的兜底绑定（写实机用机身包围盒自动对齐）。</summary>
    private const float AircraftBindPitch = -90f;
    private const float AircraftBindYaw = 0f;
    private const float AircraftBindRoll = 0f;
    private const float AircraftEngagementYawOffset = 0f;
    /// <summary>直升机炸弹水平速度（逻辑单位/秒）；较慢以便看清下落过程。</summary>
    private const float AircraftBombProjectileSpeed = 165f;
    private const float AircraftBombMinFlightSeconds = 1.35f;
    private const float AircraftBombHoverJitterX = 12f;
    private const float AircraftBombHoverJitterZ = 8f;
    private const float AircraftBombDropRadius = 42f;
    private const float AircraftBombDropTrailScale = 0.32f;
    private static readonly Color AircraftBombVisualColor = new Color(0.42f, 0.44f, 0.38f, 1f);
    private const string SoldierResourceModelPath = "Soldiers/USArmyTacticalVanguard/USArmySoldier";
    private const string SoldierResourceFolderPath = "Soldiers/USArmyTacticalVanguard";
    private const string SoldierAlternateResourceModelPath = "Quaternius/ZombieApocalypse/Characters_Sam_SingleWeapon";
    private const string SoldierAlternateResourceFolderPath = "Quaternius/ZombieApocalypse";
    private const string SoldierM14ResourceModelPath = "Weapons/M14Rifle/M14Rifle";
    private const float SoldierM14TargetLength = 0.88f;
    /// <summary>Mixamo Vanguard mesh forward is opposite game heading after baked root rotation.</summary>
    private const float SoldierVanguardYawOffset = 180f;
    /// <summary>Pixelhouse 导入朝向比 body heading 多 90°（朝屏幕下），左转 90° 后对人族。</summary>
    private const float GiantPixelhouseMeshYawOffset = -90f;
    private const float GiantKenneyMeshYawOffset = 180f;
    private const string RealisticTankFolderPath = "RealisticTanks";
    private const string RealisticTankT55AResourcePath = RealisticTankFolderPath + "/T55A";
    private const string RealisticTankT55AkResourcePath = RealisticTankFolderPath + "/T55AK";
    private const string TankResourceFolderPath = "Quaternius/AnimatedTankPack";
    private const string TankResourceModelPath = TankResourceFolderPath + "/TankA";
    private const string TankScoutResourceModelPath = TankResourceFolderPath + "/TankB";
    private const string TankAssaultResourceModelPath = TankResourceFolderPath + "/TankC";
    private const string TankHeavyResourceModelPath = TankResourceFolderPath + "/TankD";
    private const string RealisticAircraftFolderPath = "RealisticAircraft";
    private const string RealisticAircraftResourcePath = RealisticAircraftFolderPath + "/BlackHawk";
    private const string RealisticAircraftSketchfabResourcePath = RealisticAircraftFolderPath + "/BlackHawkSketchfab";
    /// <summary>人族直升机机身贴图：蓝闪电迷彩，与绿色坦克区分。</summary>
    private const string RealisticAircraftBodyTexturePath = RealisticAircraftFolderPath + "/Apache_Texture_BlueLightning";
    private const string RealisticAircraftRotorTexturePath = RealisticAircraftFolderPath + "/Apache_Texture_White";
    private static readonly Color AircraftBodyMaterialTint = new Color(0.78f, 0.86f, 1f, 1f);
    private const float AircraftDisplayScaleBoost = 1.32f;
    private const string AircraftResourceFolderPath = "KumaSousa/LowPolyHelicopter";
    private const string AircraftResourceModelPath = AircraftResourceFolderPath + "/helicopter";
    private const string AircraftDiffuseResourcePath = AircraftResourceFolderPath + "/blend 32";
    private const string GiantRealisticFolderPath = "RealisticZombies";
    private const string GiantPixelhouseResourceModelPath = GiantRealisticFolderPath + "/Pixelhouse/Zombie";
    private const string GiantPixelhouseResourceFolderPath = GiantRealisticFolderPath + "/Pixelhouse";
    private const string GiantResourceFolderPath = "Kenney/ZombieCharacters";
    private const string GiantResourceModelPath = GiantResourceFolderPath + "/Model/characterMedium";
    private const string GiantQuaterniusResourceFolderPath = "Quaternius/ZombieUnits";

    private static readonly string[] GiantResourceModelCandidates =
    {
        GiantPixelhouseResourceModelPath,
        GiantResourceModelPath,
    };
    private const string MedievalVillageResourceFolderPath = "Quaternius/MedievalVillageMegaKit";
    private const string EnvironmentResourceFolderPath = "Environment/Online";
    private const string GrassTextureResourcePath = EnvironmentResourceFolderPath + "/grass_meadow";
    private const string GrassDetailTextureResourcePath = EnvironmentResourceFolderPath + "/grass_detail";
    private const string CoastSandTextureResourcePath = EnvironmentResourceFolderPath + "/coast_sand";
    private const string DaySkyboxResourcePath = EnvironmentResourceFolderPath + "/cape_hill_sunset";

    private static readonly string[] GiantZombieSkinResourcePaths =
    {
        GiantResourceFolderPath + "/Skins/zombieMaleA",
        GiantResourceFolderPath + "/Skins/zombieFemaleA",
    };

    private static readonly string[] GiantQuaterniusResourceVariantPaths =
    {
        GiantQuaterniusResourceFolderPath + "/ZombieA",
        GiantQuaterniusResourceFolderPath + "/ZombieB",
        GiantQuaterniusResourceFolderPath + "/ZombieC",
        GiantQuaterniusResourceFolderPath + "/ZombieD",
    };

    [Header("Unit Settings")]
    [SerializeField] private UnitConfig soldierConfig;
    [SerializeField] private UnitConfig tankConfig;
    [SerializeField] private UnitConfig aircraftConfig;
    [SerializeField] private UnitConfig giantConfig;
    [SerializeField] private DanmuSpawnMappingConfig danmuSpawnMappingConfig;

    [Header("Scene Prefabs")]
    [SerializeField] private GameObject battlefieldPrefab;
    [SerializeField] private ApocalypseHudPrefab hudPrefab;

    private const string DanmuSpawnMappingResourcesPath = "Apocalypse/DanmuSpawnMappingConfig";


    private const float LogicalToWorld = 0.025f;
    private const float Left = -360f;
    private const float Right = 360f;
    private const float Top = 640f;
    private const float Bottom = -640f;
    private const float GiantGroundY = -228f;
    private const float SeparationGridCellSize = 256f;
    private const float TankSeparationMaxPush = 32f;

    private const int HumanFormationLanesPerRow = 4;
    private const int HumanFormationTanksPerRow = 3;
    private static readonly float[] AirLanes =
    {
        80f, 140f, 200f, 260f, 320f, 380f, 440f, 500f, 560f, 620f
    };
    private static readonly HashSet<string> TankDisplayMaterialNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "display",
        "floor",
        "ground",
        "shadow",
        "smoke",
        "logo",
        "default_material",
    };
    private const float RealisticTankBoundsMaxAxis = 5.5f;

    private const int DefaultPortraitScreenWidth = 1170;
    private const int DefaultPortraitScreenHeight = 2532;

    private static readonly ResolutionPreset[] ResolutionPresets =
    {
        new ResolutionPreset("iPhone13", DefaultPortraitScreenWidth, DefaultPortraitScreenHeight),
        new ResolutionPreset("720x1280", 720, 1280),
        new ResolutionPreset("1080x1920", 1080, 1920),
        new ResolutionPreset("1080x2400", 1080, 2400),
        new ResolutionPreset("1440x3200", 1440, 3200),
    };

    private static readonly Color BackgroundColor = new Color(0.09f, 0.08f, 0.10f, 1f);
    private static readonly Color RoadColor = new Color(0.33f, 0.25f, 0.16f, 1f);
    private static readonly Color RuinColor = new Color(0.11f, 0.12f, 0.15f, 1f);
    private static readonly Color HumanColor = new Color(0.24f, 0.64f, 1f, 1f);
    private static readonly Color GiantColor = new Color(1f, 0.36f, 0.28f, 1f);

    private static readonly Dictionary<UnitKind, ModelPose> Poses = new Dictionary<UnitKind, ModelPose>
    {
        { UnitKind.Soldier, new ModelPose(SoldierModelTargetHeight, 0f, 0f, 0f, 0f, true) },
        { UnitKind.Tank, new ModelPose(TankModelTargetHeight, 0f, 0f, 0f, 0f, true) },
        // 直升机轴向绑定在 NormalizePrototype 中写入原型；运行时仅绕 Y 转向（headingDegrees）。
        { UnitKind.Aircraft, new ModelPose(AircraftModelTargetHeight, 0f, 0f, 0f, 0f, true) },
        { UnitKind.Giant, new ModelPose(1.88f, 0f, 0f, 0f, 0f, false) },
        { UnitKind.Fireball, new ModelPose(1.2f, 0f, 0f, 0f, 0f, false) },
        { UnitKind.Smoke, new ModelPose(1.4f, 0f, 0f, 0f, 0f, false) },
    };

    [SerializeField] private float cameraYaw = -14f;
    [SerializeField] private float cameraPitch = 24f;
    [SerializeField] private float cameraDistance = 84f;

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
    private Canvas staticHudCanvas;
    private Canvas dynamicHudCanvas;
    private RectTransform staticHudRoot;
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
    private Image resolutionStrip;
    private Image hpFill;
    private Image humanPowerFill;
    private Image monsterPowerFill;
    private DanmuCommandQueue danmuQueue;

    private readonly Dictionary<UnitKind, GameObject> modelPrototypes = new Dictionary<UnitKind, GameObject>();
    private GameObject soldierM14WeaponPrototype;
    private Material aircraftHelicopterMaterial;
    private Material realisticAircraftBodyMaterial;
    private Material pterosaurGlbBodyMaterial;
    private Texture2D pterosaurGlbBodyAlbedo;
    private Texture2D pterosaurGlbBodyNormal;
    private Material realisticAircraftRotorMaterial;
    private GameObject tankT55AkPrototype;
    private readonly List<GameObject> tankVariantPrototypes = new List<GameObject>();
    private readonly List<GameObject> giantVariantPrototypes = new List<GameObject>();
    private readonly List<BattleUnit> soldiers = new List<BattleUnit>(MaxSoldierCount);
    private readonly List<BattleUnit> tanks = new List<BattleUnit>(MaxTankCount);
    private readonly List<BattleUnit> aircraft = new List<BattleUnit>(MaxAircraftCount);
    private readonly List<BattleUnit> pterosaurs = new List<BattleUnit>(MaxPterosaurCount);
    private readonly List<BattleUnit> giants = new List<BattleUnit>(MaxGiantCount);
    private GameObject pterosaurPrototype;
    private GameObject pterosaurVisibilityFallbackPrototype;
    private GameObject rocketTruckPrototype;
    private int pendingGiantBattleActivation;
    private const int GiantBattleActivationBatchSize = 200;
    private readonly List<BuildingObstacle> buildingObstacles = new List<BuildingObstacle>();
    private readonly List<RoadCorridor> roadCorridors = new List<RoadCorridor>();
    private readonly List<ProjectileView> projectiles = new List<ProjectileView>(MaxProjectiles);
    private readonly List<EffectView> effects = new List<EffectView>(MaxEffects);
    private readonly List<DeathVisual> deathVisuals = new List<DeathVisual>(MaxDeathVisuals);
    private readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
    private readonly Dictionary<string, GameObject> medievalVillagePrefabs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<long, List<BattleUnit>> separationGrid = new Dictionary<long, List<BattleUnit>>();
    private readonly List<List<BattleUnit>> separationGridBuckets = new List<List<BattleUnit>>();
    private readonly List<List<BattleUnit>> separationGridBucketPool = new List<List<BattleUnit>>();
    private TargetingSystem targetingSystem;
    private DamageSystem damageSystem;
    private ProjectileSystem projectileSystem;
    private VisualPoolSystem visualPoolSystem;
    private BattleStateSystem battleStateSystem;
    private UIRuntimeSystem uiRuntimeSystem;
    private UnitSeparationSystem unitSeparationSystem;
    private BuildingAvoidanceSystem buildingAvoidanceSystem;

    private TargetingSystem Targeting => targetingSystem ?? (targetingSystem = new TargetingSystem(this));
    private DamageSystem DamageResolver => damageSystem ?? (damageSystem = new DamageSystem(this));
    private ProjectileSystem ProjectileResolver => projectileSystem ?? (projectileSystem = new ProjectileSystem(this));
    private VisualPoolSystem VisualPools => visualPoolSystem ?? (visualPoolSystem = new VisualPoolSystem(this));
    private BattleStateSystem BattleState => battleStateSystem ?? (battleStateSystem = new BattleStateSystem(this));
    private UIRuntimeSystem UIRuntime => uiRuntimeSystem ?? (uiRuntimeSystem = new UIRuntimeSystem(this));
    private UnitSeparationSystem UnitSeparation => unitSeparationSystem ?? (unitSeparationSystem = new UnitSeparationSystem(this));
    private BuildingAvoidanceSystem BuildingAvoidance => buildingAvoidanceSystem ?? (buildingAvoidanceSystem = new BuildingAvoidanceSystem(this));

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
    private static int DefaultResolutionPresetIndex => 0;
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
    public bool DiagnosticsRealisticTankActive => HasRealisticTankInResources();
    public string DiagnosticsMainTankPrototypeName =>
        modelPrototypes.TryGetValue(UnitKind.Tank, out GameObject mainTank) && mainTank != null
            ? mainTank.name
            : string.Empty;
    public bool DiagnosticsTankUsingFallback =>
        !modelPrototypes.TryGetValue(UnitKind.Tank, out GameObject mainTank)
        || mainTank == null
        || mainTank.name.IndexOf("Fallback", StringComparison.OrdinalIgnoreCase) >= 0;
    public int DiagnosticsTankHelperRigCount => CountTankHelperRigs();
    public float DiagnosticsAverageTankRenderHeight => GetAverageActiveUnitRenderMetric(tanks);
    public float DiagnosticsAverageSoldierRenderHeight => GetAverageActiveUnitRenderMetric(soldiers);
    public float DiagnosticsTankToSoldierHeightRatio
    {
        get
        {
            float soldier = DiagnosticsAverageSoldierRenderHeight;
            return soldier > 0.01f ? DiagnosticsAverageTankRenderHeight / soldier : 0f;
        }
    }
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
    public int DiagnosticsProcessedDanmuCommands => processedDanmuCommandCount;
    public bool DiagnosticsHudUsesPrefab { get; private set; }
    public bool DiagnosticsDanmuMappingConfigured => danmuSpawnMappingConfig != null;
    public int DiagnosticsUnitsAttacking => CountUnitsInRuntimeState(UnitRuntimeState.Attacking);
    public int DiagnosticsUnitsMoving => CountUnitsInRuntimeState(UnitRuntimeState.Moving);

    private void Awake()
    {
        EnsureBattleEffectServices();
        danmuQueue = GetComponent<DanmuCommandQueue>();
        EnsureDanmuSpawnMappingConfig();
        EnsureApocalypseConfigs();
        InitApocalypseBases();
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        ApplyDefaultMobilePresentation();
        ApplyPortraitLiveDefaults();
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
            await PrewarmNuclearMissileAsync();
            AttachPrototypesToUnits();
            PrewarmBattlePools();
            assetsReady = true;
            ShowLoading(false);
            matchPhase = MatchPhase.ModeSelect;
            paused = true;
            ResetBattle();
            ShowBanner("末日之王 — Enter 开战 | 1/2/3 加入阵营", false, 5f);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            DiagnosticsUsingFallback = true;
            assetsReady = true;
            ShowLoading(false);
            AttachFallbackPrototypes();
            await PrewarmNuclearMissileAsync();
            PrewarmBattlePools();
            matchPhase = MatchPhase.ModeSelect;
            paused = true;
            ResetBattle();
            ShowBanner("Loaded with fallback geometry — Enter 开战", true, 4f);
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

        UpdateBattlefieldEnvironment(battleTime);

        if (pendingGiantBattleActivation > 0)
        {
            ProcessPendingGiantBattleActivation();
        }

        if (Input.GetKeyDown(KeyCode.P) && !ended)
        {
            paused = !paused;
            ShowBanner(paused ? "Paused" : "Resumed", false, 1f);
        }

        HandleCameraPresetInput();

        if (Input.GetKeyDown(KeyCode.R))
        {
            BeginApocalypseBattle();
        }

        HandleMatchPhaseInput();
        UpdateCameraShake(Time.deltaTime);
        EnqueueLocalDanmuShortcuts();
        ProcessDanmuCommands();

        if (matchPhase == MatchPhase.ModeSelect)
        {
            TickGiftFeedDisplay(Time.deltaTime);
            UpdatePterosaurs(0f);
            RefreshHud();
            return;
        }

        if (paused || ended)
        {
            UpdatePterosaurs(0f);
            RefreshHud();
            return;
        }

        float dt = Mathf.Min(Time.deltaTime, 0.045f);
        battleTime += dt;
        TickGiftFeedDisplay(dt);
        UpdateApocalypseMatch(dt);
        DrainPendingSpawns();
        UpdateHumans(dt);
        UpdateGiants(dt);
        UpdatePterosaurs(dt);
        ResolveUnitOverlaps();
        UpdateProjectiles(dt);
        UpdateEffects(dt);
        UpdateDeathVisuals(dt);
        CheckBattleEnd();
        RefreshHud();
    }

    private void OnDestroy()
    {
        DisposeUnitAnimators(soldiers);
        DisposeUnitAnimators(tanks);
        DisposeUnitAnimators(aircraft);
        DisposeUnitAnimators(pterosaurs);
        DisposeUnitAnimators(giants);
        ClearRuntimeMaterialCache();
    }

    private void ClearRuntimeMaterialCache()
    {
        foreach (KeyValuePair<string, Material> entry in materialCache)
        {
            SafeDestroyUnityObject(entry.Value);
        }

        materialCache.Clear();
    }

    private static void SafeDestroyUnityObject(UnityEngine.Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
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

    private void CreateCoreScene()
    {
        CreateEventSystemIfNeeded();

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        mainCamera = cameraObject.AddComponent<Camera>();
        if (cameraObject.GetComponent<AudioListener>() == null)
        {
            cameraObject.AddComponent<AudioListener>();
        }

        mainCamera.clearFlags = CameraClearFlags.Skybox;
        mainCamera.backgroundColor = new Color(0.74f, 0.84f, 0.92f, 1f);
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 320f;
        mainCamera.fieldOfView = 31f;
        mainCamera.transform.position = new Vector3(0f, 24f, -22f);
        mainCamera.transform.rotation = Quaternion.Euler(26f, -12f, 0f);

        orbitCamera = cameraObject.AddComponent<OrbitTouchCamera>();
        orbitCamera.yaw = Mathf.Approximately(cameraYaw, 0f) ? -12f : cameraYaw;
        orbitCamera.pitch = Mathf.Approximately(cameraPitch, 18f) ? 26f : cameraPitch;
        orbitCamera.distance = Mathf.Approximately(cameraDistance, 88f) ? 92f : cameraDistance;
        orbitCamera.minPitch = 16f;
        orbitCamera.maxPitch = 68f;
        orbitCamera.minDistance = 26f;
        orbitCamera.maxDistance = 132f;
        orbitCamera.mouseZoomSensitivity = 3.6f;
        orbitCamera.clampPanX = false;
        orbitCamera.clampPanZ = true;
        orbitCamera.panZBounds = new Vector2(-18f, 18f);

        cameraTarget = new GameObject("Camera Target").transform;
        cameraTarget.position = new Vector3(0.2f, 1.1f, -2.4f);
        orbitCamera.target = cameraTarget;

        var lightObject = new GameObject("Sun Light");
        lightObject.transform.rotation = Quaternion.Euler(52f, -58f, 0f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.98f, 0.92f, 1f);
        light.intensity = 1.28f;
        light.shadows = LightShadows.Soft;
        RegisterSunLight(light, lightObject.transform);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.60f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.74f, 0.84f, 0.92f, 1f);
        RenderSettings.fogDensity = 0.0052f;
        ApplyDaySkybox();

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

    private void ApplyDaySkybox()
    {
        Cubemap skyCubemap = Resources.Load<Cubemap>(DaySkyboxResourcePath);
        Texture skyTexture = skyCubemap != null ? skyCubemap : Resources.Load<Texture>(DaySkyboxResourcePath);
        Shader skyShader = skyCubemap != null
            ? Shader.Find("Skybox/Cubemap") ?? Shader.Find("Skybox/Panoramic")
            : Shader.Find("Skybox/Panoramic") ?? Shader.Find("Skybox/Cubemap");
        if (skyTexture == null || skyShader == null)
        {
            return;
        }

        var skybox = new Material(skyShader);
        if (skybox.HasProperty("_Tex"))
        {
            skybox.SetTexture("_Tex", skyTexture);
        }

        if (skybox.HasProperty("_MainTex"))
        {
            skybox.SetTexture("_MainTex", skyTexture);
        }

        if (skybox.HasProperty("_Tint"))
        {
            skybox.SetColor("_Tint", new Color(0.90f, 0.95f, 1f, 1f));
        }

        if (skybox.HasProperty("_Exposure"))
        {
            skybox.SetFloat("_Exposure", 1.08f);
        }

        if (skybox.HasProperty("_Rotation"))
        {
            skybox.SetFloat("_Rotation", 42f);
        }

        RenderSettings.skybox = skybox;
        runtimeSkyboxMaterial = skybox;
    }

    private void CreateHud()
    {
        uiFont = ApocalypseUiFonts.GetBuiltinUiFont();
        if (TryCreateHudFromPrefab())
        {
            return;
        }

        staticHudCanvas = CreateHudCanvas("HUD_Static", 0, ShowResolutionDebugControls);
        dynamicHudCanvas = CreateHudCanvas("HUD_Dynamic", 1, true);
        canvas = dynamicHudCanvas;

        var staticHudRootObject = new GameObject("StaticHudRoot", typeof(RectTransform));
        staticHudRootObject.transform.SetParent(staticHudCanvas.transform, false);
        staticHudRoot = staticHudRootObject.GetComponent<RectTransform>();

        var hudRootObject = new GameObject("HudRoot", typeof(RectTransform));
        hudRootObject.transform.SetParent(dynamicHudCanvas.transform, false);
        hudRoot = hudRootObject.GetComponent<RectTransform>();
        ApplySafeArea();

        var topPanel = CreatePanel(staticHudRoot, "TopPanel", new Color(0.03f, 0.035f, 0.045f, 0.88f));
        SetAnchors(topPanel.rectTransform, 0.035f, 0.89f, 0.965f, 0.992f);

        var topDynamicRoot = CreateRectRoot(hudRoot, "TopDynamicRoot");
        SetAnchors(topDynamicRoot, 0.035f, 0.89f, 0.965f, 0.992f);

        leftTeamLabel = CreateText(topDynamicRoot, "LeftTeamLabel", "BLUE FORCE", 15, HumanColor, TextAnchor.MiddleLeft);
        SetAnchors(leftTeamLabel.rectTransform, 0.03f, 0.66f, 0.25f, 0.93f);
        ConfigureTextFit(leftTeamLabel, 10, 15);

        rightTeamLabel = CreateText(topDynamicRoot, "RightTeamLabel", "丧尸", 15, GiantColor, TextAnchor.MiddleRight);
        SetAnchors(rightTeamLabel.rectTransform, 0.75f, 0.66f, 0.97f, 0.93f);
        ConfigureTextFit(rightTeamLabel, 10, 15);

        battlePhaseLabel = CreateText(topDynamicRoot, "BattlePhaseLabel", "LIVE BARRAGE WAR", 12, new Color(0.78f, 0.82f, 0.86f, 1f), TextAnchor.MiddleCenter);
        SetAnchors(battlePhaseLabel.rectTransform, 0.30f, 0.72f, 0.70f, 0.94f);
        ConfigureTextFit(battlePhaseLabel, 9, 12);

        poolLabel = CreateText(topDynamicRoot, "PoolLabel", "POINT POOL 000,000", 24, new Color(1f, 0.85f, 0.34f, 1f), TextAnchor.MiddleCenter);
        SetAnchors(poolLabel.rectTransform, 0.24f, 0.42f, 0.76f, 0.80f);
        ConfigureTextFit(poolLabel, 16, 24);

        timerLabel = CreateText(topDynamicRoot, "TimerLabel", "03:00", 18, Color.white, TextAnchor.MiddleCenter);
        SetAnchors(timerLabel.rectTransform, 0.40f, 0.18f, 0.60f, 0.46f);
        ConfigureTextFit(timerLabel, 13, 18);

        var humanPowerBack = CreatePanel(topPanel.transform, "HumanPowerBack", new Color(0.06f, 0.12f, 0.18f, 1f));
        SetAnchors(humanPowerBack.rectTransform, 0.03f, 0.03f, 0.47f, 0.17f);

        humanPowerFill = CreatePanel(topDynamicRoot, "HumanPowerFill", new Color(0.24f, 0.70f, 1f, 1f));
        humanPowerFill.type = Image.Type.Filled;
        humanPowerFill.fillMethod = Image.FillMethod.Horizontal;
        humanPowerFill.fillOrigin = 0;
        SetAnchors(humanPowerFill.rectTransform, 0.03f, 0.03f, 0.47f, 0.17f);

        var monsterPowerBack = CreatePanel(topPanel.transform, "MonsterPowerBack", new Color(0.18f, 0.08f, 0.07f, 1f));
        SetAnchors(monsterPowerBack.rectTransform, 0.53f, 0.03f, 0.97f, 0.17f);

        monsterPowerFill = CreatePanel(topDynamicRoot, "MonsterPowerFill", GiantColor);
        monsterPowerFill.type = Image.Type.Filled;
        monsterPowerFill.fillMethod = Image.FillMethod.Horizontal;
        monsterPowerFill.fillOrigin = 1;
        SetAnchors(monsterPowerFill.rectTransform, 0.53f, 0.03f, 0.97f, 0.17f);
        hpFill = monsterPowerFill;

        humanLabel = CreateText(topDynamicRoot, "HumanLabel", "Force 0/0", 12, Color.white, TextAnchor.MiddleLeft);
        SetAnchors(humanLabel.rectTransform, 0.03f, 0.18f, 0.34f, 0.36f);
        ConfigureTextFit(humanLabel, 9, 12);

        giantLabel = CreateText(topDynamicRoot, "GiantLabel", "丧尸 HP 0", 12, Color.white, TextAnchor.MiddleRight);
        SetAnchors(giantLabel.rectTransform, 0.66f, 0.18f, 0.97f, 0.36f);
        ConfigureTextFit(giantLabel, 9, 12);

        var bottomPanel = CreatePanel(staticHudRoot, "LiveBottomPanel", new Color(0.025f, 0.03f, 0.04f, 0.84f));
        SetAnchors(bottomPanel.rectTransform, 0.035f, 0.050f, 0.965f, 0.158f);

        var bottomDynamicRoot = CreateRectRoot(hudRoot, "BottomDynamicRoot");
        SetAnchors(bottomDynamicRoot, 0.035f, 0.050f, 0.965f, 0.158f);

        bottomTickerLabel = CreateText(bottomDynamicRoot, "BottomTickerLabel", "Barrage connected", 14, new Color(0.94f, 0.97f, 1f, 1f), TextAnchor.MiddleLeft);
        SetAnchors(bottomTickerLabel.rectTransform, 0.03f, 0.56f, 0.72f, 0.88f);
        ConfigureTextFit(bottomTickerLabel, 10, 14);

        giftFeedLabel = CreateText(bottomDynamicRoot, "GiftFeedLabel", "Gift heat 0", 13, new Color(1f, 0.83f, 0.38f, 1f), TextAnchor.MiddleLeft);
        SetAnchors(giftFeedLabel.rectTransform, 0.03f, 0.24f, 0.62f, 0.56f);
        ConfigureTextFit(giftFeedLabel, 9, 13);

        statusLabel = CreateText(bottomDynamicRoot, "StatusLabel", "Ready", 12, new Color(0.70f, 0.78f, 0.84f, 1f), TextAnchor.MiddleLeft);
        SetAnchors(statusLabel.rectTransform, 0.03f, 0.04f, 0.62f, 0.26f);
        ConfigureTextFit(statusLabel, 8, 12);

        skillCountdownLabel = CreateText(bottomDynamicRoot, "SkillCountdownLabel", "Skill CD 00s", 14, new Color(0.78f, 1f, 0.82f, 1f), TextAnchor.MiddleRight);
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
        EnsureModeSelectUi();
        EnsureTripleBaseHpBars();
        RefreshModeSelectUi();
    }

    private void CreateBattlefield()
    {
        buildingObstacles.Clear();
        roadCorridors.Clear();
        if (TryCreateBattlefieldFromPrefab())
        {
            return;
        }

        CreateGround();
        CreateCombatZoneRoads();
        CreateFactionCastles();
    }

    /// <summary>Only open roads in the combat lane — no village houses or fences.</summary>
    private void CreateCombatZoneRoads()
    {
        Material roadMaterial = GetTexturedOpaqueMaterial(CoastSandTextureResourcePath, new Color(0.55f, 0.42f, 0.25f, 1f), new Vector2(5f, 1.3f), 0.04f);

        CreateBattlefieldPlane("Battle_MainRoad", ToWorldPoint(0f, -120f, 0.032f), new Vector2(22f, 3.4f), roadMaterial, -3f);
        AddRoadCorridor("MainStreet", 0f, -120f, 440f, 70f, 0f);
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

    private GameObject CreateBattlefieldPlane(string name, Vector3 position, Vector2 size, Material material, float yawDegrees = 0f)
    {
        var plane = CreatePrimitive(PrimitiveType.Plane, name, decorRoot);
        plane.transform.localScale = new Vector3(size.x / 10f, 1f, size.y / 10f);
        plane.transform.localPosition = position;
        plane.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);

        var renderer = plane.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = true;
        return plane;
    }

    private void CreateVillagePaths()
    {
        Material roadMaterial = GetTexturedOpaqueMaterial(CoastSandTextureResourcePath, new Color(0.55f, 0.42f, 0.25f, 1f), new Vector2(5f, 1.3f), 0.04f);

        CreateBattlefieldPlane("VillageDirtRoad_MainStreet", ToWorldPoint(0f, -184f, 0.032f), new Vector2(21.4f, 3.7f), roadMaterial, -3f);
        AddRoadCorridor("MainStreet", 0f, -184f, 430f, 78f, 0f);

        CreateBattlefieldPlane("VillageDirtRoad_HeavyTrack", ToWorldPoint(-120f, -486f, 0.034f), new Vector2(13.4f, 2.45f), roadMaterial, 2f);
        AddRoadCorridor("HeavyTrack", -120f, -486f, 270f, 58f, -80f);

        CreateBattlefieldPlane("VillageMarketPlaza", ToWorldPoint(34f, -52f, 0.036f), new Vector2(6.8f, 5.2f), roadMaterial, 4f);
        AddRoadCorridor("MarketPlaza", 34f, -52f, 138f, 112f, -18f);

        CreateBattlefieldPlane("VillageDirtRoad_CrossStreet", ToWorldPoint(58f, 92f, 0.038f), new Vector2(4.2f, 17.7f), roadMaterial, 8f);
        AddRoadCorridor("CrossStreet", 58f, 92f, 84f, 356f, 12f);

        CreateBattlefieldPlane("VillageDirtRoad_MonsterGate", ToWorldPoint(282f, -250f, 0.040f), new Vector2(5.8f, 3.1f), roadMaterial, -8f);
        AddRoadCorridor("MonsterGate", 282f, -250f, 116f, 70f, -48f);
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
        CreateVillageCottage("VillageForge", -248f, -58f, 92f, 76f, 1.00f, new Color(0.50f, 0.36f, 0.23f, 1f), new Color(0.24f, 0.12f, 0.08f, 1f), -9f);
        CreateVillageCottage("VillageTavern", -116f, 96f, 108f, 84f, 1.12f, new Color(0.54f, 0.40f, 0.26f, 1f), new Color(0.31f, 0.15f, 0.08f, 1f), 8f);
        CreateVillageCottage("VillageHouseNorth", -86f, 318f, 96f, 78f, 0.96f, new Color(0.57f, 0.43f, 0.29f, 1f), new Color(0.33f, 0.17f, 0.09f, 1f), -12f);
        CreateVillageCottage("VillageStable", 112f, 330f, 132f, 82f, 0.86f, new Color(0.42f, 0.30f, 0.18f, 1f), new Color(0.28f, 0.14f, 0.07f, 1f), 10f);
        CreateVillageCottage("VillageBarn", 196f, 126f, 120f, 94f, 1.10f, new Color(0.49f, 0.30f, 0.18f, 1f), new Color(0.24f, 0.10f, 0.06f, 1f), -10f);
        CreateVillageCottage("VillageGranary", 294f, 298f, 90f, 70f, 1.26f, new Color(0.58f, 0.45f, 0.28f, 1f), new Color(0.35f, 0.20f, 0.10f, 1f), 13f);

        CreateVillageTower("VillageChapel", 222f, -332f, 82f, 112f, 2.05f, new Color(0.55f, 0.50f, 0.42f, 1f), new Color(0.22f, 0.16f, 0.13f, 1f), 4f);
        CreateVillageWell(34f, -52f);
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

        CreateMegaKitHouse("VillageForge", -248f, -58f, 104f, 84f, -9f, false, 2, 2, "Roof_RoundTiles_4x4", 0.50f, true);
        CreateMegaKitHouse("VillageTavern", -116f, 96f, 132f, 92f, 8f, false, 3, 2, "Roof_RoundTiles_6x4", 0.52f, true);
        CreateMegaKitHouse("VillageHouseNorth", -86f, 318f, 108f, 88f, -12f, true, 2, 2, "Roof_RoundTiles_4x4", 0.50f, false);
        CreateMegaKitHouse("VillageStable", 112f, 330f, 148f, 92f, 10f, false, 3, 2, "Roof_RoundTiles_6x4", 0.52f, false);
        CreateMegaKitHouse("VillageBarn", 196f, 126f, 148f, 122f, -10f, true, 3, 3, "Roof_RoundTiles_6x6", 0.54f, true);
        CreateMegaKitHouse("VillageGranary", 294f, 298f, 104f, 84f, 13f, false, 2, 2, "Roof_RoundTiles_4x4", 0.50f, true);
        CreateMegaKitHouse("VillageGatehouse", 294f, -98f, 112f, 86f, -12f, true, 2, 2, "Roof_RoundTiles_4x4", 0.50f, true);
        CreateMegaKitTower("VillageChapel", 222f, -332f, 92f, 112f, 4f, 0.48f);
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

        AddBuildingObstacle(root, name, centerX, centerZ, width * 0.5f, depth * 0.5f, 2.2f, 10f, 170f);
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
        AddBuildingObstacle(root, name, centerX, centerZ, width * 0.5f, depth * 0.5f, 3.8f, 12f, 260f);
    }

    private void CreateMegaKitMarketProps()
    {
        CreateMegaKitPlacedProp("Prop_Wagon", -34f, 28f, -18f, 0.46f);
        CreateMegaKitPlacedProp("Prop_Crate", 18f, 36f, 16f, 0.48f);
        CreateMegaKitPlacedProp("Prop_Crate", 72f, 18f, -9f, 0.48f);
        CreateMegaKitPlacedProp("Balcony_Cross_Straight", -58f, 158f, 7f, 0.38f);
        CreateMegaKitPlacedProp("Prop_MetalFence_Ornament", 118f, 46f, 92f, 0.40f);
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

        AddBuildingObstacle(root, name, centerX, centerZ, width * 0.5f, depth * 0.5f, bodyHeight + 0.6f, 8f, 150f);
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

        AddBuildingObstacle(root, name, centerX, centerZ, width * 0.5f, depth * 0.5f, bodyHeight + 0.8f, 10f, 240f);
    }

    private void CreateVillageWell(float centerX, float centerZ)
    {
        var root = new GameObject("VillageWell");
        root.transform.SetParent(decorRoot, false);
        root.transform.localPosition = ToWorldPoint(centerX, centerZ, 0f);

        var baseRing = CreatePrimitive(PrimitiveType.Cylinder, "VillageWell_StoneRing", root.transform);
        baseRing.transform.localScale = new Vector3(0.54f, 0.16f, 0.54f);
        baseRing.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        baseRing.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.38f, 0.36f, 0.31f, 1f));

        var beam = CreatePrimitive(PrimitiveType.Cube, "VillageWell_CrossBeam", root.transform);
        beam.transform.localScale = new Vector3(1.12f, 0.10f, 0.12f);
        beam.transform.localPosition = new Vector3(0f, 0.78f, 0f);
        beam.transform.localRotation = Quaternion.Euler(0f, -16f, 0f);
        beam.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.30f, 0.17f, 0.08f, 1f));

        AddBuildingObstacle(root, "VillageWell", centerX, centerZ, 28f, 28f, 0.9f, 6f, 90f);
    }

    private void CreateMarketStalls()
    {
        Color clothBlue = new Color(0.20f, 0.38f, 0.56f, 1f);
        Color clothRed = new Color(0.55f, 0.20f, 0.15f, 1f);
        for (int i = 0; i < 4; i++)
        {
            float x = -44f + i * 36f;
            float z = 24f + (i % 2) * 40f;
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
        AddBuildingObstacle(null, name, centerX, centerZ, halfX, halfZ, height, padding, 160f);
    }

    private void AddBuildingObstacle(GameObject root, string name, float centerX, float centerZ, float halfX, float halfZ, float height, float padding, float hp)
    {
        buildingObstacles.Add(new BuildingObstacle(root, name, centerX, centerZ, halfX, halfZ, height, padding, hp));
    }

    private void AddRoadCorridor(string name, float centerX, float centerZ, float halfX, float halfZ, float priority)
    {
        roadCorridors.Add(new RoadCorridor(name, centerX, centerZ, halfX, halfZ, priority));
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
        Material blueZone = GetTransparentMaterial(new Color(0.10f, 0.38f, 0.58f, 0.44f));
        Material blueMarkerMaterial = GetTransparentMaterial(new Color(0.20f, 0.68f, 0.92f, 0.50f));
        Material redZone = GetTransparentMaterial(new Color(0.55f, 0.20f, 0.12f, 0.42f));
        Material redMarkerMaterial = GetTransparentMaterial(new Color(0.92f, 0.36f, 0.18f, 0.52f));

        CreateBattlefieldPlane("BlueFrontlineBase", new Vector3(-8.25f, 0.046f, -6.8f), new Vector2(2.4f, 6.4f), blueZone, -7f);
        CreateBattlefieldPlane("BlueFrontlineMarker", new Vector3(-9.2f, 0.052f, -5.1f), new Vector2(0.58f, 0.52f), blueMarkerMaterial, 15f);

        CreateBattlefieldPlane("RedFrontlineBase", new Vector3(8.25f, 0.046f, -6.8f), new Vector2(2.4f, 6.4f), redZone, 11f);
        CreateBattlefieldPlane("RedFrontlineMarker", new Vector3(9.2f, 0.052f, -4.9f), new Vector2(0.64f, 0.54f), redMarkerMaterial, -12f);

        for (int i = 0; i < 3; i++)
        {
            float z = -11.2f + i * 4.6f;
            CreateBattlefieldPlane($"BlueFrontlineMark_{i}", new Vector3(-6.9f + Noise(i + 211f) * 0.45f, 0.058f, z), new Vector2(1.15f, 0.74f), blueMarkerMaterial, -10f + i * 6f);
            CreateBattlefieldPlane($"RedFrontlineMark_{i}", new Vector3(6.9f - Noise(i + 257f) * 0.45f, 0.058f, z + 0.4f), new Vector2(1.15f, 0.74f), redMarkerMaterial, 10f - i * 6f);
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

        for (int i = 0; i < MaxSoldierCount; i++)
        {
            soldiers.Add(CreateUnitShell(UnitKind.Soldier));
        }

        for (int i = 0; i < MaxTankCount; i++)
        {
            TankModelVariant tankModel = i < TankT55ACount || (i >= TankCount && i % 2 == 0) ? TankModelVariant.T55A : TankModelVariant.T55AK;
            tanks.Add(CreateUnitShell(UnitKind.Tank, tankModel));
        }

        for (int i = 0; i < MaxAircraftCount; i++)
        {
            aircraft.Add(CreateUnitShell(UnitKind.Aircraft));
        }

        for (int i = 0; i < MaxPterosaurCount; i++)
        {
            var pterosaur = CreateUnitShell(UnitKind.Aircraft);
            pterosaur.team = TeamKind.Giant;
            pterosaur.faction = FactionId.Zombie;
            pterosaur.combatVariant = UnitCombatVariant.Pterosaur;
            pterosaurs.Add(pterosaur);
        }

        for (int i = 0; i < MaxGiantCount; i++)
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
            soldierConfig.MoveSpeed = SoldierDefaultMoveSpeed;
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
            tankConfig.MoveSpeed = TankDefaultMoveSpeed;
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
            aircraftConfig.MoveSpeed = AircraftDefaultMoveSpeed;
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
            giantConfig.MoveSpeed = 42f;
            giantConfig.Radius = 82f;
            giantConfig.AttackRange = 126f;
            giantConfig.AttackInterval = 1.12f;
        }
    }

    private void EnsureDanmuSpawnMappingConfig()
    {
        if (danmuSpawnMappingConfig == null)
        {
            danmuSpawnMappingConfig = Resources.Load<DanmuSpawnMappingConfig>(DanmuSpawnMappingResourcesPath);
        }

        DanmuCommandParser.ConfigureHumanSpawnMappings(
            danmuSpawnMappingConfig != null
                ? danmuSpawnMappingConfig.HumanSpawnMappings
                : DanmuSpawnMapping.CreateDefaultHumanMappings());
    }

    private BattleUnit CreateUnitShell(UnitKind kind, TankModelVariant tankModel = TankModelVariant.None)
    {
        var root = new GameObject($"Unit_{kind}_{nextId}");
        root.transform.SetParent(unitRoot, false);

        var shadow = CreatePrimitive(PrimitiveType.Cylinder, "Shadow", root.transform);
        shadow.transform.localScale = new Vector3(kind == UnitKind.Aircraft ? 1.08f : 1.05f, 0.03f, kind == UnitKind.Aircraft ? 1.08f : 1.35f);
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
            combatVariant = UnitCombatVariant.Standard,
            tankModel = tankModel,
            team = kind == UnitKind.Giant ? TeamKind.Giant : TeamKind.Human,
            root = root,
            body = body.transform,
            active = false,
            x = 0f,
            z = 0f,
            visualX = 0f,
            visualZ = 0f,
            baseZ = 0f,
            altitude = kind == UnitKind.Aircraft ? AircraftDefaultAltitude : 0f,
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
            baseModelLocalRotation = Quaternion.identity,
            soldierUsesVanguardMesh = false,
            tankAimRoot = tankAimRoot,
            tankTurretVisual = tankTurretVisual,
            tankBarrelVisual = tankBarrelVisual,
            tankMuzzleVisual = tankMuzzleVisual,
            soldierMuzzleVisual = null,
        };
    }

    private static float TankModelYawOffset(TankModelVariant tankModel)
    {
        if (!HasRealisticTankInResources())
        {
            return TankLowPolyYawOffset;
        }

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
            ResolveAircraftModelPath(),
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
                if (kinds[i] == UnitKind.Giant)
                {
                    Debug.LogError("[ApocalypseKing] 丧尸模型未加载。请运行 .\\tools\\import-zombie-units.ps1 后重新导入 Unity。");
                }
                else
                {
                    DiagnosticsUsingFallback = true;
                    prototype = CreateFallbackPrototype(kinds[i]);
                }
            }

            if (prototype != null)
            {
                modelPrototypes[kinds[i]] = prototype;
            }
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
        LoadSpecialUnitPrototypes();
    }

    private string ResolveSoldierModelPath()
    {
        string[] candidates =
        {
            "Soldiers/us_army_soldier.glb",
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
            "RealisticTanks/T55A.glb",
            "RealisticTanks/T55AK.glb",
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

    private string ResolveAircraftModelPath()
    {
        string[] candidates =
        {
            "RealisticAircraft/BlackHawk.fbx",
            "RealisticAircraft/BlackHawkSketchfab.glb",
            "PolyPizza/helicopter.glb",
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

    private string ResolveGiantModelPath()
    {
        string[] candidates =
        {
            "RealisticZombies/Pixelhouse/Zombie.fbx",
            "Quaternius/ZombieUnits/ZombieA.glb",
            "Kenney/ZombieCharacters/Model/characterMedium.fbx",
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
            var realisticTankPrototype = LoadRealisticTankResourcePrototype(tankModel);
            if (realisticTankPrototype != null)
            {
                return realisticTankPrototype;
            }

            var tankResourcePrototype = LoadTankResourcePrototype(tankModel);
            if (tankResourcePrototype != null)
            {
                return tankResourcePrototype;
            }
        }
        else if (kind == UnitKind.Aircraft)
        {
            var aircraftResourcePrototype = LoadAircraftResourcePrototype();
            if (aircraftResourcePrototype != null)
            {
                return aircraftResourcePrototype;
            }
        }
        else if (kind == UnitKind.Giant)
        {
            for (int i = 0; i < GiantResourceModelCandidates.Length; i++)
            {
                string resourcePath = GiantResourceModelCandidates[i];
                string clipFolder = ResolveGiantAnimationClipFolder(resourcePath);
                string skinPath = resourcePath == GiantResourceModelPath ? GiantZombieSkinResourcePaths[0] : null;
                var giantResourcePrototype = TryLoadGiantResourcePrototype(resourcePath, clipFolder, skinPath);
                if (giantResourcePrototype != null)
                {
                    return giantResourcePrototype;
                }
            }

            for (int i = 0; i < GiantQuaterniusResourceVariantPaths.Length; i++)
            {
                var giantResourcePrototype = TryLoadGiantResourcePrototype(
                    GiantQuaterniusResourceVariantPaths[i],
                    GiantQuaterniusResourceFolderPath,
                    null);
                if (giantResourcePrototype != null)
                {
                    return giantResourcePrototype;
                }
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
        var prototype = TryLoadSoldierResourcePrototype(SoldierResourceModelPath, SoldierResourceFolderPath);
        if (prototype != null)
        {
            return prototype;
        }

        return TryLoadSoldierResourcePrototype(SoldierAlternateResourceModelPath, SoldierAlternateResourceFolderPath);
    }

    private GameObject TryLoadSoldierResourcePrototype(string resourceModelPath, string resourceFolderPath)
    {
        var source = Resources.Load<GameObject>(resourceModelPath);
        if (source == null)
        {
            return null;
        }

        var prototype = Instantiate(source, modelCacheRoot, false);
        prototype.name = $"{UnitKind.Soldier}_Prototype";
        prototype.hideFlags = HideFlags.HideInHierarchy;
        AttachResourceAnimationClips(prototype, resourceModelPath, resourceFolderPath);
        if (!SoldierPrototypeIsUsable(prototype))
        {
            Destroy(prototype);
            return null;
        }

        ConfigureImportedPrototype(prototype, UnitKind.Soldier);
        prototype.SetActive(false);
        return prototype;
    }

    private static bool SoldierPrototypeIsUsable(GameObject prototype)
    {
        if (prototype == null)
        {
            return false;
        }

        if (prototype.GetComponentsInChildren<Renderer>(true).Length == 0)
        {
            return false;
        }

        AnimationClip[] clips = CollectRuntimeAnimationClips(prototype);
        if (clips.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            string name = clip.name;
            if (name.IndexOf("Run", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Idle", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Forward", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return true;
    }

    private static bool HasRealisticTankInResources()
    {
        return LoadTankResourceModelRoot(RealisticTankT55AResourcePath) != null
            || LoadTankResourceModelRoot(RealisticTankT55AkResourcePath) != null;
    }

    private GameObject LoadRealisticTankResourcePrototype(TankModelVariant tankModel)
    {
        string resourcePath = tankModel == TankModelVariant.T55AK
            ? RealisticTankT55AkResourcePath
            : RealisticTankT55AResourcePath;
        var prototype = LoadTankResourcePrototype(resourcePath);
        if (prototype != null)
        {
            prototype.name = $"{UnitKind.Tank}_Realistic_{tankModel}";
            return prototype;
        }

        if (tankModel == TankModelVariant.T55AK)
        {
            return LoadTankResourcePrototype(RealisticTankT55AResourcePath);
        }

        return null;
    }

    private GameObject LoadTankResourcePrototype(TankModelVariant tankModel)
    {
        if (HasRealisticTankInResources())
        {
            return LoadRealisticTankResourcePrototype(tankModel);
        }

        string resourcePath = tankModel == TankModelVariant.T55AK ? TankHeavyResourceModelPath : TankResourceModelPath;
        return LoadTankResourcePrototype(resourcePath);
    }

    private GameObject LoadTankResourcePrototype(string resourcePath)
    {
        bool realistic = resourcePath.StartsWith(RealisticTankFolderPath, StringComparison.Ordinal);
        var source = LoadTankResourceModelRoot(resourcePath);
        if (source == null)
        {
            return null;
        }

        var prototype = Instantiate(source, modelCacheRoot, false);
        prototype.name = realistic ? $"{UnitKind.Tank}_Realistic_Prototype" : $"{UnitKind.Tank}_Prototype";
        prototype.hideFlags = HideFlags.HideInHierarchy;
        string clipFolder = realistic ? RealisticTankFolderPath : TankResourceFolderPath;
        AttachResourceAnimationClips(prototype, resourcePath, clipFolder);
        ConfigureImportedPrototype(prototype, UnitKind.Tank);
        prototype.SetActive(false);
        return prototype;
    }

    private static GameObject LoadTankResourceModelRoot(string resourcePath)
    {
        var source = Resources.Load<GameObject>(resourcePath);
        if (source != null)
        {
            return source;
        }

        var assets = Resources.LoadAll(resourcePath, typeof(GameObject));
        GameObject best = null;
        int bestRendererCount = 0;
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is not GameObject gameObject)
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

    private static bool HasRealisticAircraftInResources()
    {
        return LoadRealisticAircraftModelRoot() != null;
    }

    private static GameObject LoadRealisticAircraftModelRoot()
    {
        string[] candidates =
        {
            RealisticAircraftResourcePath,
            RealisticAircraftSketchfabResourcePath,
        };

        GameObject best = null;
        int bestScore = -1;
        for (int i = 0; i < candidates.Length; i++)
        {
            GameObject root = LoadTankResourceModelRoot(candidates[i]);
            if (!AircraftModelRootLooksValid(root, out int score))
            {
                continue;
            }

            if (score > bestScore)
            {
                best = root;
                bestScore = score;
            }
        }

        return best;
    }

    private static bool AircraftModelRootLooksValid(GameObject root, out int score)
    {
        score = 0;
        if (root == null)
        {
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || IsAircraftStrayRenderer(renderer))
            {
                continue;
            }

            score++;
        }

        if (score <= 0 || !TryComputeModelBounds(root, out Bounds bounds))
        {
            score = 0;
            return false;
        }

        float span = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (span < 0.02f || span > 5000f)
        {
            score = 0;
            return false;
        }

        score += Mathf.RoundToInt(span * 10f);
        return true;
    }

    private static bool AircraftPrototypeLooksRenderable(GameObject prototype)
    {
        return AircraftModelRootLooksValid(prototype, out _);
    }

    private GameObject LoadRealisticAircraftResourcePrototype()
    {
        var source = LoadRealisticAircraftModelRoot();
        if (source == null)
        {
            return null;
        }

        var prototype = Instantiate(source, modelCacheRoot, false);
        prototype.name = $"{UnitKind.Aircraft}_Realistic_Prototype";
        prototype.hideFlags = HideFlags.HideInHierarchy;
        ConfigureImportedPrototype(prototype, UnitKind.Aircraft);
        prototype.SetActive(false);
        return AircraftPrototypeLooksRenderable(prototype) ? prototype : null;
    }

    private GameObject LoadAircraftResourcePrototype()
    {
        GameObject realisticPrototype = LoadRealisticAircraftResourcePrototype();
        if (realisticPrototype != null)
        {
            return realisticPrototype;
        }

        if (LoadRealisticAircraftModelRoot() != null)
        {
            Debug.LogWarning("[ApocalypseKing] Realistic helicopter failed validation; falling back to LowPolyHelicopter.");
        }

        var source = Resources.Load<GameObject>(AircraftResourceModelPath);
        if (source == null)
        {
            return null;
        }

        var prototype = Instantiate(source, modelCacheRoot, false);
        prototype.name = $"{UnitKind.Aircraft}_Prototype";
        prototype.hideFlags = HideFlags.HideInHierarchy;
        AttachResourceAnimationClips(prototype, AircraftResourceModelPath, AircraftResourceFolderPath);
        ConfigureImportedPrototype(prototype, UnitKind.Aircraft);
        prototype.SetActive(false);
        return prototype;
    }

    private static string ResolveGiantAnimationClipFolder(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return GiantResourceFolderPath;
        }

        if (resourcePath.StartsWith(GiantPixelhouseResourceFolderPath, StringComparison.Ordinal))
        {
            return GiantPixelhouseResourceFolderPath;
        }

        if (resourcePath.StartsWith(GiantQuaterniusResourceFolderPath, StringComparison.Ordinal))
        {
            return GiantQuaterniusResourceFolderPath;
        }

        if (resourcePath.StartsWith(GiantResourceFolderPath, StringComparison.Ordinal))
        {
            return GiantResourceFolderPath;
        }

        int slash = resourcePath.LastIndexOf('/');
        return slash > 0 ? resourcePath.Substring(0, slash) : resourcePath;
    }

    private GameObject TryLoadGiantResourcePrototype(string resourcePath, string clipFolder, string skinTexturePath)
    {
        var prototype = LoadGiantResourcePrototype(resourcePath, clipFolder, skinTexturePath);
        if (prototype == null)
        {
            return null;
        }

        if (!IsAcceptableGiantPrototype(prototype, Poses[UnitKind.Giant].TargetHeight))
        {
            Destroy(prototype);
            return null;
        }

        return prototype;
    }

    private GameObject LoadGiantResourcePrototype(string resourcePath, string clipFolder, string skinTexturePath)
    {
        var source = Resources.Load<GameObject>(resourcePath);
        if (source == null)
        {
            return null;
        }

        var prototype = Instantiate(source, modelCacheRoot, false);
        prototype.name = $"{UnitKind.Giant}_Prototype_{SanitizeResourceToken(resourcePath)}";
        prototype.hideFlags = HideFlags.HideInHierarchy;
        AttachGiantResourceAnimationClips(prototype, resourcePath, clipFolder);
        ConfigureImportedPrototype(prototype, UnitKind.Giant);
        if (resourcePath.StartsWith(GiantPixelhouseResourceFolderPath, StringComparison.Ordinal))
        {
            ApplyPixelhouseZombieMaterials(prototype);
        }
        else if (!string.IsNullOrEmpty(skinTexturePath))
        {
            ApplyKenneyZombieSkin(prototype, skinTexturePath);
        }

        prototype.SetActive(false);
        return prototype;
    }

    private static string SanitizeResourceToken(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return "Unknown";
        }

        int slash = resourcePath.LastIndexOf('/');
        return slash >= 0 ? resourcePath.Substring(slash + 1) : resourcePath;
    }

    private static bool IsAcceptableGiantPrototype(GameObject prototype, float targetHeight)
    {
        if (prototype == null || !TryComputeModelBounds(prototype, out Bounds bounds))
        {
            return false;
        }

        float height = Mathf.Max(0.001f, bounds.size.y);
        float maxSpan = Mathf.Max(bounds.size.x, bounds.size.z);
        if (height < targetHeight * 0.35f || height > targetHeight * 2.8f)
        {
            return false;
        }

        if (maxSpan > height * 2.6f)
        {
            return false;
        }

        return HasGiantLocomotionAnimation(prototype);
    }

    private static bool HasGiantLocomotionAnimation(GameObject prototype)
    {
        AnimationClip[] clips = CollectRuntimeAnimationClips(prototype);
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            string name = clip.name;
            if (name.IndexOf("Run", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Fury", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return clips.Length > 0;
    }

    private void ConfigureTankVariantPrototypes()
    {
        tankVariantPrototypes.Clear();

        if (HasRealisticTankInResources())
        {
            AddTankVariantPrototype(LoadRealisticTankResourcePrototype(TankModelVariant.T55A));
            AddTankVariantPrototype(LoadRealisticTankResourcePrototype(TankModelVariant.T55AK));
            HarmonizeTankPrototypeScales();
            return;
        }

        GameObject standardPrototype;
        if (modelPrototypes.TryGetValue(UnitKind.Tank, out standardPrototype))
        {
            AddTankVariantPrototype(standardPrototype);
        }

        AddTankVariantPrototype(LoadTankResourcePrototype(TankResourceModelPath));
        AddTankVariantPrototype(LoadTankResourcePrototype(TankScoutResourceModelPath));
        AddTankVariantPrototype(LoadTankResourcePrototype(TankAssaultResourceModelPath));
        AddTankVariantPrototype(LoadTankResourcePrototype(TankHeavyResourceModelPath));
        AddTankVariantPrototype(tankT55AkPrototype);
        HarmonizeTankPrototypeScales();
    }

    private static float GetTankBoundsMetric(Bounds bounds)
    {
        return Mathf.Max(0.001f, bounds.size.x, bounds.size.z, bounds.size.y);
    }

    private void HarmonizeTankPrototypeScales()
    {
        bool includeTankDisplayGeometry = !HasRealisticTankInResources();
        var prototypes = new List<GameObject>(tankVariantPrototypes.Count + 1);
        for (int i = 0; i < tankVariantPrototypes.Count; i++)
        {
            prototypes.Add(tankVariantPrototypes[i]);
        }

        GameObject mainTankPrototype;
        if (modelPrototypes.TryGetValue(UnitKind.Tank, out mainTankPrototype)
            && mainTankPrototype != null
            && !prototypes.Contains(mainTankPrototype))
        {
            prototypes.Add(mainTankPrototype);
        }

        float minMetric = float.PositiveInfinity;
        for (int i = 0; i < prototypes.Count; i++)
        {
            if (!TryComputeModelBounds(prototypes[i], out Bounds bounds, includeTankDisplayGeometry))
            {
                continue;
            }

            minMetric = Mathf.Min(minMetric, GetTankBoundsMetric(bounds));
        }

        if (minMetric >= float.PositiveInfinity)
        {
            return;
        }

        for (int i = 0; i < prototypes.Count; i++)
        {
            GameObject prototype = prototypes[i];
            if (!TryComputeModelBounds(prototype, out Bounds bounds, includeTankDisplayGeometry))
            {
                continue;
            }

            float metric = GetTankBoundsMetric(bounds);
            float ratio = minMetric / metric;
            if (ratio >= 0.999f)
            {
                continue;
            }

            prototype.transform.localScale *= ratio;
            if (TryComputeModelBounds(prototype, out bounds, includeTankDisplayGeometry))
            {
                prototype.transform.localPosition += new Vector3(0f, -bounds.min.y, 0f);
            }
        }

        if (TankHarmonizeDisplayBoost > 1.001f)
        {
            for (int i = 0; i < prototypes.Count; i++)
            {
                GameObject prototype = prototypes[i];
                prototype.transform.localScale *= TankHarmonizeDisplayBoost;
                if (TryComputeModelBounds(prototype, out Bounds bounds, includeTankDisplayGeometry))
                {
                    prototype.transform.localPosition += new Vector3(0f, -bounds.min.y, 0f);
                }
            }
        }
    }

    private void ConfigureGiantVariantPrototypes()
    {
        giantVariantPrototypes.Clear();

        GameObject standardPrototype;
        if (!modelPrototypes.TryGetValue(UnitKind.Giant, out standardPrototype) || standardPrototype == null)
        {
            return;
        }

        AddGiantVariantPrototype(standardPrototype);

        bool kenneyBase = standardPrototype.name.IndexOf("characterMedium", StringComparison.OrdinalIgnoreCase) >= 0
            || standardPrototype.name.IndexOf("Kenney", StringComparison.OrdinalIgnoreCase) >= 0;
        if (kenneyBase)
        {
            for (int i = 0; i < GiantZombieSkinResourcePaths.Length; i++)
            {
                var variant = Instantiate(standardPrototype, modelCacheRoot, false);
                variant.name = $"{UnitKind.Giant}_KenneySkin_{i}";
                variant.hideFlags = HideFlags.HideInHierarchy;
                ApplyKenneyZombieSkin(variant, GiantZombieSkinResourcePaths[i]);
                variant.SetActive(false);
                AddGiantVariantPrototype(variant);
            }
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
        var clips = CollectResourceAnimationClips(resourceModelPath, resourceFolderPath);
        if (clips.Length == 0)
        {
            return;
        }

        var clipStore = GetOrCreateAnimationClipStore(prototype);
        clipStore.Clips = clips;
        clipStore.AnimatorClips = CreateAnimatorCompatibleClips(clips);
        clipStore.AnimatorReady = clipStore.AnimatorClips.Length > 0;
    }

    private static void AttachGiantResourceAnimationClips(GameObject prototype, string resourceModelPath, string resourceFolderPath)
    {
        var clips = new List<AnimationClip>();
        bool pixelhouse = resourceModelPath.StartsWith(GiantPixelhouseResourceFolderPath, StringComparison.Ordinal);
        AppendEmbeddedModelAnimationClips(clips, prototype, legacyOnly: pixelhouse);
        if (!pixelhouse)
        {
            AppendResourceAnimationClips(clips, resourceModelPath);
            AppendResourceAnimationClips(clips, resourceFolderPath);
        }

        if (clips.Count == 0)
        {
            return;
        }

        var clipStore = GetOrCreateAnimationClipStore(prototype);
        clipStore.Clips = clips.ToArray();
        clipStore.AnimatorClips = pixelhouse ? clipStore.Clips : CreateAnimatorCompatibleClips(clipStore.Clips);
        clipStore.AnimatorReady = clipStore.Clips.Length > 0;
        clipStore.UseLegacyBoneAnimation = pixelhouse;
    }

    private static void AppendEmbeddedModelAnimationClips(List<AnimationClip> clips, GameObject model, bool legacyOnly = false)
    {
        if (model == null)
        {
            return;
        }

        Animation host = FindGiantAnimationHost(model);
        if (host == null)
        {
            return;
        }

        if (host.clip != null)
        {
            AddUniqueGiantAnimationClip(clips, host.clip, legacyOnly);
        }

        foreach (AnimationState state in host)
        {
            if (state != null && state.clip != null)
            {
                AddUniqueGiantAnimationClip(clips, state.clip, legacyOnly);
            }
        }
    }

    private static void AddUniqueGiantAnimationClip(List<AnimationClip> clips, AnimationClip clip, bool legacyOnly)
    {
        if (clip == null || ContainsClip(clips, clip))
        {
            return;
        }

        if (legacyOnly)
        {
            clips.Add(clip);
            return;
        }

        AddUniqueAnimatorCompatibleClip(clips, clip);
    }

    private static Animation FindGiantAnimationHost(GameObject model)
    {
        if (model == null)
        {
            return null;
        }

        Animation best = null;
        int bestScore = -1;
        Animation[] candidates = model.GetComponentsInChildren<Animation>(true);
        for (int i = 0; i < candidates.Length; i++)
        {
            Animation candidate = candidates[i];
            if (candidate == null)
            {
                continue;
            }

            int score = 0;
            if (candidate.clip != null)
            {
                score += 10;
            }

            foreach (AnimationState state in candidate)
            {
                if (state != null && state.clip != null)
                {
                    score++;
                }
            }

            if (score > bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    private static AnimationClip SelectGiantLocomotionClip(AnimationClip[] clips, bool moving)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        string[] preferred = moving
            ? new[] { "walk", "Walk", "ZombieWalk", "Run", "Forward", "fury", "Fury" }
            : new[] { "fury", "Fury", "idle", "Idle", "walk", "Walk" };
        for (int i = 0; i < preferred.Length; i++)
        {
            for (int c = 0; c < clips.Length; c++)
            {
                AnimationClip clip = clips[c];
                if (clip != null && clip.name.IndexOf(preferred[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return clip;
                }
            }
        }

        AnimationClip longest = null;
        float longestLength = 0f;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null || clip.length < 0.2f)
            {
                continue;
            }

            if (clip.length > longestLength)
            {
                longest = clip;
                longestLength = clip.length;
            }
        }

        return longest ?? clips[0];
    }

    private static AnimationClip[] CollectResourceAnimationClips(string resourceModelPath, string resourceFolderPath)
    {
        var clips = new List<AnimationClip>();
        AppendResourceAnimationClips(clips, resourceModelPath);
        AppendResourceAnimationClips(clips, resourceFolderPath);
        return clips.ToArray();
    }

    private static void AppendResourceAnimationClips(List<AnimationClip> clips, string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return;
        }

        var loadedClips = Resources.LoadAll<AnimationClip>(resourcePath);
        if (loadedClips != null)
        {
            for (int i = 0; i < loadedClips.Length; i++)
            {
                AddUniqueAnimatorCompatibleClip(clips, loadedClips[i]);
            }
        }

        var assets = Resources.LoadAll(resourcePath);
        if (assets == null)
        {
            return;
        }

        for (int i = 0; i < assets.Length; i++)
        {
            var clip = assets[i] as AnimationClip;
            if (clip != null)
            {
                AddUniqueAnimatorCompatibleClip(clips, clip);
            }
        }
    }

    private static RuntimeAnimationClipStore GetOrCreateAnimationClipStore(GameObject prototype)
    {
        var clipStore = prototype.GetComponent<RuntimeAnimationClipStore>();
        if (clipStore == null)
        {
            clipStore = prototype.AddComponent<RuntimeAnimationClipStore>();
        }

        return clipStore;
    }

    private static void AttachRuntimeAnimationClips(GameObject prototype, GLTFComponent gltf)
    {
        if (prototype == null || gltf == null || gltf.CreatedAnimationClips == null || gltf.CreatedAnimationClips.Length == 0)
        {
            return;
        }

        var clipStore = GetOrCreateAnimationClipStore(prototype);
        clipStore.Clips = gltf.CreatedAnimationClips;
        clipStore.AnimatorClips = CreateAnimatorCompatibleClips(gltf.CreatedAnimationClips);
        clipStore.AnimatorReady = clipStore.AnimatorClips.Length > 0;
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
        if (source == null)
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
        StripImportedModelStrayComponents(prototype);

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

        bool isRocketTruckPrototype = prototype.name.IndexOf("RocketTruck", StringComparison.OrdinalIgnoreCase) >= 0;
        if (kind == UnitKind.Tank && !isRocketTruckPrototype)
        {
            RemoveTankDisplayGeometry(prototype);
            if (HasRealisticTankInResources())
            {
                ApplyRealisticTankMaterials(prototype);
            }
        }

        if (kind == UnitKind.Giant)
        {
            RemoveGiantStrayGeometry(prototype);
        }

        if (kind == UnitKind.Soldier && !SoldierUsesBuiltInTextures(prototype))
        {
            ApplySoldierMilitaryTint(prototype);
        }

        if (prototype.name.IndexOf("Pterosaur", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            RemoveSketchfabSceneExtras(prototype);
            ApplyPterosaurGltfTextures(prototype, PterosaurPteranodonResourceModelPath);
        }

        if (prototype.name.IndexOf("RocketTruck", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            RemoveSketchfabSceneExtras(prototype);
            ApplyRocketTruckPrototypeBindRotation(prototype);
            ApplyRocketTruckPresentation(prototype);
        }

        if (kind == UnitKind.Aircraft && prototype.name.IndexOf("Pterosaur", StringComparison.OrdinalIgnoreCase) < 0)
        {
            if (prototype.name.IndexOf("Realistic", StringComparison.OrdinalIgnoreCase) >= 0
                || prototype.name.IndexOf("BlackHawk", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RemoveSketchfabSceneExtras(prototype);
            }

            RemoveAircraftStrayGeometry(prototype);
            if (prototype.name.IndexOf("Realistic", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ApplyRealisticAircraftMaterials(prototype);
            }
            else
            {
                ApplyAircraftHelicopterMaterials(prototype);
            }
        }

        float normalizeHeight = kind == UnitKind.Aircraft && prototype.name.IndexOf("Pterosaur", StringComparison.OrdinalIgnoreCase) >= 0
            ? PterosaurModelTargetHeight
            : Poses[kind].TargetHeight;
        NormalizePrototype(prototype, normalizeHeight, kind);

        if (kind == UnitKind.Soldier)
        {
            ConfigureSoldierWeaponPresentation(prototype);
        }
    }

    private GameObject EnsureSoldierM14WeaponPrototype()
    {
        if (soldierM14WeaponPrototype != null)
        {
            return soldierM14WeaponPrototype;
        }

        var source = Resources.Load<GameObject>(SoldierM14ResourceModelPath);
        if (source == null)
        {
            return null;
        }

        soldierM14WeaponPrototype = Instantiate(source, modelCacheRoot, false);
        soldierM14WeaponPrototype.name = "M14Rifle_Prototype";
        soldierM14WeaponPrototype.hideFlags = HideFlags.HideInHierarchy;
        StripCollidersRecursive(soldierM14WeaponPrototype);
        ApplySoldierM14Materials(soldierM14WeaponPrototype);
        NormalizePrototype(soldierM14WeaponPrototype, SoldierM14TargetLength);
        soldierM14WeaponPrototype.SetActive(false);
        return soldierM14WeaponPrototype;
    }

    private void ConfigureSoldierWeaponPresentation(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        EnsureSoldierWeaponOnModel(model);
        ResolveSoldierMuzzleVisual(model);
    }

    private void EnsureSoldierWeaponOnModel(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        RemoveSoldierWeaponFromModel(model);
        AttachSoldierM14Weapon(model);
    }

    private static void RemoveSoldierWeaponFromModel(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        var transforms = model.GetComponentsInChildren<Transform>(true);
        for (int i = transforms.Length - 1; i >= 0; i--)
        {
            Transform part = transforms[i];
            if (part != null
                && (string.Equals(part.name, "SoldierWeapon_M14", StringComparison.Ordinal)
                    || string.Equals(part.name, "SoldierAimMuzzle", StringComparison.Ordinal)))
            {
                Destroy(part.gameObject);
            }
        }
    }

    private void AttachSoldierM14Weapon(GameObject prototype)
    {
        Transform hand = FindSoldierWeaponHand(prototype.transform);
        if (hand == null)
        {
            hand = FindSoldierWeaponArmFallback(prototype.transform) ?? prototype.transform;
        }

        float soldierHeight = SoldierModelTargetHeight;
        if (TryComputeModelBounds(prototype, out Bounds soldierBounds))
        {
            soldierHeight = Mathf.Max(0.001f, soldierBounds.size.y);
        }

        GameObject m14Prototype = EnsureSoldierM14WeaponPrototype();
        if (m14Prototype != null && SoldierWeaponPrefabHasMesh(m14Prototype))
        {
            var weapon = Instantiate(m14Prototype);
            weapon.name = "SoldierWeapon_M14";
            weapon.transform.SetParent(hand, false);
            weapon.transform.localPosition = new Vector3(0.06f, 0.09f, 0.12f);
            weapon.transform.localRotation = Quaternion.Euler(8f, 96f, -98f);
            weapon.SetActive(true);
            EnableSoldierWeaponRenderers(weapon);
            ApplySoldierWeaponLocalScale(weapon.transform, soldierHeight, prototype.transform.lossyScale.y);
            return;
        }

        AttachSoldierM14FallbackPrimitives(hand);
        Transform attached = FindSoldierWeaponRoot(prototype.transform);
        if (attached != null)
        {
            EnableSoldierWeaponRenderers(attached.gameObject);
            ApplySoldierWeaponLocalScale(attached, soldierHeight, prototype.transform.lossyScale.y);
        }
    }

    private static bool SoldierWeaponPrefabHasMesh(GameObject weaponRoot)
    {
        if (weaponRoot == null)
        {
            return false;
        }

        return weaponRoot.GetComponentsInChildren<MeshFilter>(true).Length > 0
            || weaponRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0;
    }

    private static void ApplySoldierWeaponLocalScale(Transform weaponRoot, float soldierHeight, float soldierLossyScaleY)
    {
        if (weaponRoot == null)
        {
            return;
        }

        soldierHeight = Mathf.Max(0.001f, soldierHeight);
        soldierLossyScaleY = Mathf.Max(0.001f, soldierLossyScaleY);
        float targetRifleLength = soldierHeight * 0.62f;
        float scale = targetRifleLength / (SoldierM14TargetLength * soldierLossyScaleY);
        weaponRoot.localScale = Vector3.one * Mathf.Clamp(scale, 0.35f, 2.5f);
    }

    private static void EnableSoldierWeaponRenderers(GameObject weaponRoot)
    {
        if (weaponRoot == null)
        {
            return;
        }

        var renderers = weaponRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
    }

    private static Transform FindSoldierWeaponArmFallback(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform best = null;
        int bestScore = 0;
        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform part = transforms[i];
            if (part == null)
            {
                continue;
            }

            string name = part.name;
            int score = 0;
            if (ContainsNameToken(name, "rightforearm") || ContainsNameToken(name, "right_forearm"))
            {
                score += 70;
            }
            else if (ContainsNameToken(name, "right") && ContainsNameToken(name, "forearm"))
            {
                score += 60;
            }
            else if (ContainsNameToken(name, "right") && ContainsNameToken(name, "arm") && !ContainsNameToken(name, "forearm"))
            {
                score += 40;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = part;
            }
        }

        return best;
    }

    private void AttachSoldierM14FallbackPrimitives(Transform hand)
    {
        var weaponRoot = new GameObject("SoldierWeapon_M14");
        weaponRoot.transform.SetParent(hand, false);
        weaponRoot.transform.localPosition = new Vector3(0.06f, 0.09f, 0.12f);
        weaponRoot.transform.localRotation = Quaternion.Euler(8f, 96f, -98f);
        weaponRoot.transform.localScale = Vector3.one * 1.35f;

        Material metal = GetOpaqueMaterial(new Color(0.22f, 0.24f, 0.20f, 1f));
        Material wood = GetOpaqueMaterial(new Color(0.34f, 0.22f, 0.12f, 1f));
        Material grip = GetOpaqueMaterial(new Color(0.10f, 0.11f, 0.09f, 1f));

        var receiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
        receiver.name = "Receiver";
        receiver.transform.SetParent(weaponRoot.transform, false);
        receiver.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        receiver.transform.localScale = new Vector3(0.07f, 0.10f, 0.18f);
        receiver.GetComponent<Renderer>().sharedMaterial = metal;
        DestroyCollider(receiver);

        var barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrel.name = "Barrel";
        barrel.transform.SetParent(weaponRoot.transform, false);
        barrel.transform.localPosition = new Vector3(0f, 0.01f, 0.36f);
        barrel.transform.localScale = new Vector3(0.04f, 0.04f, 0.52f);
        barrel.GetComponent<Renderer>().sharedMaterial = metal;
        DestroyCollider(barrel);

        var handguard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handguard.name = "Handguard";
        handguard.transform.SetParent(weaponRoot.transform, false);
        handguard.transform.localPosition = new Vector3(0f, -0.01f, 0.18f);
        handguard.transform.localScale = new Vector3(0.055f, 0.055f, 0.20f);
        handguard.GetComponent<Renderer>().sharedMaterial = wood;
        DestroyCollider(handguard);

        var magazine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        magazine.name = "Magazine";
        magazine.transform.SetParent(weaponRoot.transform, false);
        magazine.transform.localPosition = new Vector3(0f, -0.06f, 0.04f);
        magazine.transform.localScale = new Vector3(0.05f, 0.13f, 0.11f);
        magazine.GetComponent<Renderer>().sharedMaterial = grip;
        DestroyCollider(magazine);

        var stock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stock.name = "Stock";
        stock.transform.SetParent(weaponRoot.transform, false);
        stock.transform.localPosition = new Vector3(0f, 0.02f, -0.22f);
        stock.transform.localScale = new Vector3(0.05f, 0.09f, 0.26f);
        stock.GetComponent<Renderer>().sharedMaterial = wood;
        DestroyCollider(stock);
    }

    private void ApplySoldierM14Materials(GameObject weaponRoot)
    {
        if (weaponRoot == null)
        {
            return;
        }

        Material metal = GetOpaqueMaterial(new Color(0.14f, 0.15f, 0.13f, 1f));
        Material wood = GetOpaqueMaterial(new Color(0.34f, 0.22f, 0.12f, 1f));
        Material grip = GetOpaqueMaterial(new Color(0.10f, 0.11f, 0.09f, 1f));

        var renderers = weaponRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            string partName = renderer.gameObject.name;
            Material material = metal;
            if (ContainsNameToken(partName, "stock") || ContainsNameToken(partName, "handguard"))
            {
                material = wood;
            }
            else if (ContainsNameToken(partName, "magazine"))
            {
                material = grip;
            }

            var materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                materials[m] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private void StripCollidersRecursive(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        var colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                Destroy(colliders[i]);
            }
        }
    }

    private static Transform FindSoldierWeaponRoot(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform part = transforms[i];
            if (part != null && string.Equals(part.name, "SoldierWeapon_M14", StringComparison.Ordinal))
            {
                return part;
            }
        }

        return null;
    }

    private Transform ResolveSoldierMuzzleVisual(GameObject model)
    {
        if (model == null)
        {
            return null;
        }

        Transform weapon = FindSoldierWeaponRoot(model.transform);
        if (weapon != null)
        {
            Transform barrel = FindDescendantByNameToken(weapon, "barrel");
            if (barrel != null)
            {
                return barrel;
            }

            Transform muzzle = FindDescendantByNameToken(weapon, "muzzle");
            if (muzzle != null)
            {
                return muzzle;
            }

            return weapon;
        }

        Transform hand = FindSoldierWeaponHand(model.transform);
        if (hand == null)
        {
            return null;
        }

        Transform marker = hand.Find("SoldierAimMuzzle");
        if (marker == null)
        {
            marker = new GameObject("SoldierAimMuzzle").transform;
            marker.SetParent(hand, false);
            marker.localPosition = new Vector3(0.03f, 0.08f, 0.38f);
            marker.localRotation = Quaternion.Euler(8f, 92f, -95f);
            marker.localScale = Vector3.one;
        }

        return marker;
    }

    private static Transform FindDescendantByNameToken(Transform root, string token)
    {
        if (root == null)
        {
            return null;
        }

        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform part = transforms[i];
            if (part != null && ContainsNameToken(part.name, token))
            {
                return part;
            }
        }

        return null;
    }

    private Transform FindSoldierWeaponHand(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform best = null;
        int bestScore = 0;
        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform part = transforms[i];
            if (part == null)
            {
                continue;
            }

            string name = part.name;
            if (IsSoldierFingerBoneName(name))
            {
                continue;
            }

            int score = 0;
            if (string.Equals(name, "mixamorig:RightHand", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "RightHand", StringComparison.OrdinalIgnoreCase))
            {
                score += 200;
            }
            else if (ContainsNameToken(name, "righthand"))
            {
                score += 120;
            }
            else if (ContainsNameToken(name, "right") && ContainsNameToken(name, "hand"))
            {
                score += 90;
            }
            else if (ContainsNameToken(name, "hand.r"))
            {
                score += 80;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = part;
            }
        }

        return best;
    }

    private static bool IsSoldierFingerBoneName(string name)
    {
        return ContainsNameToken(name, "index")
            || ContainsNameToken(name, "thumb")
            || ContainsNameToken(name, "pinky")
            || ContainsNameToken(name, "ring")
            || ContainsNameToken(name, "middle");
    }

    private void DestroyCollider(GameObject part)
    {
        if (part == null)
        {
            return;
        }

        var collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private static bool UnitUsesGiantSkinnedLocomotion(BattleUnit unit)
    {
        if (unit == null || unit.kind != UnitKind.Giant)
        {
            return false;
        }

        if (UsesAnimatorPlayback(unit))
        {
            return true;
        }

        return unit.animator == null && unit.animations != null && unit.animations.Length > 0;
    }

    private static bool PterosaurModelUsesAuthoredTextures(GameObject model)
    {
        return model != null && UnitModelUsesAuthoredTextures(model);
    }

    private static bool UnitModelUsesAuthoredTextures(GameObject model)
    {
        if (model == null)
        {
            return false;
        }

        var renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var materials = renderers[i].sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                Material material = materials[m];
                if (material == null)
                {
                    continue;
                }

                if (material.mainTexture != null)
                {
                    return true;
                }

                if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ModelHasEmbeddedTextures(GameObject prototype)
    {
        if (prototype == null)
        {
            return false;
        }

        var renderers = prototype.GetComponentsInChildren<Renderer>(true);
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
                Material material = materials[m];
                if (material == null)
                {
                    continue;
                }

                if (material.mainTexture != null)
                {
                    return true;
                }

                if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
                {
                    return true;
                }

                if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool SoldierUsesBuiltInTextures(GameObject prototype)
    {
        if (prototype == null)
        {
            return false;
        }

        var renderers = prototype.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (renderer.name.IndexOf("vanguard", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                var material = materials[m];
                if (material != null
                    && (material.HasProperty("_Metallic") || material.HasProperty("_MetallicGlossMap")))
                {
                    return true;
                }
            }
        }

        return ModelHasEmbeddedTextures(prototype);
    }

    private void ApplyPixelhouseZombieMaterials(GameObject prototype)
    {
        if (prototype == null)
        {
            return;
        }

        Material bodyMaterial = GetPixelhouseZombieMaterial();
        if (bodyMaterial == null)
        {
            return;
        }

        var renderers = prototype.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || IsGiantStrayRenderer(renderer))
            {
                continue;
            }

            var materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                materials[m] = bodyMaterial;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private Material GetPixelhouseZombieMaterial()
    {
        const string diffusePath = GiantPixelhouseResourceFolderPath + "/ZombieDiffuse";
        const string normalPath = GiantPixelhouseResourceFolderPath + "/ZombieNormal";
        const string specularPath = GiantPixelhouseResourceFolderPath + "/ZombieSpecular";
        string key = $"pixelhouse-zombie:{diffusePath}:{normalPath}:{specularPath}";
        Material material;
        if (materialCache.TryGetValue(key, out material))
        {
            return material;
        }

        Texture diffuse = Resources.Load<Texture>(diffusePath);
        if (diffuse == null)
        {
            return null;
        }

        Texture normal = Resources.Load<Texture>(normalPath);
        Texture specular = Resources.Load<Texture>(specularPath);
        diffuse.wrapMode = TextureWrapMode.Clamp;
        diffuse.filterMode = FilterMode.Trilinear;
        diffuse.anisoLevel = 4;

        Shader shader = FindRuntimeShader(null, "Standard", "Legacy Shaders/Diffuse", "Unlit/Texture", "Sprites/Default");
        material = new Material(shader);
        Color tint = Color.white;
        material.color = tint;

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", diffuse);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", diffuse);
            material.SetColor("_BaseColor", tint);
        }

        if (normal != null)
        {
            if (material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            if (material.HasProperty("_NormalMap"))
            {
                material.SetTexture("_NormalMap", normal);
            }
        }

        if (specular != null && material.HasProperty("_SpecGlossMap"))
        {
            material.SetTexture("_SpecGlossMap", specular);
            material.EnableKeyword("_SPECGLOSSMAP");
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0.28f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0.04f);
        }

        ApplyOpaqueDoubleSided(material);
        materialCache[key] = material;
        return material;
    }

    private static void RemoveSketchfabSceneExtras(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        var cameras = root.GetComponentsInChildren<Camera>(true);
        for (int i = cameras.Length - 1; i >= 0; i--)
        {
            if (cameras[i] != null)
            {
                SafeDestroyUnityObject(cameras[i].gameObject);
            }
        }

        var lights = root.GetComponentsInChildren<Light>(true);
        for (int i = lights.Length - 1; i >= 0; i--)
        {
            if (lights[i] != null)
            {
                SafeDestroyUnityObject(lights[i].gameObject);
            }
        }

        var audioSources = root.GetComponentsInChildren<AudioSource>(true);
        for (int i = audioSources.Length - 1; i >= 0; i--)
        {
            if (audioSources[i] != null)
            {
                SafeDestroyUnityObject(audioSources[i]);
            }
        }

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = renderers.Length - 1; i >= 0; i--)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            string name = renderer.gameObject.name;
            if (ContainsNameToken(name, "plane")
                || ContainsNameToken(name, "ground")
                || ContainsNameToken(name, "floor")
                || ContainsNameToken(name, "shadow")
                || ContainsNameToken(name, "quad")
                || ContainsNameToken(name, "card"))
            {
                SafeDestroyUnityObject(renderer.gameObject);
            }
        }
    }

    private void RemoveGiantStrayGeometry(GameObject prototype)
    {
        if (prototype == null)
        {
            return;
        }

        var cameras = prototype.GetComponentsInChildren<Camera>(true);
        for (int i = cameras.Length - 1; i >= 0; i--)
        {
            DestroyImmediate(cameras[i].gameObject);
        }

        var lights = prototype.GetComponentsInChildren<Light>(true);
        for (int i = lights.Length - 1; i >= 0; i--)
        {
            DestroyImmediate(lights[i].gameObject);
        }

        var renderers = prototype.GetComponentsInChildren<Renderer>(true);
        int removed = 0;
        for (int i = renderers.Length - 1; i >= 0; i--)
        {
            Renderer renderer = renderers[i];
            if (!IsGiantStrayRenderer(renderer))
            {
                continue;
            }

            DestroyImmediate(renderer.gameObject);
            removed++;
        }

        if (removed > 0)
        {
            Debug.Log($"Removed {removed} stray renderer(s) from giant/zombie model.");
        }
    }

    private static bool IsGiantStrayRenderer(Renderer renderer)
    {
        if (renderer == null)
        {
            return true;
        }

        string name = renderer.gameObject.name;
        if (ContainsNameToken(name, "plane")
            || ContainsNameToken(name, "ground")
            || ContainsNameToken(name, "floor")
            || ContainsNameToken(name, "shadow")
            || ContainsNameToken(name, "quad")
            || ContainsNameToken(name, "card"))
        {
            return true;
        }

        Mesh mesh = null;
        var meshFilter = renderer.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            mesh = meshFilter.sharedMesh;
        }
        else
        {
            var skinned = renderer as SkinnedMeshRenderer;
            if (skinned != null)
            {
                mesh = skinned.sharedMesh;
            }
        }

        if (mesh == null)
        {
            return false;
        }

        Vector3 size = mesh.bounds.size;
        float height = Mathf.Max(0.0001f, size.y);
        float footprint = Mathf.Max(size.x, size.z);
        if (footprint > height * 4.5f && height < footprint * 0.12f)
        {
            return true;
        }

        float maxSpan = Mathf.Max(size.x, size.y, size.z);
        float minSpan = Mathf.Min(size.x, size.y, size.z);
        return maxSpan > 6f && minSpan < 0.08f;
    }

    private static void ApplyKenneyZombieSkin(GameObject prototype, string textureResourcePath)
    {
        if (prototype == null || string.IsNullOrEmpty(textureResourcePath))
        {
            return;
        }

        Texture skinTexture = Resources.Load<Texture>(textureResourcePath);
        if (skinTexture == null)
        {
            return;
        }

        var renderers = prototype.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            var materials = renderer.materials;
            for (int m = 0; m < materials.Length; m++)
            {
                Material material = materials[m];
                if (material == null)
                {
                    continue;
                }

                if (material.HasProperty("_MainTex"))
                {
                    material.mainTexture = skinTexture;
                }

                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", skinTexture);
                }
            }
        }
    }

    private Texture2D LoadRealisticAircraftBodyAlbedo()
    {
        string[] candidates =
        {
            RealisticAircraftBodyTexturePath,
            RealisticAircraftFolderPath + "/Apache_Texture_White",
            RealisticAircraftFolderPath + "/Apache_Texture_Orange",
            RealisticAircraftFolderPath + "/Apache_Texture_Purple",
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            Texture2D texture = Resources.Load<Texture2D>(candidates[i]);
            if (texture != null)
            {
                return texture;
            }
        }

        return null;
    }

    private void ApplyRealisticAircraftMaterials(GameObject prototype)
    {
        if (prototype == null)
        {
            return;
        }

        Texture2D bodyAlbedo = LoadRealisticAircraftBodyAlbedo();
        Texture2D rotorAlbedo = Resources.Load<Texture2D>(RealisticAircraftRotorTexturePath) ?? bodyAlbedo;
        EnsureRealisticAircraftMaterials(bodyAlbedo, rotorAlbedo);

        Renderer[] renderers = prototype.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || IsAircraftStrayRenderer(renderer))
            {
                continue;
            }

            Material target = IsAircraftRotorRenderer(renderer)
                ? realisticAircraftRotorMaterial
                : realisticAircraftBodyMaterial;
            if (target == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                materials[m] = target;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private void EnsureRealisticAircraftMaterials(Texture2D bodyAlbedo, Texture2D rotorAlbedo)
    {
        if (realisticAircraftBodyMaterial == null)
        {
            realisticAircraftBodyMaterial = GetOpaqueMaterial(AircraftBodyMaterialTint);
            ApplyOpaqueDoubleSided(realisticAircraftBodyMaterial);
        }

        if (bodyAlbedo != null)
        {
            ApplyAlbedoToMaterial(realisticAircraftBodyMaterial, bodyAlbedo);
        }

        ApplyAircraftMaterialTint(realisticAircraftBodyMaterial, AircraftBodyMaterialTint);

        if (realisticAircraftRotorMaterial == null)
        {
            realisticAircraftRotorMaterial = GetOpaqueMaterial(new Color(0.92f, 0.94f, 0.98f, 1f));
            ApplyOpaqueDoubleSided(realisticAircraftRotorMaterial);
        }

        if (rotorAlbedo != null)
        {
            ApplyAlbedoToMaterial(realisticAircraftRotorMaterial, rotorAlbedo);
        }

        ApplyAircraftMaterialTint(realisticAircraftRotorMaterial, new Color(0.92f, 0.94f, 0.98f, 1f));
    }

    private static void ApplyAircraftMaterialTint(Material material, Color tint)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", tint);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }
    }

    private static void ApplyAlbedoToMaterial(Material material, Texture2D albedo)
    {
        if (material == null || albedo == null)
        {
            return;
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", albedo);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", albedo);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }
    }

    private static bool IsAircraftRotorRenderer(Renderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        string name = renderer.gameObject.name.ToLowerInvariant();
        if (renderer.transform.parent != null)
        {
            name += " " + renderer.transform.parent.name.ToLowerInvariant();
        }

        return name.Contains("propeller")
            || name.Contains("rotor")
            || (name.Contains("prop") && !name.Contains("property"));
    }

    private static bool IsAircraftStrayRenderer(Renderer renderer)
    {
        if (renderer == null)
        {
            return true;
        }

        string name = renderer.gameObject.name;
        return string.Equals(name, "HelicopterBase", StringComparison.OrdinalIgnoreCase)
            || name.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Light", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void RemoveAircraftStrayGeometry(GameObject prototype)
    {
        if (prototype == null)
        {
            return;
        }

        Transform[] transforms = prototype.GetComponentsInChildren<Transform>(true);
        for (int i = transforms.Length - 1; i >= 0; i--)
        {
            Transform node = transforms[i];
            if (node == null || node == prototype.transform)
            {
                continue;
            }

            string name = node.name;
            bool stray = string.Equals(name, "HelicopterBase", StringComparison.OrdinalIgnoreCase)
                || name.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0
                || (name.IndexOf("Light", StringComparison.OrdinalIgnoreCase) >= 0 && node.GetComponent<Light>() != null);
            if (!stray)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(node.gameObject);
            }
            else
            {
                DestroyImmediate(node.gameObject);
            }
        }
    }

    private void ApplyAircraftHelicopterMaterials(GameObject prototype)
    {
        if (prototype == null)
        {
            return;
        }

        if (aircraftHelicopterMaterial == null)
        {
            aircraftHelicopterMaterial = GetOpaqueMaterial(AircraftBodyMaterialTint);
            Texture2D diffuse = Resources.Load<Texture2D>(AircraftDiffuseResourcePath);
            if (diffuse != null && aircraftHelicopterMaterial.HasProperty("_MainTex"))
            {
                aircraftHelicopterMaterial.mainTexture = diffuse;
            }

            ApplyAircraftMaterialTint(aircraftHelicopterMaterial, AircraftBodyMaterialTint);
        }

        Renderer[] renderers = prototype.GetComponentsInChildren<Renderer>(true);
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
                materials[m] = aircraftHelicopterMaterial;
                ApplyOpaqueDoubleSided(materials[m]);
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static void ApplySoldierMilitaryTint(GameObject prototype)
    {
        if (prototype == null)
        {
            return;
        }

        var renderers = prototype.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            bool skin = renderer.name.IndexOf("skin", StringComparison.OrdinalIgnoreCase) >= 0
                || renderer.name.IndexOf("face", StringComparison.OrdinalIgnoreCase) >= 0
                || renderer.name.IndexOf("head", StringComparison.OrdinalIgnoreCase) >= 0;
            Color target = skin
                ? new Color(0.82f, 0.68f, 0.52f, 1f)
                : new Color(0.42f, 0.48f, 0.34f, 1f);

            var materials = renderer.materials;
            for (int m = 0; m < materials.Length; m++)
            {
                if (materials[m] != null && materials[m].HasProperty("_Color"))
                {
                    materials[m].color = Color.Lerp(materials[m].color, target, skin ? 0.38f : 0.52f);
                }
            }
        }
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

            if (Application.isPlaying)
            {
                Destroy(renderer.gameObject);
            }
            else
            {
                DestroyImmediate(renderer.gameObject);
            }

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

    private static bool TryComputeModelBounds(GameObject root, out Bounds bounds, bool includeTankDisplayGeometry = true)
    {
        Bounds combinedBounds = default;
        bool hasBounds = false;

        void EncapsulateWorldBounds(Bounds worldBounds)
        {
            if (!hasBounds)
            {
                combinedBounds = worldBounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(worldBounds.min);
                combinedBounds.Encapsulate(worldBounds.max);
            }
        }

        void EncapsulateLocalMeshBounds(Transform transform, Bounds localMeshBounds)
        {
            Vector3 worldCenter = transform.TransformPoint(localMeshBounds.center);
            Vector3 extents = localMeshBounds.extents;
            Vector3 axisX = transform.TransformVector(new Vector3(extents.x, 0f, 0f));
            Vector3 axisY = transform.TransformVector(new Vector3(0f, extents.y, 0f));
            Vector3 axisZ = transform.TransformVector(new Vector3(0f, 0f, extents.z));
            float extentX = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            float extentY = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            float extentZ = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);
            EncapsulateWorldBounds(new Bounds(worldCenter, new Vector3(extentX * 2f, extentY * 2f, extentZ * 2f)));
        }

        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            Mesh mesh = meshFilters[i].sharedMesh;
            if (mesh == null)
            {
                continue;
            }

            var renderer = meshFilters[i].GetComponent<Renderer>();
            if (renderer != null && IsGiantStrayRenderer(renderer))
            {
                continue;
            }

            if (renderer != null && ShouldSkipTankBoundsRenderer(renderer, mesh.bounds, includeTankDisplayGeometry))
            {
                continue;
            }

            EncapsulateLocalMeshBounds(meshFilters[i].transform, mesh.bounds);
        }

        SkinnedMeshRenderer[] skinnedMeshes = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedMeshes.Length; i++)
        {
            SkinnedMeshRenderer skinned = skinnedMeshes[i];
            if (skinned.sharedMesh == null || IsGiantStrayRenderer(skinned))
            {
                continue;
            }

            skinned.updateWhenOffscreen = true;
            EncapsulateLocalMeshBounds(skinned.transform, skinned.sharedMesh.bounds);
        }

        if (hasBounds)
        {
            bounds = combinedBounds;
            return true;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bool started = false;
        bounds = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || ShouldSkipTankBoundsRenderer(renderer, renderer.bounds, includeTankDisplayGeometry))
            {
                continue;
            }

            if (!started)
            {
                bounds = renderer.bounds;
                started = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return started;
    }

    private static bool TryComputeModelBoundsInRootLocalSpace(GameObject root, out Bounds localBounds, bool includeTankDisplayGeometry = true)
    {
        localBounds = default;
        if (root == null)
        {
            return false;
        }

        if (!TryComputeModelBounds(root, out Bounds worldBounds, includeTankDisplayGeometry))
        {
            return false;
        }

        Transform rootTransform = root.transform;
        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;
        bool hasBounds = false;
        for (int xi = -1; xi <= 1; xi += 2)
        {
            for (int yi = -1; yi <= 1; yi += 2)
            {
                for (int zi = -1; zi <= 1; zi += 2)
                {
                    Vector3 worldCorner = center + new Vector3(extents.x * xi, extents.y * yi, extents.z * zi);
                    Vector3 localCorner = rootTransform.InverseTransformPoint(worldCorner);
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(localCorner);
                    }
                }
            }
        }

        return hasBounds;
    }

    private static bool ShouldSkipTankBoundsRenderer(Renderer renderer, Bounds localOrWorldBounds, bool includeTankDisplayGeometry)
    {
        if (!HasRealisticTankInResources())
        {
            return false;
        }

        if (!includeTankDisplayGeometry && IsTankDisplayRenderer(renderer))
        {
            return true;
        }

        if (!includeTankDisplayGeometry)
        {
            return false;
        }

        if (IsTankDisplayRenderer(renderer))
        {
            return true;
        }

        Vector3 size = localOrWorldBounds.size;
        float maxAxis = Mathf.Max(size.x, size.y, size.z);
        return maxAxis > RealisticTankBoundsMaxAxis;
    }

    private void ApplyRealisticTankMaterials(GameObject prototype)
    {
        var bodyAlbedo = Resources.Load<Texture2D>(RealisticTankFolderPath + "/tex1");
        var bodyNormal = Resources.Load<Texture2D>(RealisticTankFolderPath + "/tex1 - nrm");
        var detailAlbedo = Resources.Load<Texture2D>(RealisticTankFolderPath + "/tex2");
        var detailNormal = Resources.Load<Texture2D>(RealisticTankFolderPath + "/tex2 - nrm");
        var rubberAlbedo = Resources.Load<Texture2D>(RealisticTankFolderPath + "/tex3");
        var rubberNormal = Resources.Load<Texture2D>(RealisticTankFolderPath + "/tex3 - nrm");
        if (bodyAlbedo == null)
        {
            return;
        }

        var renderers = prototype.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || IsTankDisplayRenderer(renderer))
            {
                continue;
            }

            var materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                Material material = materials[m];
                if (material == null)
                {
                    continue;
                }

                string materialName = material.name;
                int instanceSuffix = materialName.IndexOf(" (", StringComparison.Ordinal);
                if (instanceSuffix >= 0)
                {
                    materialName = materialName.Substring(0, instanceSuffix);
                }

                Texture2D albedo = bodyAlbedo;
                Texture2D normal = bodyNormal;
                if (ContainsNameToken(materialName, "tyre") || ContainsNameToken(materialName, "tire"))
                {
                    albedo = rubberAlbedo ?? bodyAlbedo;
                    normal = rubberNormal ?? bodyNormal;
                }
                else if (ContainsNameToken(materialName, "tex3"))
                {
                    albedo = rubberAlbedo ?? bodyAlbedo;
                    normal = rubberNormal ?? bodyNormal;
                }
                else if (ContainsNameToken(materialName, "tex2"))
                {
                    albedo = detailAlbedo ?? bodyAlbedo;
                    normal = detailNormal ?? bodyNormal;
                }

                ApplyTankMaterialMaps(material, albedo, normal);
                materials[m] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static void ApplyTankMaterialMaps(Material material, Texture2D albedo, Texture2D normal)
    {
        if (material == null || albedo == null)
        {
            return;
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", albedo);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", albedo);
        }

        if (normal != null && material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
        }

        if (material.HasProperty("_Color"))
        {
            material.color = Color.white;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }
    }

    private static void ApplyAircraftPrototypeBindRotation(GameObject prototype)
    {
        if (prototype == null)
        {
            return;
        }

        if (!TryGetAircraftFuselageLocalBounds(prototype, out Bounds bounds))
        {
            prototype.transform.localRotation = Quaternion.Euler(AircraftBindPitch, AircraftBindYaw, AircraftBindRoll);
            return;
        }

        Vector3 size = bounds.size;
        int axis = 0;
        float maxSize = size.x;
        if (size.y > maxSize)
        {
            axis = 1;
            maxSize = size.y;
        }

        if (size.z > maxSize)
        {
            axis = 2;
        }

        Quaternion correction = axis switch
        {
            1 => Quaternion.Euler(-90f, 0f, 0f),
            0 => Quaternion.Euler(0f, 90f, 0f),
            _ => Quaternion.identity,
        };

        prototype.transform.localRotation = correction;
    }

    private static float GetPterosaurBoundsHeight(GameObject model, Bounds bounds)
    {
        if (model != null && TryGetPterosaurFuselageLocalBounds(model, out Bounds fuselage))
        {
            return Mathf.Max(0.001f, Mathf.Max(fuselage.size.x, fuselage.size.z, fuselage.size.y * 0.72f));
        }

        // 滑翔姿态翼展沿 X/Z，单独用 size.y 会把缩放放大到数十倍并叠成灰团。
        return Mathf.Max(0.001f, Mathf.Max(bounds.size.x, bounds.size.z, bounds.size.y * 0.55f));
    }

    private static float GetAircraftBoundsMetric(GameObject model, Bounds fullBounds)
    {
        if (TryGetAircraftFuselageLocalBounds(model, out Bounds fuselage))
        {
            return Mathf.Max(0.001f, Mathf.Max(fuselage.size.x, fuselage.size.z, fuselage.size.y * 0.72f));
        }

        return Mathf.Max(0.001f, Mathf.Max(fullBounds.size.x, fullBounds.size.z, fullBounds.size.y * 0.55f));
    }

    private static bool PterosaurPrototypeLooksYUp(Bounds bounds)
    {
        Vector3 size = bounds.size;
        float maxHorizontal = Mathf.Max(size.x, size.z);
        return size.y >= maxHorizontal * 0.32f;
    }

    private static void ApplyRocketTruckPrototypeBindRotation(GameObject prototype)
    {
        if (prototype == null)
        {
            return;
        }

        if (!TryComputeModelBounds(prototype, out Bounds bounds))
        {
            prototype.transform.localRotation = Quaternion.identity;
            return;
        }

        Vector3 size = bounds.size;
        prototype.transform.localRotation = size.x > size.z * 1.05f
            ? Quaternion.Euler(0f, -90f, 0f)
            : Quaternion.identity;
    }

    private static void ApplyPterosaurPrototypeBindRotation(GameObject prototype)
    {
        if (prototype == null)
        {
            return;
        }

        if (!TryComputeModelBounds(prototype, out Bounds bounds))
        {
            prototype.transform.localRotation = Quaternion.identity;
            return;
        }

        Vector3 size = bounds.size;
        // Sketchfab Pteranodon：滑翔姿态翼展沿 X、机头朝 +X，统一绕 Y 对齐游戏 +Z 前进方向。
        if (size.x >= size.z * 0.9f)
        {
            prototype.transform.localRotation = Quaternion.Euler(0f, PterosaurGlbBindYawDegrees, 0f);
            return;
        }

        if (size.z > size.x * 1.08f)
        {
            prototype.transform.localRotation = Quaternion.identity;
            return;
        }

        prototype.transform.localRotation = Quaternion.Euler(0f, PterosaurGlbBindYawDegrees, 0f);
    }

    private void NormalizePrototype(GameObject prototype, float targetHeight, UnitKind kind = UnitKind.Soldier)
    {
        bool includeTankDisplayGeometry = !(HasRealisticTankInResources() && kind == UnitKind.Tank);
        if (!TryComputeModelBounds(prototype, out Bounds bounds, includeTankDisplayGeometry))
        {
            return;
        }

        if (kind == UnitKind.Aircraft)
        {
            if (prototype.name.IndexOf("Pterosaur", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ApplyPterosaurPrototypeBindRotation(prototype);
            }
            else
            {
                ApplyAircraftPrototypeBindRotation(prototype);
            }

            if (!TryComputeModelBounds(prototype, out bounds))
            {
                return;
            }
        }

        bool normalizePterosaur = kind == UnitKind.Aircraft
            && prototype.name.IndexOf("Pterosaur", StringComparison.OrdinalIgnoreCase) >= 0;
        float currentHeight = normalizePterosaur
            ? GetPterosaurBoundsHeight(prototype, bounds)
            : kind == UnitKind.Aircraft
            ? GetAircraftBoundsMetric(prototype, bounds)
            : kind == UnitKind.Tank
                ? GetTankBoundsMetric(bounds)
                : Mathf.Max(0.001f, bounds.size.y);
        float uniformScale = targetHeight / currentHeight;
        if (kind == UnitKind.Aircraft)
        {
            uniformScale = Mathf.Clamp(uniformScale, 0.04f, 48f);
        }

        prototype.transform.localScale = prototype.transform.localScale * uniformScale;

        if (!TryComputeModelBounds(prototype, out bounds, includeTankDisplayGeometry))
        {
            return;
        }

        if (kind == UnitKind.Aircraft)
        {
            UnitCombatVariant variant = prototype.name.IndexOf("Pterosaur", StringComparison.OrdinalIgnoreCase) >= 0
                ? UnitCombatVariant.Pterosaur
                : UnitCombatVariant.Standard;
            AlignAirUnitModelForCruise(prototype, variant);
        }
        else if (TryComputeModelBoundsInRootLocalSpace(prototype, out Bounds groundedBounds))
        {
            prototype.transform.localPosition += new Vector3(0f, -groundedBounds.min.y, 0f);
        }
        else
        {
            prototype.transform.localPosition += new Vector3(0f, -bounds.min.y, 0f);
        }
    }

    private static bool TryGetAirUnitCruiseAlignBounds(GameObject model, UnitCombatVariant variant, out Bounds bounds)
    {
        bounds = default;
        if (model == null)
        {
            return false;
        }

        if (variant == UnitCombatVariant.Pterosaur && TryGetPterosaurFuselageLocalBounds(model, out bounds))
        {
            return true;
        }

        if (variant != UnitCombatVariant.Pterosaur && TryGetAircraftFuselageLocalBounds(model, out bounds))
        {
            return true;
        }

        return TryComputeModelBoundsInRootLocalSpace(model, out bounds);
    }

    private static void AlignAirUnitModelForCruise(GameObject model, UnitCombatVariant variant = UnitCombatVariant.Standard)
    {
        if (!TryGetAirUnitCruiseAlignBounds(model, variant, out Bounds bounds))
        {
            return;
        }

        model.transform.localPosition += new Vector3(
            -bounds.center.x,
            AirUnitCruiseBottomY - bounds.min.y,
            -bounds.center.z);
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
                body.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.42f, 0.48f, 0.34f, 1f));
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
                return null;
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

        NormalizePrototype(root, pose.TargetHeight, kind);
        root.SetActive(false);
        return root;
    }

    private void AttachPrototypesToUnits()
    {
        // Units attach on first ActivateUnit (lazy) — keeps load fast with large armies.
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

        SetupPterosaurBattlePrototype();
        AttachPrototypesToUnits();
    }

    private void DetachUnitModelInstance(BattleUnit unit)
    {
        if (unit == null || unit.modelInstance == null)
        {
            return;
        }

        DisposeUnitAnimator(unit);
        Destroy(unit.modelInstance);
        unit.modelInstance = null;
        unit.motionAccessoryRoot = null;
        unit.aircraftRotorRoot = null;
        unit.aircraftRotorBaseLocalRotation = Quaternion.identity;
        unit.aircraftRotorRigs.Clear();
        unit.pterosaurWingRigs.Clear();
        unit.tankMotionRig = null;
        unit.soldierMuzzleVisual = null;
        unit.currentAnimation = string.Empty;
        unit.animationPresentationKey = -1;
    }

    private void AttachUnitModel(BattleUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        // During battle only instantiate from cached prototypes (no GLTF reload).
        if (battleTime > 0f)
        {
            if (unit.modelInstance == null)
            {
                TryAttachCachedUnitModel(unit);
            }

            return;
        }

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
            unit.aircraftRotorBaseLocalRotation = Quaternion.identity;
            unit.aircraftRotorRigs.Clear();
            unit.pterosaurWingRigs.Clear();
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
            ConfigureRuntimeModel(model, unit);
            if (unit.kind == UnitKind.Tank && unit.tankAimRoot != null)
            {
                unit.tankAimRoot.gameObject.SetActive(usingFallbackPrototype);
            }

            FinalizeAttachedUnitModel(unit, model, usingFallbackPrototype);
        }
        finally
        {
            if (!rootWasActive)
            {
                unit.root.SetActive(false);
            }
        }
    }

    private bool TryAttachCachedUnitModel(BattleUnit unit)
    {
        if (unit == null || unit.modelInstance != null)
        {
            return unit.modelInstance != null;
        }

        GameObject prototype = ResolvePrototypeForUnit(unit);
        if (prototype == null || unit.body == null)
        {
            return false;
        }

        bool rootWasActive = unit.root.activeSelf;
        if (!rootWasActive)
        {
            unit.root.SetActive(true);
        }

        try
        {
            bool usingFallbackPrototype = prototype.name.IndexOf("Fallback", StringComparison.OrdinalIgnoreCase) >= 0;
            var model = Instantiate(prototype, unit.body, false);
            model.name = unit.kind.ToString();
            model.SetActive(true);
            ConfigureRuntimeModel(model, unit);
            if (unit.kind == UnitKind.Tank && unit.tankAimRoot != null)
            {
                unit.tankAimRoot.gameObject.SetActive(usingFallbackPrototype);
            }

            FinalizeAttachedUnitModel(unit, model, usingFallbackPrototype);
            return true;
        }
        finally
        {
            if (!rootWasActive)
            {
                unit.root.SetActive(false);
            }
        }
    }

    private void FinalizeAttachedUnitModel(BattleUnit unit, GameObject model, bool usingFallbackPrototype)
    {
        unit.modelInstance = model;
        unit.animations = model.GetComponentsInChildren<Animation>(true);
        unit.baseModelScale = model.transform.localScale;
        unit.baseModelLocalPosition = model.transform.localPosition;
        unit.baseModelLocalRotation = model.transform.localRotation;
        unit.soldierUsesVanguardMesh = unit.kind == UnitKind.Soldier && SoldierUsesBuiltInTextures(model);
        if (unit.kind == UnitKind.Soldier)
        {
            EnsureSoldierWeaponOnModel(model);
            unit.soldierMuzzleVisual = ResolveSoldierMuzzleVisual(model);
        }
        else
        {
            unit.soldierMuzzleVisual = null;
        }

        if (unit.kind == UnitKind.Giant && unit.combatVariant == UnitCombatVariant.RocketGiant)
        {
            AttachGiantRocketLauncher(model);
        }

        if (unit.combatVariant == UnitCombatVariant.RocketTruck)
        {
            unit.modelYawOffset = RocketTruckMeshYawOffset;
            unit.tankAimRoot = null;
            unit.tankBarrelVisual = null;
            unit.tankMuzzleVisual = null;
        }

        unit.currentAnimation = string.Empty;
        ConfigureAnimatorPlayback(unit, model);
        ConfigureProceduralMotionRig(unit);
        if (unit.kind == UnitKind.Aircraft)
        {
            AlignAirUnitModelForCruise(model, unit.combatVariant);
            unit.baseModelLocalPosition = model.transform.localPosition;
            unit.baseModelLocalRotation = model.transform.localRotation;
        }

        if (UnitUsesGroundAltitude(unit))
        {
            AlignModelBottomToLocalOrigin(model);
            unit.baseModelLocalPosition = model.transform.localPosition;
            unit.baseModelLocalRotation = model.transform.localRotation;
        }

        SetUnitPlaceholderVisible(unit, unit.modelInstance == null);
        if (unit.combatVariant == UnitCombatVariant.Pterosaur)
        {
            FinalizePterosaurUnitModel(unit, unit.modelInstance);
        }

        PlayUnitAnimation(unit);
    }

    private void FinalizePterosaurUnitModel(BattleUnit unit, GameObject model)
    {
        if (unit == null || model == null)
        {
            return;
        }

        ApplyPterosaurPrototypeBindRotation(model);
        EnsureMinimumPterosaurModelScale(unit, model);
        EnsurePterosaurRenderersVisible(model);
        if (pterosaurVisibilityFallbackPrototype != null
            && model.name.IndexOf("Fallback", StringComparison.OrdinalIgnoreCase) < 0
            && !PterosaurModelIsProceduralBattleMesh(model)
            && !PterosaurModelWorldSpanLooksValid(model)
            && !PterosaurModelUsesAuthoredTextures(model))
        {
            AttachPterosaurVisibilityFallback(unit, model);
            SetPterosaurGltfRenderersEnabled(model, false);
        }
    }

    private void RefreshPterosaurUnitDisplay(BattleUnit unit)
    {
        if (unit == null || !unit.active)
        {
            return;
        }

        if (unit.modelInstance == null)
        {
            EnsureUnitModelAttached(unit);
        }

        EnsurePterosaurUnitDisplay(unit);
    }

    private void EnsurePterosaurUnitDisplay(BattleUnit unit)
    {
        if (unit == null || !unit.active || unit.body == null)
        {
            return;
        }

        if (unit.modelInstance != null
            && PterosaurRuntimeModelIsVisible(unit.modelInstance)
            && PterosaurModelWorldSpanLooksValid(unit.modelInstance))
        {
            FinalizePterosaurUnitModel(unit, unit.modelInstance);
            return;
        }

        if (unit.modelInstance != null)
        {
            Destroy(unit.modelInstance);
            unit.modelInstance = null;
        }

        if (pterosaurVisibilityFallbackPrototype == null)
        {
            Debug.LogWarning($"[ApocalypseKing] Pterosaur unit {unit.id} invisible and no fallback prototype.");
            return;
        }

        var fallback = Instantiate(pterosaurVisibilityFallbackPrototype, unit.body, false);
        fallback.name = "Pterosaur_FallbackRuntime";
        fallback.SetActive(true);
        ConfigureRuntimeModel(fallback, unit);
        FinalizeAttachedUnitModel(unit, fallback, usingFallbackPrototype: true);
        Debug.LogWarning($"[ApocalypseKing] Pterosaur unit {unit.id} uses runtime fallback mesh at ({unit.x:F0},{unit.z:F0}) alt={unit.altitude:F1}");
    }

    private static bool PterosaurSkinnedMeshCollapsed(GameObject model)
    {
        if (model == null)
        {
            return true;
        }

        SkinnedMeshRenderer[] skinnedMeshes = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (skinnedMeshes.Length == 0)
        {
            return false;
        }

        bool hasSkinned = false;
        for (int i = 0; i < skinnedMeshes.Length; i++)
        {
            SkinnedMeshRenderer skinned = skinnedMeshes[i];
            if (skinned == null || !skinned.enabled || skinned.sharedMesh == null)
            {
                continue;
            }

            hasSkinned = true;
            Bounds meshBounds = skinned.sharedMesh.bounds;
            float extent = Mathf.Max(meshBounds.size.x, meshBounds.size.y, meshBounds.size.z)
                * Mathf.Max(0.001f, skinned.transform.lossyScale.magnitude);
            if (extent >= 0.22f)
            {
                return false;
            }
        }

        return hasSkinned;
    }

    private static bool PterosaurRuntimeModelIsVisible(GameObject model)
    {
        if (model == null)
        {
            return false;
        }

        int enabledRenderers = 0;
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].enabled)
            {
                enabledRenderers++;
            }
        }

        if (enabledRenderers == 0)
        {
            return false;
        }

        SkinnedMeshRenderer[] skinnedMeshes = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedMeshes.Length; i++)
        {
            SkinnedMeshRenderer skinned = skinnedMeshes[i];
            if (skinned == null || !skinned.enabled || skinned.sharedMesh == null)
            {
                continue;
            }

            Vector3 meshSize = skinned.sharedMesh.bounds.size;
            float extent = Mathf.Max(meshSize.x, meshSize.y, meshSize.z) * Mathf.Max(0.001f, skinned.transform.lossyScale.magnitude);
            if (extent >= 0.12f)
            {
                return true;
            }
        }

        if (!TryComputeModelBounds(model, out Bounds bounds))
        {
            return enabledRenderers > 0;
        }

        float span = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        return span >= 0.22f;
    }

    private void AttachPterosaurVisibilityFallback(BattleUnit unit, GameObject model)
    {
        if (unit == null || model == null || pterosaurVisibilityFallbackPrototype == null)
        {
            return;
        }

        Transform existing = model.transform.Find("PterosaurVisibilityFallback");
        if (existing != null)
        {
            return;
        }

        var shell = Instantiate(pterosaurVisibilityFallbackPrototype, model.transform, false);
        shell.name = "PterosaurVisibilityFallback";
        shell.transform.localPosition = Vector3.zero;
        shell.transform.localRotation = Quaternion.identity;
        float parentMaxScale = GetMaxAbsVectorComponent(model.transform.lossyScale);
        shell.transform.localScale = parentMaxScale < 0.85f
            ? Vector3.one * Mathf.Clamp(1f / Mathf.Max(0.05f, parentMaxScale), 1f, 6f)
            : Vector3.one;
        shell.SetActive(true);
        ApplyPterosaurProceduralBattleMaterials(shell);
        EnsurePterosaurRenderersVisible(shell);
    }

    private static float GetMaxAbsVectorComponent(Vector3 value)
    {
        return Mathf.Max(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private void EnsureMinimumPterosaurModelScale(BattleUnit unit, GameObject model)
    {
        if (model == null)
        {
            return;
        }

        Vector3 scale = model.transform.localScale;
        float maxScale = GetMaxAbsVectorComponent(scale);
        if (maxScale < 0.12f)
        {
            float boost = 1.35f / Mathf.Max(0.001f, maxScale);
            model.transform.localScale = scale * Mathf.Min(boost, 6f);
        }
        else if (maxScale > 12f)
        {
            model.transform.localScale = scale * (6f / maxScale);
        }

        if (unit != null)
        {
            unit.baseModelScale = model.transform.localScale;
            unit.baseModelLocalPosition = model.transform.localPosition;
            unit.baseModelLocalRotation = model.transform.localRotation;
        }
    }

    private static bool TryComputePterosaurDisplayBounds(GameObject model, out Bounds bounds)
    {
        bounds = default;
        if (model == null)
        {
            return false;
        }

        bool wasActive = model.activeSelf;
        if (!wasActive)
        {
            model.SetActive(true);
        }

        SkinnedMeshRenderer[] skinnedMeshes = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedMeshes.Length; i++)
        {
            if (skinnedMeshes[i] != null)
            {
                skinnedMeshes[i].updateWhenOffscreen = true;
            }
        }

        bool hasBounds = TryComputeModelBounds(model, out bounds);
        if (!wasActive)
        {
            model.SetActive(false);
        }

        return hasBounds;
    }

    private static bool PterosaurModelWorldSpanLooksValid(GameObject model)
    {
        if (!TryComputePterosaurDisplayBounds(model, out Bounds bounds))
        {
            return false;
        }

        Vector3 size = bounds.size;
        float height = size.y;
        float wingspan = Mathf.Max(size.x, size.z);
        if (height < 0.28f || wingspan < 0.28f)
        {
            return false;
        }

        // 目标身高约 4.2；翼展可达体长 2~3 倍，不能按单一 max span 7.5 拒绝。
        return height <= 10f && wingspan <= 22f;
    }

    private static string DescribePterosaurBounds(GameObject model)
    {
        if (!TryComputePterosaurDisplayBounds(model, out Bounds bounds))
        {
            return "no-bounds";
        }

        Vector3 size = bounds.size;
        return $"size=({size.x:F2},{size.y:F2},{size.z:F2})";
    }

    private static void SetPterosaurGltfRenderersEnabled(GameObject model, bool enabled)
    {
        if (model == null)
        {
            return;
        }

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.transform == null)
            {
                continue;
            }

            if (renderer.transform.name.IndexOf("PterosaurVisibilityFallback", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            renderer.enabled = enabled;
        }
    }

    private static void EnsurePterosaurRenderersVisible(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = true;
            }
        }
    }

    private static void SetUnitPlaceholderVisible(BattleUnit unit, bool visible)
    {
        if (unit == null || unit.body == null)
        {
            return;
        }

        var renderers = unit.body.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || unit.modelInstance != null && renderer.transform.IsChildOf(unit.modelInstance.transform))
            {
                continue;
            }

            renderer.enabled = visible;
        }
    }

    private void ConfigureAnimatorPlayback(BattleUnit unit, GameObject model)
    {
        unit.animator = null;
        unit.animatorClips = null;
        unit.currentAnimatorClip = string.Empty;

        bool pterosaurAnimator = unit.kind == UnitKind.Aircraft && unit.combatVariant == UnitCombatVariant.Pterosaur;
        if ((unit.kind != UnitKind.Soldier && unit.kind != UnitKind.Tank && unit.kind != UnitKind.Giant && !pterosaurAnimator) || model == null)
        {
            return;
        }

        AnimationClip[] clips = CollectRuntimeAnimationClips(model);
        if (clips.Length == 0)
        {
            return;
        }

        if (pterosaurAnimator)
        {
            ConfigurePterosaurAnimatorPlayback(unit, model, clips);
            return;
        }

        if (unit.kind == UnitKind.Giant)
        {
            ConfigureGiantAnimatorPlayback(unit, model, clips);
            return;
        }

        for (int i = 0; i < unit.animations.Length; i++)
        {
            if (unit.animations[i] != null)
            {
                unit.animations[i].enabled = false;
            }
        }

        var animator = model.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            animator = model.GetComponent<Animator>();
        }

        if (animator == null)
        {
            animator = model.AddComponent<Animator>();
        }

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;

        unit.animator = animator;
        unit.animatorClips = clips;
    }

    private static bool PterosaurSupportsAnimatorPlayable(Animator animator)
    {
        return animator != null;
    }

    private void ConfigurePterosaurAnimatorPlayback(BattleUnit unit, GameObject model, AnimationClip[] clips)
    {
        if (unit.animations != null)
        {
            for (int i = 0; i < unit.animations.Length; i++)
            {
                if (unit.animations[i] != null)
                {
                    unit.animations[i].enabled = false;
                }
            }
        }

        if (model != null)
        {
            Animator[] animators = model.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i] != null)
                {
                    animators[i].enabled = false;
                    animators[i].applyRootMotion = false;
                }
            }
        }

        unit.animator = null;
        unit.animatorClips = clips;
        unit.animations = null;
        unit.currentAnimation = string.Empty;
    }

    private static void ConfigurePterosaurLegacyFlyingPlayback(BattleUnit unit, GameObject model, AnimationClip[] clips)
    {
        if (unit == null || model == null || clips == null || clips.Length == 0)
        {
            return;
        }

        Animator[] animators = model.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
            {
                animators[i].enabled = false;
            }
        }

        Animation animation = model.GetComponent<Animation>();
        if (animation == null)
        {
            animation = model.AddComponent<Animation>();
        }

        animation.enabled = true;
        animation.playAutomatically = false;
        animation.cullingType = AnimationCullingType.AlwaysAnimate;
        animation.animatePhysics = false;

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            string clipKey = clip.name;
            if (animation.GetClip(clipKey) != null)
            {
                continue;
            }

            AnimationClip runtimeClip = Instantiate(clip);
            runtimeClip.name = clipKey;
            runtimeClip.legacy = true;
            runtimeClip.wrapMode = WrapMode.Loop;
            animation.AddClip(runtimeClip, clipKey);
        }

        unit.animations = new[] { animation };
        unit.animator = null;
        unit.animatorClips = clips;
    }

    private static bool GiantModelSupportsAnimatorPlayable(GameObject model)
    {
        Animator animator = model != null ? model.GetComponentInChildren<Animator>(true) : null;
        return animator != null
            && animator.avatar != null
            && animator.avatar.isValid;
    }

    private static void ConfigureGiantAnimatorPlayback(BattleUnit unit, GameObject model, AnimationClip[] clips)
    {
        AnimationClip[] resolvedClips = clips.Length > 0 ? clips : CollectRuntimeAnimationClips(model);
        RuntimeAnimationClipStore clipStore = model.GetComponent<RuntimeAnimationClipStore>();
        bool forceLegacy = clipStore != null && clipStore.UseLegacyBoneAnimation;
        Animation host = FindGiantAnimationHost(model);
        if (forceLegacy || host != null)
        {
            ConfigureGiantLegacyAnimationPlayback(unit, model, resolvedClips, host);
            return;
        }

        if (GiantModelSupportsAnimatorPlayable(model))
        {
            ConfigureGiantPlayableAnimator(unit, model, resolvedClips);
            return;
        }

        ConfigureGiantLegacyAnimationPlayback(unit, model, resolvedClips, host);
    }

    private static void ConfigureGiantPlayableAnimator(BattleUnit unit, GameObject model, AnimationClip[] clips)
    {
        Animation[] legacyAnimations = model.GetComponentsInChildren<Animation>(true);
        for (int i = 0; i < legacyAnimations.Length; i++)
        {
            if (legacyAnimations[i] != null)
            {
                legacyAnimations[i].enabled = false;
            }
        }

        Animator animator = model.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            animator = model.AddComponent<Animator>();
        }

        animator.enabled = true;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;

        unit.animations = legacyAnimations;
        unit.animator = animator;
        unit.animatorClips = clips;
    }

    private static void ConfigureGiantLegacyAnimationPlayback(BattleUnit unit, GameObject model, AnimationClip[] clips, Animation host = null)
    {
        Animator[] animators = model.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
            {
                animators[i].enabled = false;
            }
        }

        Animation animation = host ?? FindGiantAnimationHost(model);
        if (animation == null)
        {
            animation = model.AddComponent<Animation>();
        }

        animation.enabled = true;
        animation.playAutomatically = false;
        animation.cullingType = AnimationCullingType.AlwaysAnimate;
        animation.animatePhysics = false;

        bool hostHasStates = false;
        foreach (AnimationState state in animation)
        {
            if (state != null && state.clip != null)
            {
                hostHasStates = true;
                state.wrapMode = WrapMode.Loop;
            }
        }

        if (!hostHasStates)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip == null)
                {
                    continue;
                }

                string clipKey = clip.name;
                if (animation.GetClip(clipKey) != null)
                {
                    continue;
                }

                AnimationClip runtimeClip = Instantiate(clip);
                runtimeClip.name = clipKey;
                runtimeClip.legacy = true;
                runtimeClip.wrapMode = WrapMode.Loop;
                animation.AddClip(runtimeClip, clipKey);
            }
        }

        unit.animations = new[] { animation };
        unit.animator = null;
        unit.animatorClips = CollectGiantHostAnimationClips(animation, clips);
    }

    private static AnimationClip[] CollectGiantHostAnimationClips(Animation host, AnimationClip[] fallbackClips)
    {
        var clips = new List<AnimationClip>();
        if (host != null)
        {
            if (host.clip != null)
            {
                clips.Add(host.clip);
            }

            foreach (AnimationState state in host)
            {
                if (state != null && state.clip != null && !ContainsClip(clips, state.clip))
                {
                    clips.Add(state.clip);
                }
            }
        }

        if (clips.Count == 0 && fallbackClips != null)
        {
            for (int i = 0; i < fallbackClips.Length; i++)
            {
                AddUniqueGiantAnimationClip(clips, fallbackClips[i], legacyOnly: true);
            }
        }

        return clips.ToArray();
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

            var storeClips = stores[i].UseLegacyBoneAnimation
                ? stores[i].Clips
                : stores[i].AnimatorClips;
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
            if (unit.combatVariant == UnitCombatVariant.Pterosaur)
            {
                ConfigurePterosaurWingRig(unit);
                return;
            }

            if (!TryBindAircraftRotorRig(unit))
            {
                ConfigureAircraftRotorRig(unit);
            }
            return;
        }

        if (unit.kind == UnitKind.Tank && unit.combatVariant != UnitCombatVariant.RocketTruck)
        {
            ConfigureTankMotionRig(unit, UsesAnimatorPlayback(unit));
        }
    }

    private void ConfigureAircraftRotorRig(BattleUnit unit)
    {
        var root = new GameObject("AircraftRotorMotionRig");
        root.transform.SetParent(unit.body, false);
        root.transform.localPosition = new Vector3(0f, AircraftModelTargetHeight * 0.95f, 0f);

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
        RegisterAircraftRotor(unit, root.transform, Vector3.up, 1f);
    }

    private bool TryBindAircraftRotorRig(BattleUnit unit)
    {
        if (unit == null || unit.modelInstance == null)
        {
            return false;
        }

        Transform mainRotor = null;
        Transform tailRotor = null;
        int mainScore = 0;
        int tailScore = 0;
        var transforms = unit.modelInstance.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate == unit.modelInstance.transform)
            {
                continue;
            }

            int candidateMainScore = AircraftMainRotorNameScore(candidate);
            if (candidateMainScore > mainScore)
            {
                mainRotor = candidate;
                mainScore = candidateMainScore;
            }

            int candidateTailScore = AircraftTailRotorNameScore(candidate);
            if (candidateTailScore > tailScore)
            {
                tailRotor = candidate;
                tailScore = candidateTailScore;
            }
        }

        if (mainRotor != null)
        {
            RegisterAircraftRotor(unit, mainRotor, Vector3.up, 1f);
        }

        if (tailRotor != null && tailRotor != mainRotor)
        {
            RegisterAircraftRotor(unit, tailRotor, Vector3.up, -2.4f);
        }

        return unit.aircraftRotorRigs.Count > 0;
    }

    private static int AircraftMainRotorNameScore(Transform transform)
    {
        if (transform == null || string.IsNullOrEmpty(transform.name))
        {
            return 0;
        }

        string lower = transform.name.ToLowerInvariant();
        if (lower.EndsWith("_end", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        bool parentRotor = transform.GetComponent<Renderer>() == null && transform.GetComponentsInChildren<Renderer>(true).Length > 0;
        if (string.Equals(lower, "heli", StringComparison.OrdinalIgnoreCase))
        {
            return parentRotor ? 170 : 115;
        }

        if (lower.Contains("main") && lower.Contains("rotor"))
        {
            return 130;
        }

        if (lower.Contains("rotor"))
        {
            return 120;
        }

        if (lower.Contains("propeller") || lower.Contains("helice"))
        {
            return 110;
        }

        if (lower.Contains("main") && lower.Contains("prop"))
        {
            return 108;
        }

        if (lower.Contains("blade"))
        {
            return 95;
        }

        if (lower.Contains("prop") && !lower.Contains("property"))
        {
            return 80;
        }

        return 0;
    }

    private static int AircraftTailRotorNameScore(Transform transform)
    {
        if (transform == null || string.IsNullOrEmpty(transform.name))
        {
            return 0;
        }

        string lower = transform.name.ToLowerInvariant();
        if (lower.EndsWith("_end", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        bool parentRotor = transform.GetComponent<Renderer>() == null && transform.GetComponentsInChildren<Renderer>(true).Length > 0;
        if (string.Equals(lower, "tail", StringComparison.OrdinalIgnoreCase))
        {
            return parentRotor ? 160 : 105;
        }

        if (lower.Contains("tail") && (lower.Contains("rotor") || lower.Contains("prop") || lower.Contains("blade")))
        {
            return parentRotor ? 150 : 100;
        }

        return 0;
    }

    private static void RegisterAircraftRotor(BattleUnit unit, Transform rotor, Vector3 localAxis, float speedMultiplier)
    {
        if (unit == null || rotor == null)
        {
            return;
        }

        var rig = new AircraftRotorRig
        {
            rotor = rotor,
            baseLocalRotation = rotor.localRotation,
            localAxis = localAxis.sqrMagnitude > 0.001f ? localAxis.normalized : Vector3.up,
            speedMultiplier = speedMultiplier,
        };

        unit.aircraftRotorRigs.Add(rig);
        if (unit.aircraftRotorRoot == null)
        {
            unit.aircraftRotorRoot = rotor;
            unit.aircraftRotorBaseLocalRotation = rotor.localRotation;
        }
    }

    private void ConfigureTankMotionRig(BattleUnit unit, bool animatorDrivenTracks)
    {
        var rig = new TankMotionRig();
        CollectTankAimParts(unit, rig);

        if (!animatorDrivenTracks && !HasRealisticTankInResources())
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
        if (unit != null && unit.combatVariant == UnitCombatVariant.Pterosaur && pterosaurPrototype != null)
        {
            return pterosaurPrototype;
        }

        if (unit != null && unit.combatVariant == UnitCombatVariant.RocketTruck && rocketTruckPrototype != null)
        {
            return rocketTruckPrototype;
        }

        if (unit.kind == UnitKind.Tank)
        {
            if (HasRealisticTankInResources())
            {
                if (unit.tankModel == TankModelVariant.T55AK && tankT55AkPrototype != null)
                {
                    return tankT55AkPrototype;
                }

                if (modelPrototypes.TryGetValue(UnitKind.Tank, out GameObject realisticT55a) && realisticT55a != null)
                {
                    return realisticT55a;
                }
            }
            else if (unit.tankModel == TankModelVariant.T55AK && tankT55AkPrototype != null)
            {
                return tankT55AkPrototype;
            }
            else if (unit.tankModel == TankModelVariant.T55A
                && modelPrototypes.TryGetValue(UnitKind.Tank, out GameObject t55aPrototype)
                && t55aPrototype != null)
            {
                return t55aPrototype;
            }
            else if (tankVariantPrototypes.Count > 0)
            {
                int variantIndex = Mathf.Abs(unit.id) % tankVariantPrototypes.Count;
                return tankVariantPrototypes[variantIndex];
            }
        }

        if (unit.kind == UnitKind.Giant && giantVariantPrototypes.Count > 0)
        {
            int variantIndex = Mathf.Abs(unit.id + unit.rank) % giantVariantPrototypes.Count;
            return giantVariantPrototypes[variantIndex];
        }

        GameObject prototype;
        return modelPrototypes.TryGetValue(unit.kind, out prototype) ? prototype : null;
    }

    private static void StripImportedModelStrayComponents(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        var audioSources = root.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
            {
                Destroy(audioSources[i]);
            }
        }

        var listeners = root.GetComponentsInChildren<AudioListener>(true);
        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] != null)
            {
                Destroy(listeners[i]);
            }
        }

        var lights = root.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
            {
                Destroy(lights[i]);
            }
        }
    }

    private void ConfigureRuntimeModel(GameObject model, BattleUnit unit)
    {
        UnitKind kind = unit != null ? unit.kind : UnitKind.Soldier;
        bool isPterosaur = unit != null && unit.combatVariant == UnitCombatVariant.Pterosaur;
        bool isRocketTruck = unit != null && unit.combatVariant == UnitCombatVariant.RocketTruck;
        StripImportedModelStrayComponents(model);

        if (kind == UnitKind.Tank && !isRocketTruck)
        {
            RemoveTankDisplayGeometry(model);
            if (HasRealisticTankInResources())
            {
                ApplyRealisticTankMaterials(model);
            }
        }

        if (isRocketTruck)
        {
            ApplyRocketTruckPresentation(model);
        }

        if (kind == UnitKind.Aircraft)
        {
            RemoveAircraftStrayGeometry(model);
            if (!isPterosaur)
            {
                if (HasRealisticAircraftInResources())
                {
                    ApplyRealisticAircraftMaterials(model);
                }
                else
                {
                    ApplyAircraftHelicopterMaterials(model);
                }
            }
        }

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

        if (kind == UnitKind.Tank && isRocketTruck)
        {
            FitModelToHeight(model, RocketTruckModelTargetHeight, kind);
            GroundTankModelOnTerrain(model);
            return;
        }

        if (kind == UnitKind.Tank && HasRealisticTankInResources())
        {
            GroundTankModelOnTerrain(model);
            return;
        }

        float targetHeight = Poses[kind].TargetHeight;
        if (isPterosaur)
        {
            if (PterosaurModelIsProceduralBattleMesh(model))
            {
                ApplyPterosaurProceduralBattleMaterials(model);
            }
            else
            {
                ApplyPterosaurGltfTextures(model, PterosaurPteranodonResourceModelPath);
            }

            FitPterosaurRuntimeModel(model);
        }
        else
        {
            FitModelToHeight(model, targetHeight, kind);
        }

        if (kind == UnitKind.Soldier)
        {
            ConfigureSoldierWeaponPresentation(model);
            AlignModelBottomToLocalOrigin(model);
        }
    }

    private static bool UnitUsesGroundAltitude(BattleUnit unit)
    {
        return unit != null
            && unit.kind != UnitKind.Aircraft
            && unit.combatVariant != UnitCombatVariant.Pterosaur;
    }

    private void GroundTankModelOnTerrain(GameObject model)
    {
        if (!TryComputeModelBounds(model, out Bounds bounds, includeTankDisplayGeometry: false))
        {
            return;
        }

        model.transform.localPosition += new Vector3(0f, -bounds.min.y, 0f);
    }

    private static void AlignModelBottomToLocalOrigin(GameObject model)
    {
        if (model == null || !TryComputeModelBoundsInRootLocalSpace(model, out Bounds bounds))
        {
            return;
        }

        float bottom = bounds.min.y;
        if (Mathf.Abs(bottom) > 0.001f)
        {
            model.transform.localPosition += new Vector3(0f, -bottom, 0f);
        }
    }

    private static void FitPterosaurRuntimeModel(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        if (!TryComputeModelBoundsInRootLocalSpace(model, out Bounds bounds))
        {
            return;
        }

        float currentHeight = GetPterosaurBoundsHeight(model, bounds);
        if (Mathf.Abs(currentHeight - PterosaurModelTargetHeight) > 0.08f)
        {
            float uniformScale = PterosaurModelTargetHeight / Mathf.Max(0.02f, currentHeight);
            uniformScale = Mathf.Clamp(uniformScale, 0.12f, 6f);
            model.transform.localScale *= uniformScale;
            if (!TryComputeModelBoundsInRootLocalSpace(model, out bounds))
            {
                return;
            }
        }

        AlignAirUnitModelForCruise(model, UnitCombatVariant.Pterosaur);
    }

    private void FitModelToHeight(GameObject model, float targetHeight, UnitKind kind = UnitKind.Soldier)
    {
        if (!TryComputeModelBounds(model, out Bounds bounds))
        {
            return;
        }

        float currentHeight = kind == UnitKind.Tank
            ? GetTankBoundsMetric(bounds)
            : kind == UnitKind.Aircraft
                ? GetAircraftBoundsMetric(model, bounds)
                : Mathf.Max(0.001f, bounds.size.y);
        if (Mathf.Abs(currentHeight - targetHeight) < 0.08f)
        {
            ApplyModelGroundLift(model, kind);
            return;
        }

        float uniformScale = targetHeight / currentHeight;
        model.transform.localScale = model.transform.localScale * uniformScale;
        ApplyModelGroundLift(model, kind);
    }

    private static void ApplyModelGroundLift(GameObject model, UnitKind kind)
    {
        if (kind == UnitKind.Aircraft)
        {
            AlignAirUnitModelForCruise(model, UnitCombatVariant.Standard);
            return;
        }

        float maxLift = kind == UnitKind.Soldier ? 1.25f : kind == UnitKind.Giant ? 2.5f : 4f;

        if (TryComputeModelBoundsInRootLocalSpace(model, out Bounds groundedBounds))
        {
            float lift = Mathf.Clamp(-groundedBounds.min.y, 0f, maxLift);
            model.transform.localPosition += new Vector3(0f, lift, 0f);
            return;
        }

        if (TryComputeModelBounds(model, out Bounds bounds))
        {
            float lift = Mathf.Clamp(-bounds.min.y, 0f, maxLift);
            model.transform.localPosition += new Vector3(0f, lift, 0f);
        }
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
        ResetTestBattleDeathCounter();
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

        ResetSoldiers();
        ResetTanks();
        ResetAircraft();
        ResetPterosaurs();
        ResetRocketGiants();
        ResetGiants();
        RefreshHud();
        ShowBanner("特殊兵种：翼龙×20 | 火箭丧尸×20 | 火箭炮车×18 已上阵", false, 2.6f);
    }

    private void ResetSoldiers()
    {
        for (int i = 0; i < soldiers.Count; i++)
        {
            var unit = soldiers[i];
            if (i >= SoldierCount)
            {
                DeactivatePooledUnit(unit);
                continue;
            }

            unit.combatVariant = UnitCombatVariant.Standard;
            unit.kind = UnitKind.Soldier;
            DetachUnitModelInstance(unit);
            GetHumanSoldierMassSpawn(i, out float x, out float z);
            ActivateUnit(unit, x, z, soldierConfig.MaxHp, soldierConfig.Damage, soldierConfig.MoveSpeed + Noise(i + 73f) * 8f, soldierConfig.Radius, soldierConfig.AttackRange + Noise(i + 101f) * 34f, soldierConfig.AttackInterval + Noise(i + 131f) * 0.22f, i, 1, 0f);
            unit.headingDegrees = DirectionYawDegrees(
                BeastCastleGateX - x,
                BeastCastleCenterZ - z,
                unit.headingDegrees);
            unit.turretYawDegrees = unit.headingDegrees;
            EnsureUnitModelAttached(unit);
        }
    }

    private void ResetTanks()
    {
        int activeTanks = TankCount + RocketTruckCount;
        for (int i = 0; i < tanks.Count; i++)
        {
            if (i >= activeTanks)
            {
                DeactivatePooledUnit(tanks[i]);
                continue;
            }

            var unit = tanks[i];
            unit.kind = UnitKind.Tank;
            DetachUnitModelInstance(unit);
            bool rocketTruck = i >= TankCount;
            unit.combatVariant = rocketTruck ? UnitCombatVariant.RocketTruck : UnitCombatVariant.Standard;
            int spawnIndex = rocketTruck ? i - TankCount : i;
            if (rocketTruck)
            {
                GetHumanRocketTruckMassSpawn(spawnIndex, out float x, out float z);
                ActivateUnit(
                    unit,
                    x,
                    z,
                    tankConfig.MaxHp * 0.92f,
                    tankConfig.Damage * 1.35f,
                    tankConfig.MoveSpeed * RocketTruckMoveSpeedRatio + Noise(i + 401f) * 2f,
                    tankConfig.Radius * 1.05f,
                    tankConfig.AttackRange + RocketTruckAttackRangeBonus,
                    tankConfig.AttackInterval * 1.15f + Noise(i + 503f) * 0.25f,
                    TankCount + spawnIndex,
                    1,
                    0f);
                unit.modelYawOffset = RocketTruckMeshYawOffset;
                unit.headingDegrees = DirectionYawDegrees(
                    BeastCastleGateX - x,
                    BeastCastleCenterZ - z,
                    unit.headingDegrees);
                unit.turretYawDegrees = unit.headingDegrees;
                EnsureUnitModelAttached(unit);
            }
            else
            {
                GetHumanTankMassSpawn(spawnIndex, out float x, out float z);
                ActivateUnit(unit, x, z, tankConfig.MaxHp, tankConfig.Damage, tankConfig.MoveSpeed + Noise(i + 401f) * 6f, tankConfig.Radius, tankConfig.AttackRange, tankConfig.AttackInterval + Noise(i + 503f) * 0.3f, i, 1, 0f);
                unit.headingDegrees = DirectionYawDegrees(
                    BeastCastleGateX - x,
                    BeastCastleCenterZ - z,
                    unit.headingDegrees);
                unit.turretYawDegrees = unit.headingDegrees;
                EnsureUnitModelAttached(unit);
            }
        }
    }

    private void ResetAircraft()
    {
        for (int i = 0; i < aircraft.Count; i++)
        {
            if (i >= AircraftCount)
            {
                DeactivatePooledUnit(aircraft[i]);
                continue;
            }

            var unit = aircraft[i];
            unit.combatVariant = UnitCombatVariant.Standard;
            unit.kind = UnitKind.Aircraft;
            DetachUnitModelInstance(unit);
            GetHumanAircraftMassSpawn(i, out float x, out float z);
            ActivateUnit(unit, x, z, aircraftConfig.MaxHp, aircraftConfig.Damage, aircraftConfig.MoveSpeed + i * 7f, aircraftConfig.Radius, aircraftConfig.AttackRange, aircraftConfig.AttackInterval + i * 0.12f, i, 1, AircraftDefaultAltitude);
        }
    }

    private void ResetGiants()
    {
        pendingGiantBattleActivation = 0;
        for (int i = 0; i < giants.Count; i++)
        {
            DeactivatePooledUnit(giants[i]);
        }

        pendingGiantBattleActivation = BaseGiantCount;
    }

    private void ProcessPendingGiantBattleActivation()
    {
        if (pendingGiantBattleActivation <= 0)
        {
            return;
        }

        int start = BaseGiantCount - pendingGiantBattleActivation;
        int batch = Mathf.Min(GiantBattleActivationBatchSize, pendingGiantBattleActivation);
        for (int b = 0; b < batch; b++)
        {
            int i = start + b;
            if (i >= BaseGiantCount)
            {
                pendingGiantBattleActivation = 0;
                return;
            }

            GetGiantMassSpawn(i, out float x, out float z);
            var giant = giants[i];
            giant.combatVariant = UnitCombatVariant.Standard;
            ActivateUnit(giant, x, z, giantConfig.MaxHp, giantConfig.Damage, giantConfig.MoveSpeed + Noise(i + 207f) * 4f, giantConfig.Radius, giantConfig.AttackRange, giantConfig.AttackInterval + Noise(i + 307f) * 0.18f, i, -1, 0f);
            giant.attackCooldown = 2.2f + Noise(i + 907f) * 1.4f;
        }

        pendingGiantBattleActivation -= batch;
    }

    private void ActivateUnit(BattleUnit unit, float x, float z, float hp, float damage, float speed, float radius, float range, float interval, int rank, int facing, float altitude)
    {
        unit.active = true;
        unit.runtimeState = UnitRuntimeState.Idle;
        unit.root.SetActive(true);
        unit.x = x;
        unit.z = z;
        unit.visualX = x;
        unit.visualZ = z;
        unit.baseZ = z;
        unit.hp = hp;
        unit.maxHp = hp;
        unit.damage = damage;
        unit.baseSpeed = speed;
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
        unit.altitude = unit.kind == UnitKind.Aircraft && altitude <= 0.01f
            ? AircraftDefaultAltitude
            : unit.kind == UnitKind.Aircraft
                ? altitude
                : 0f;
        unit.headingDegrees = facing < 0 ? -90f : 90f;
        unit.turretYawDegrees = unit.headingDegrees;
        unit.moveSpeed = 0f;
        unit.rotorSpinDegrees = Noise(unit.id + rank * 7.1f) * 360f;
        unit.wheelSpinDegrees = Noise(unit.id + rank * 5.3f) * 360f;
        unit.trackScroll = 0f;
        unit.animationPresentationKey = -1;
        unit.infectionTimer = 0f;
        unit.burnTimer = 0f;
        unit.burnTickTimer = 0f;
        unit.burnDamagePerTick = 0f;
        unit.preInfectionFaction = FactionId.Neutral;
        if (unit.faction == FactionId.Neutral)
        {
            unit.faction = unit.team == TeamKind.Giant ? FactionId.Zombie : FactionId.Blue;
        }

        EnsureUnitModelAttached(unit);

        UpdateUnitTransform(unit, 0f);
        PlayUnitAnimation(unit);
    }

    private void EnsureUnitModelAttached(BattleUnit unit)
    {
        if (unit == null || !unit.active || unit.body == null)
        {
            return;
        }

        if (unit.combatVariant == UnitCombatVariant.Pterosaur && pterosaurPrototype == null)
        {
            SetupPterosaurBattlePrototype();
        }

        if (unit.modelInstance != null)
        {
            return;
        }

        if (!TryAttachCachedUnitModel(unit))
        {
            AttachUnitModel(unit);
        }

        if (unit.combatVariant == UnitCombatVariant.Pterosaur)
        {
            EnsurePterosaurUnitDisplay(unit);
        }

        if (unit.kind == UnitKind.Giant && unit.combatVariant == UnitCombatVariant.RocketGiant && unit.modelInstance != null)
        {
            AttachGiantRocketLauncher(unit.modelInstance);
        }
    }

    private void DeactivatePooledUnit(BattleUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        unit.active = false;
        unit.runtimeState = UnitRuntimeState.Inactive;
        unit.hp = 0f;
        unit.maxHp = 0f;
        unit.attackVisualTimer = 0f;
        unit.hitFlashTimer = 0f;
        unit.attackCooldown = 0f;
        unit.moveSpeed = 0f;
        unit.animationPresentationKey = -1;
        unit.altitude = unit.kind == UnitKind.Aircraft ? AircraftDefaultAltitude : 0f;
        DetachUnitModelInstance(unit);
        if (unit.root != null)
        {
            unit.root.SetActive(false);
        }
    }

    private bool ReviveSoldierFromDanmu(DanmuCommand command)
    {
        var unit = FindInactiveUnit(soldiers);
        if (unit == null)
        {
            return false;
        }

        int soldierIndex = CountActive(soldiers);
        GetHumanSoldierMassSpawn(soldierIndex, out float x, out float z);
        ActivateUnit(unit, x, z, soldierConfig.MaxHp + 4f, soldierConfig.Damage + 1f, soldierConfig.MoveSpeed + 6f, soldierConfig.Radius, soldierConfig.AttackRange + 26f, soldierConfig.AttackInterval - 0.08f, soldierIndex, 1, 0f);
        unit.headingDegrees = DirectionYawDegrees(
            BeastCastleGateX - x,
            BeastCastleCenterZ - z,
            unit.headingDegrees);
        unit.turretYawDegrees = unit.headingDegrees;
        EnsureUnitModelAttached(unit);
        PlayDanmuSpawnEffect(BattleEffectId.HumanSummon, x, z, 0.92f);
        return true;
    }

    private bool ReviveTankFromDanmu(DanmuCommand command)
    {
        var unit = FindInactiveUnit(tanks);
        if (unit == null)
        {
            return false;
        }

        int tankIndex = CountActive(tanks);
        int rank = tankIndex / HumanFormationTanksPerRow;
        GetHumanTankMassSpawn(tankIndex, out float x, out float z);
        ActivateUnit(unit, x, z, tankConfig.MaxHp + 40f, tankConfig.Damage + 7f, tankConfig.MoveSpeed + 5f, tankConfig.Radius, tankConfig.AttackRange + 20f, tankConfig.AttackInterval - 0.1f, rank, 1, 0f);
        PlayDanmuSpawnEffect(BattleEffectId.HumanSummon, x, z, 1.0f);
        return true;
    }

    private bool ReviveAircraftFromDanmu(DanmuCommand command)
    {
        var unit = FindInactiveUnit(aircraft);
        if (unit == null)
        {
            return false;
        }

        int airIndex = CountActive(aircraft);
        GetHumanAircraftMassSpawn(airIndex, out float x, out float z);
        ActivateUnit(unit, x, z, aircraftConfig.MaxHp + 24f, aircraftConfig.Damage + 5f, aircraftConfig.MoveSpeed + 12f, aircraftConfig.Radius, aircraftConfig.AttackRange + 26f, aircraftConfig.AttackInterval - 0.08f, airIndex, 1, AircraftDefaultAltitude);
        PlayDanmuSpawnEffect(BattleEffectId.HumanSummon, x, z, 1.05f);
        return true;
    }

    private bool ReviveGiantFromDanmu(DanmuCommand command)
    {
        var unit = FindInactiveUnit(giants);
        if (unit == null)
        {
            return false;
        }

        GetGiantMassSpawn(CountActive(giants), out float x, out float z);
        ActivateUnit(unit, x, z, giantConfig.MaxHp, giantConfig.Damage, giantConfig.MoveSpeed + 5f, giantConfig.Radius, giantConfig.AttackRange, giantConfig.AttackInterval, processedDanmuCommandCount, -1, 0f);
        unit.attackCooldown = 0.3f;
        PlayDanmuSpawnEffect(BattleEffectId.OrcSummon, x, z, 1.15f);
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

    private void PrewarmBattlePools()
    {
        PrewarmAircraftBombMesh();
        RebuildBombProjectilePool();
        RebuildRocketProjectilePool();

        if (projectiles.Count > 0 || effects.Count > 0 || deathVisuals.Count > 0)
        {
            return;
        }

        PrewarmProjectiles(ProjectileKind.Bullet, PrewarmBulletProjectiles, new Color(1f, 0.82f, 0.32f, 1f));
        PrewarmProjectiles(ProjectileKind.Shell, PrewarmShellProjectiles, new Color(0.58f, 0.56f, 0.52f, 0.9f));
        PrewarmProjectiles(ProjectileKind.Bomb, PrewarmBombProjectiles, AircraftBombVisualColor);
        PrewarmProjectiles(ProjectileKind.Rock, PrewarmRockProjectiles, new Color(0.72f, 1f, 0.52f, 1f));
        PrewarmProjectiles(ProjectileKind.Rocket, PrewarmRocketProjectiles, TacticalRocketVisualColor);
        PrewarmPterosaurFireballProjectiles();
        InitializeNuclearWarheadVisual();

        PrewarmFallbackEffectViews(PrewarmFallbackEffects);

        PrewarmDeathVisuals(UnitKind.Soldier, 24);
        PrewarmDeathVisuals(UnitKind.Tank, 8);
        PrewarmDeathVisuals(UnitKind.Aircraft, 4);
        PrewarmDeathVisuals(UnitKind.Giant, 8);
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
        if (TryPlayBattleEffect(effectId, ToWorldPoint(x, z, 0.12f), Quaternion.identity, scale))
        {
            return;
        }

        SpawnEffect(x, z + 18f, scale, EffectKind.Smoke, 0.32f);
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
            if (effects.Count >= MaxEffects)
            {
                return;
            }

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

        float yaw = unit.kind == UnitKind.Soldier || unit.kind == UnitKind.Tank || unit.kind == UnitKind.Giant ? unit.headingDegrees : unit.facing < 0 ? -90f : 90f;
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
                visual.root.transform.localScale = Vector3.one * AircraftVisualScale;
                visual.velocity = new Vector3(unit.facing * 0.9f, -2.6f, -0.35f + Noise(unit.id + 71f) * 0.7f);
                break;
            case UnitKind.Giant:
                visual.root.transform.rotation = Quaternion.Euler(0f, yaw + GiantPixelhouseMeshYawOffset, 0f);
                visual.root.transform.localScale = Vector3.one;
                visual.velocity = Vector3.zero;
                GroundDeathVisualRoot(visual.root, 0.04f);
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
                if (!TryBuildGiantDeathVisual(root.transform))
                {
                    CreateDeathVisualPart(root.transform, PrimitiveType.Capsule, "MonsterFallenBody", new Vector3(0f, 0.42f, 0f), new Vector3(0.82f, 1.35f, 0.82f), Quaternion.Euler(0f, 0f, 90f), new Color(0.20f, 0.12f, 0.10f, 1f));
                    CreateDeathVisualPart(root.transform, PrimitiveType.Sphere, "MonsterFallenHead", new Vector3(0.92f, 0.46f, 0f), new Vector3(0.44f, 0.44f, 0.44f), Quaternion.identity, new Color(0.28f, 0.16f, 0.12f, 1f));
                    CreateDeathVisualPart(root.transform, PrimitiveType.Cube, "MonsterFallenArm", new Vector3(0.05f, 0.22f, 0.52f), new Vector3(1.05f, 0.18f, 0.24f), Quaternion.Euler(0f, 0f, 15f), new Color(0.18f, 0.10f, 0.08f, 1f));
                }

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
                    PlayBattleEffect(BattleEffectId.TankWreckSmoke, position, 0.52f, Quaternion.identity);
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
            PlayBattleEffect(BattleEffectId.AircraftCrashSmoke, position, 0.72f, Quaternion.identity);
            PlayBattleEffect(BattleEffectId.ShellExplosionSmall, position, 0.78f, Quaternion.identity);
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
        float rawSpeed = Mathf.Sqrt(dx * dx + dz * dz) / Mathf.Max(0.001f, dt);
        if (unit.kind == UnitKind.Tank)
        {
            unit.moveSpeed = Mathf.Min(rawSpeed, unit.speed * 1.15f);
        }
        else
        {
            unit.moveSpeed = rawSpeed;
        }
    }

    private static bool UsesEngagementHeading(UnitKind kind)
    {
        return kind == UnitKind.Soldier || kind == UnitKind.Tank || kind == UnitKind.Aircraft || kind == UnitKind.Giant;
    }

    private void UpdateUnitTransform(BattleUnit unit, float dt)
    {
        if (unit.root == null)
        {
            return;
        }

        if (unit.kind == UnitKind.Tank)
        {
            unit.visualX = unit.x;
            unit.visualZ = unit.z;
        }

        unit.root.transform.position = ToWorldPoint(unit.x, unit.z, 0f);

        bool animatorMotion = UsesAnimatorPlayback(unit);
        bool tankUsesMotionRig = unit.kind == UnitKind.Tank && unit.tankMotionRig != null;
        float moveFactor = Mathf.Clamp01(unit.moveSpeed / Mathf.Max(1f, unit.speed * 0.75f));
        float cycle = unit.animTimer * MotionCycleSpeed(unit.kind, moveFactor) + unit.seed * Mathf.PI * 2f;
        float airPhase = battleTime > 0.01f ? battleTime : unit.animTimer;
        float bob = unit.kind == UnitKind.Aircraft
            ? Mathf.Sin(airPhase * 4.8f + unit.seed * 12f) * AirUnitAltitudeBobAmplitude
            : unit.kind == UnitKind.Soldier && !animatorMotion
                ? Mathf.Abs(Mathf.Sin(cycle)) * 0.045f * moveFactor
                : unit.kind == UnitKind.Giant && !UnitUsesGiantSkinnedLocomotion(unit)
                    ? Mathf.Abs(Mathf.Sin(cycle)) * 0.04f * moveFactor
                    : unit.kind == UnitKind.Tank
                        ? (tankUsesMotionRig ? 0f : Mathf.Sin(cycle * 0.45f) * 0.006f * moveFactor)
                        : 0f;

        float bodyAltitude = UnitUsesGroundAltitude(unit) ? 0f : unit.altitude;
        if (UnitUsesGroundAltitude(unit) && unit.altitude > 0.01f)
        {
            unit.altitude = 0f;
        }

        unit.body.localPosition = new Vector3(0f, bodyAltitude + bob, 0f);
        if (unit.kind == UnitKind.Tank)
        {
            unit.body.localRotation = Quaternion.Euler(0f, unit.headingDegrees + unit.modelYawOffset, 0f);
        }
        else if (unit.kind == UnitKind.Aircraft)
        {
            unit.body.localRotation = Quaternion.Euler(0f, unit.headingDegrees + AircraftEngagementYawOffset, 0f);
        }
        else if (unit.kind == UnitKind.Giant || unit.kind == UnitKind.Soldier)
        {
            unit.body.localRotation = Quaternion.Euler(0f, unit.headingDegrees, 0f);
        }
        else
        {
            unit.body.localRotation = Quaternion.identity;
        }

        if (unit.tankAimRoot != null)
        {
            float turretRelativeYaw = Mathf.DeltaAngle(unit.headingDegrees, unit.turretYawDegrees);
            unit.tankAimRoot.localRotation = Quaternion.Euler(0f, turretRelativeYaw, 0f);
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

        if (unit.modelInstance)
        {
            var pose = Poses[unit.kind];
            float mirrorYaw = pose.MirrorWithFacing && unit.facing < 0 ? 180f : 0f;
            float wobble = unit.kind == UnitKind.Aircraft
                ? Mathf.Sin(airPhase * 3.2f + unit.seed * 11f) * 4f
                : 0f;
            float hitBoost = unit.kind == UnitKind.Giant && unit.hitFlashTimer > 0f ? 1.08f : 1f;
            float attackBoost = unit.attackVisualTimer > 0f ? (unit.kind == UnitKind.Giant ? 1.04f : 1.02f) : 1f;
            float modelYaw = unit.kind == UnitKind.Tank
                ? (tankUsesMotionRig ? 0f : wobble)
                : unit.kind == UnitKind.Aircraft && unit.combatVariant == UnitCombatVariant.Pterosaur
                    ? PterosaurMeshYawOffset + wobble
                    : unit.kind == UnitKind.Aircraft
                        ? wobble
                : unit.kind == UnitKind.Soldier || unit.kind == UnitKind.Giant
                    ? wobble + (unit.kind == UnitKind.Giant
                        ? (UnitModelUsesAuthoredTextures(unit.modelInstance)
                            ? GiantPixelhouseMeshYawOffset
                            : GiantKenneyMeshYawOffset)
                        : 0f)
                : UsesEngagementHeading(unit.kind)
                        ? unit.headingDegrees + wobble
                        : pose.Yaw + mirrorYaw + wobble;
            if (unit.kind == UnitKind.Soldier && unit.soldierUsesVanguardMesh)
            {
                modelYaw += SoldierVanguardYawOffset;
            }

            Vector3 modelLocalPosition = unit.baseModelLocalPosition;
            float modelPitch = unit.combatVariant == UnitCombatVariant.Pterosaur ? 0f : pose.Pitch;
            float modelRoll = unit.combatVariant == UnitCombatVariant.Pterosaur ? 0f : pose.Roll;
            Quaternion facingRotation = Quaternion.Euler(modelPitch, modelYaw, modelRoll);
            Quaternion modelRotation = facingRotation * unit.baseModelLocalRotation;
            if (!animatorMotion && !UnitUsesGiantSkinnedLocomotion(unit)
                && !(unit.kind == UnitKind.Aircraft && unit.combatVariant == UnitCombatVariant.Pterosaur)
                && !(unit.kind == UnitKind.Tank && tankUsesMotionRig))
            {
                ApplyProceduralModelMotion(unit, cycle, moveFactor, ref modelLocalPosition, ref modelRotation);
            }
            else if (unit.kind == UnitKind.Giant)
            {
                modelLocalPosition = unit.baseModelLocalPosition;
            }
            if (UnitUsesGroundAltitude(unit))
            {
                float groundedY = unit.baseModelLocalPosition.y;
                if (modelLocalPosition.y > groundedY + 0.35f || modelLocalPosition.y < groundedY - 0.15f)
                {
                    modelLocalPosition.y = groundedY;
                }
            }

            unit.modelInstance.transform.localScale = unit.baseModelScale * hitBoost * attackBoost;
            unit.modelInstance.transform.localPosition = modelLocalPosition;
            unit.modelInstance.transform.localRotation = modelRotation;
            UpdateProceduralMotionRig(unit, dt, moveFactor);
            PlayUnitAnimation(unit);
            if (unit.combatVariant == UnitCombatVariant.Pterosaur)
            {
                UpdatePterosaurWingFlap(unit, moveFactor);
            }

            EnsureGiantLegacyAnimationPlaying(unit);
        }
        else if (unit.modelInstance == null)
        {
            if (battleTime > 0f)
            {
                TryAttachCachedUnitModel(unit);
            }
            else
            {
                AttachUnitModel(unit);
            }
        }
    }

    private float MotionCycleSpeed(UnitKind kind, float moveFactor)
    {
        switch (kind)
        {
            case UnitKind.Soldier:
                return Mathf.Lerp(5.8f, 10.8f, moveFactor);
            case UnitKind.Giant:
                return Mathf.Lerp(3.0f, 5.8f, moveFactor);
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
                // 轴向已在原型绑定；巡航只做水平面内轻微偏航/滚转，避免再绕 X 俯仰把机头扎向地面。
                localRotation *= Quaternion.Euler(
                    0f,
                    Mathf.Sin(battleTime * 2.8f + unit.seed * 6f) * 1.4f,
                    Mathf.Sin(battleTime * 3.5f + unit.seed * 5f) * 3.4f);
                break;
            }
        }
    }

    private static bool PterosaurUnitHasEmbeddedFlyClip(BattleUnit unit)
    {
        if (unit == null || unit.combatVariant != UnitCombatVariant.Pterosaur)
        {
            return false;
        }

        if (UsesAnimatorPlayback(unit))
        {
            return true;
        }

        return unit.animations != null
            && unit.animations.Length > 0
            && unit.animatorClips != null
            && unit.animatorClips.Length > 0
            && FindPterosaurFlyClip(unit.animatorClips) != null;
    }

    private static AnimationClip FindPterosaurFlyClip(AnimationClip[] clips)
    {
        if (clips == null)
        {
            return null;
        }

        string[] preferred =
        {
            "flying", "Flying", "Fly", "Flight", "Flap", "Glide", "Soar", "Hover", "Gliding", "Wing",
        };
        for (int p = 0; p < preferred.Length; p++)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip != null && string.Equals(clip.name, preferred[p], StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }
        }

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            string name = clip.name.ToLowerInvariant();
            if (name.Contains("fly") || name.Contains("flap") || name.Contains("glide") || name.Contains("soar"))
            {
                return clip;
            }
        }

        return clips.Length > 0 ? clips[0] : null;
    }

    private void ConfigurePterosaurWingRig(BattleUnit unit)
    {
        unit.pterosaurWingRigs.Clear();
        if (unit == null || unit.modelInstance == null)
        {
            return;
        }

        if (PterosaurUnitHasEmbeddedFlyClip(unit))
        {
            return;
        }

        TryBindPterosaurPrimaryWingRoots(unit);
        TryBindPterosaurWingTransform(unit, "Wing_L", -1f, 1f);
        TryBindPterosaurWingTransform(unit, "Wing_R", 1f, 1f);
        TryBindPterosaurWingTransform(unit, "WingFinger_L", -1f, 1.18f);
        TryBindPterosaurWingTransform(unit, "WingFinger_R", 1f, 1.18f);

        if (unit.pterosaurWingRigs.Count >= 2)
        {
            return;
        }

        Transform[] transforms = unit.modelInstance.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate == unit.modelInstance.transform)
            {
                continue;
            }

            string name = candidate.name;
            if (name.IndexOf("wing", StringComparison.OrdinalIgnoreCase) < 0
                || name.IndexOf("tail", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            float sideSign = ResolvePterosaurWingSideSign(candidate, name);
            if (Mathf.Approximately(sideSign, 0f))
            {
                continue;
            }

            bool finger = name.IndexOf("finger", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("phalange", StringComparison.OrdinalIgnoreCase) >= 0;
            bool membrane = name.IndexOf("membrane", StringComparison.OrdinalIgnoreCase) >= 0;
            float amp = finger ? 1.18f : membrane ? 1.08f : 1f;
            RegisterPterosaurWingRig(unit, candidate, sideSign, amp);
        }

        if (unit.pterosaurWingRigs.Count < 2)
        {
            TryBindPterosaurWingBonesFromSkinnedMeshes(unit);
        }

        if (unit.pterosaurWingRigs.Count == 0)
        {
            Debug.LogWarning(
                $"[ApocalypseKing] 翼龙未绑定到翅膀节点，无法程序化扇翼：{unit.modelInstance.name}。"
                + " 请确认 GLB 含 Wing_L/Wing_R 或名称带 wing 的子物体。");
        }
    }

    private static void TryBindPterosaurWingBonesFromSkinnedMeshes(BattleUnit unit)
    {
        if (unit == null || unit.modelInstance == null)
        {
            return;
        }

        SkinnedMeshRenderer[] skinnedMeshes = unit.modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int m = 0; m < skinnedMeshes.Length; m++)
        {
            SkinnedMeshRenderer skinned = skinnedMeshes[m];
            if (skinned == null || skinned.bones == null)
            {
                continue;
            }

            for (int i = 0; i < skinned.bones.Length; i++)
            {
                Transform bone = skinned.bones[i];
                if (bone == null)
                {
                    continue;
                }

                string name = bone.name;
                if (name.IndexOf("wing", StringComparison.OrdinalIgnoreCase) < 0
                    || name.IndexOf("tail", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                float sideSign = ResolvePterosaurWingSideSign(bone, name);
                if (Mathf.Approximately(sideSign, 0f))
                {
                    continue;
                }

                bool finger = name.IndexOf("finger", StringComparison.OrdinalIgnoreCase) >= 0;
                RegisterPterosaurWingRig(unit, bone, sideSign, finger ? 1.12f : 1f);
            }
        }
    }

    private static float ResolvePterosaurWingSideSign(Transform wing, string name)
    {
        string lower = name.ToLowerInvariant();
        if (lower.Contains("_l") || lower.EndsWith("_l") || lower.Contains("left") || lower.Contains("_l_"))
        {
            return -1f;
        }

        if (lower.Contains("_r") || lower.EndsWith("_r") || lower.Contains("right") || lower.Contains("_r_"))
        {
            return 1f;
        }

        return wing.localPosition.z < -0.02f ? -1f : wing.localPosition.z > 0.02f ? 1f : 0f;
    }

    private static Transform FindChildTransformByName(Transform root, string exactName)
    {
        if (root == null || string.IsNullOrEmpty(exactName))
        {
            return null;
        }

        if (string.Equals(root.name, exactName, StringComparison.Ordinal))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildTransformByName(root.GetChild(i), exactName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void TryBindPterosaurPrimaryWingRoots(BattleUnit unit)
    {
        if (unit == null || unit.modelInstance == null)
        {
            return;
        }

        Transform[] transforms = unit.modelInstance.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform bone = transforms[i];
            if (bone == null)
            {
                continue;
            }

            string name = bone.name;
            if (name.StartsWith("Lwing01", StringComparison.OrdinalIgnoreCase))
            {
                RegisterPterosaurWingRig(unit, bone, -1f, 1f);
            }
            else if (name.StartsWith("Rwing01", StringComparison.OrdinalIgnoreCase))
            {
                RegisterPterosaurWingRig(unit, bone, 1f, 1f);
            }
        }
    }

    private static void TryBindPterosaurWingTransform(BattleUnit unit, string exactName, float sideSign, float amplitudeScale)
    {
        if (unit == null || unit.modelInstance == null)
        {
            return;
        }

        Transform wing = FindChildTransformByName(unit.modelInstance.transform, exactName);
        if (wing != null)
        {
            RegisterPterosaurWingRig(unit, wing, sideSign, amplitudeScale);
        }
    }

    private static void RegisterPterosaurWingRig(BattleUnit unit, Transform wing, float sideSign, float amplitudeScale)
    {
        if (unit == null || wing == null)
        {
            return;
        }

        for (int i = 0; i < unit.pterosaurWingRigs.Count; i++)
        {
            if (unit.pterosaurWingRigs[i].wing == wing)
            {
                return;
            }
        }

        unit.pterosaurWingRigs.Add(new PterosaurWingRig
        {
            wing = wing,
            baseLocalRotation = wing.localRotation,
            sideSign = sideSign,
            amplitudeScale = amplitudeScale,
        });
    }

    private void UpdatePterosaurWingFlap(BattleUnit unit, float moveFactor)
    {
        if (unit == null
            || unit.combatVariant != UnitCombatVariant.Pterosaur
            || PterosaurUnitHasEmbeddedFlyClip(unit)
            || unit.pterosaurWingRigs.Count == 0)
        {
            return;
        }

        float phaseSource = battleTime > 0.01f ? battleTime : unit.animTimer;
        float speedBoost = Mathf.Lerp(0.92f, 1.18f, moveFactor);
        float phase = phaseSource * (Mathf.PI * 2f * PterosaurWingFlapFrequencyHz * speedBoost) + unit.seed * 1.73f;
        float flap = Mathf.Sin(phase) * PterosaurWingFlapDegrees;

        for (int i = 0; i < unit.pterosaurWingRigs.Count; i++)
        {
            PterosaurWingRig rig = unit.pterosaurWingRigs[i];
            if (rig == null || rig.wing == null)
            {
                continue;
            }

            float angle = flap * rig.sideSign * rig.amplitudeScale;
            rig.wing.localRotation = rig.baseLocalRotation * Quaternion.Euler(angle * 0.35f, 0f, angle);
        }
    }

    private void UpdateProceduralMotionRig(BattleUnit unit, float dt, float moveFactor)
    {
        if (unit == null || dt <= 0f)
        {
            return;
        }

        UpdatePterosaurWingFlap(unit, moveFactor);

        if (unit.aircraftRotorRigs != null && unit.aircraftRotorRigs.Count > 0)
        {
            unit.rotorSpinDegrees = Mathf.Repeat(unit.rotorSpinDegrees + dt * (1680f + moveFactor * 620f), 360f);
            for (int i = 0; i < unit.aircraftRotorRigs.Count; i++)
            {
                AircraftRotorRig rotor = unit.aircraftRotorRigs[i];
                if (rotor == null || rotor.rotor == null)
                {
                    continue;
                }

                rotor.rotor.localRotation = rotor.baseLocalRotation * Quaternion.AngleAxis(unit.rotorSpinDegrees * rotor.speedMultiplier, rotor.localAxis);
            }
        }

        var rig = unit.tankMotionRig;
        if (rig == null)
        {
            return;
        }

        if (rig.helperRoot != null)
        {
            rig.helperRoot.localRotation = Quaternion.identity;
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
        return kind == UnitKind.Soldier || kind == UnitKind.Tank || kind == UnitKind.Aircraft || kind == UnitKind.Giant;
    }

    private float DefaultHeadingYaw(UnitKind kind)
    {
        return kind == UnitKind.Giant ? -90f : 90f;
    }

    private void PlayUnitAnimation(BattleUnit unit)
    {
        if (unit == null || !unit.active)
        {
            return;
        }

        bool attacking = ShouldPresentUnitAsAttacking(unit);
        bool moving = unit.runtimeState == UnitRuntimeState.Moving;
        int presentationKey = ((int)unit.runtimeState << 4) | (int)unit.kind;
        if (unit.animationPresentationKey == presentationKey)
        {
            if (unit.kind == UnitKind.Aircraft && unit.combatVariant == UnitCombatVariant.Pterosaur)
            {
                RefreshPterosaurAnimatorClipSpeed(unit);
            }

            if (unit.kind != UnitKind.Giant || GiantAnimatorClipMatchesState(unit, attacking, moving))
            {
                return;
            }
        }

        unit.animationPresentationKey = presentationKey;

        if (TryPlayAnimatorAnimation(unit, attacking, moving))
        {
            return;
        }

        if (unit.combatVariant == UnitCombatVariant.Pterosaur && TryPlayPterosaurLegacyAnimation(unit))
        {
            return;
        }

        if (unit.animations == null || unit.animations.Length == 0)
        {
            return;
        }

        if (unit.kind == UnitKind.Giant && unit.animator == null)
        {
            AnimationClip locomotionClip = SelectGiantLocomotionClip(unit.animatorClips, moving && !attacking);
            if (locomotionClip != null)
            {
                for (int i = 0; i < unit.animations.Length; i++)
                {
                    Animation animation = unit.animations[i];
                    if (animation == null)
                    {
                        continue;
                    }

                    if (animation.GetClip(locomotionClip.name) != null)
                    {
                        if (!string.Equals(unit.currentAnimation, locomotionClip.name, StringComparison.Ordinal))
                        {
                            animation.CrossFade(locomotionClip.name, 0.15f, PlayMode.StopSameLayer);
                            unit.currentAnimation = locomotionClip.name;
                        }

                        return;
                    }
                }
            }
        }

        string desired = GetAnimationName(unit.kind, attacking, moving);
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
                animation.CrossFade(desired, 0.12f, PlayMode.StopSameLayer);
                unit.currentAnimation = desired;
                return;
            }

            // Fallback: substring matching
            string[] keywords = unit.kind == UnitKind.Giant
                ? attacking
                    ? new[] { "Fury", "fury", "Attack", "Punch", "Bite" }
                    : new[] { "Walk", "walk", "ZombieWalk", "Run", "Forward", "Fury", "fury" }
                : attacking
                    ? new[] { "Attack", "Shoot", "Fire" }
                    : new[] { "Walk", "Run", "Forward" };
            foreach (AnimationState state in animation)
            {
                if (state != null && state.clip != null)
                {
                    string clipName = state.clip.name;
                    foreach (string kw in keywords)
                    {
                        if (clipName.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            animation.CrossFade(clipName, 0.12f, PlayMode.StopSameLayer);
                            unit.currentAnimation = clipName;
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

    private static bool GiantAnimatorClipMatchesState(BattleUnit unit, bool attacking, bool moving)
    {
        if (!UsesAnimatorPlayback(unit) || string.IsNullOrEmpty(unit.currentAnimatorClip))
        {
            return false;
        }

        string clip = unit.currentAnimatorClip;
        if (attacking)
        {
            return ClipNameLooksLikeGiantAttack(clip);
        }

        return moving
            ? ClipNameLooksLikeGiantWalk(clip)
            : !ClipNameLooksLikeGiantWalk(clip);
    }

    private static bool ClipNameLooksLikeGiantWalk(string clipName)
    {
        return !string.IsNullOrEmpty(clipName)
            && (clipName.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0
                || clipName.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0
                || clipName.IndexOf("Run", StringComparison.OrdinalIgnoreCase) >= 0
                || clipName.IndexOf("Forward", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void EnsureGiantLegacyAnimationPlaying(BattleUnit unit)
    {
        if (unit == null || unit.kind != UnitKind.Giant || !unit.active || UsesAnimatorPlayback(unit))
        {
            return;
        }

        if (unit.animations == null || unit.animations.Length == 0)
        {
            return;
        }

        bool moving = unit.runtimeState == UnitRuntimeState.Moving;
        bool attacking = ShouldPresentUnitAsAttacking(unit);
        for (int i = 0; i < unit.animations.Length; i++)
        {
            Animation animation = unit.animations[i];
            if (animation == null || animation.isPlaying)
            {
                continue;
            }

            unit.animationPresentationKey = -1;
            PlayUnitAnimation(unit);
            break;
        }

        if (!moving && !attacking)
        {
            return;
        }

        for (int i = 0; i < unit.animations.Length; i++)
        {
            Animation animation = unit.animations[i];
            if (animation == null || animation.isPlaying)
            {
                continue;
            }

            AnimationClip fallback = SelectGiantAnimatorClip(unit, attacking, moving);
            if (fallback != null && animation.GetClip(fallback.name) != null)
            {
                animation.CrossFade(fallback.name, 0.08f, PlayMode.StopSameLayer);
                unit.currentAnimation = fallback.name;
            }
        }
    }

    private static bool ClipNameLooksLikeGiantAttack(string clipName)
    {
        return !string.IsNullOrEmpty(clipName)
            && (clipName.IndexOf("Fury", StringComparison.OrdinalIgnoreCase) >= 0
                || clipName.IndexOf("fury", StringComparison.OrdinalIgnoreCase) >= 0
                || clipName.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0
                || clipName.IndexOf("Punch", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private bool TryPlayAnimatorAnimation(BattleUnit unit, bool attacking, bool moving)
    {
        if (!UsesAnimatorPlayback(unit))
        {
            return false;
        }

        AnimationClip clip = SelectAnimatorClip(unit, attacking, moving);
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
            float clipSpeed = GetAnimatorClipSpeed(unit, clip);
            unit.animationPlayable.SetSpeed(clipSpeed);
            if (!unit.animationGraph.IsPlaying())
            {
                unit.animationGraph.Play();
            }
        }

        return true;
    }

    private void RefreshPterosaurAnimatorClipSpeed(BattleUnit unit)
    {
        if (!UsesAnimatorPlayback(unit)
            || unit.kind != UnitKind.Aircraft
            || unit.combatVariant != UnitCombatVariant.Pterosaur
            || !unit.animationPlayable.IsValid()
            || unit.animatorClips == null
            || unit.animatorClips.Length == 0)
        {
            return;
        }

        AnimationClip clip = unit.animatorClips[0];
        for (int i = 0; i < unit.animatorClips.Length; i++)
        {
            AnimationClip candidate = unit.animatorClips[i];
            if (candidate != null
                && !string.IsNullOrEmpty(unit.currentAnimatorClip)
                && string.Equals(candidate.name, unit.currentAnimatorClip, StringComparison.Ordinal))
            {
                clip = candidate;
                break;
            }
        }

        unit.animationPlayable.SetSpeed(GetAnimatorClipSpeed(unit, clip));
    }

    private AnimationClip SelectAnimatorClip(BattleUnit unit, bool attacking, bool moving)
    {
        if (unit.kind == UnitKind.Aircraft && unit.combatVariant == UnitCombatVariant.Pterosaur)
        {
            return SelectPterosaurAnimatorClip(unit);
        }

        if (unit.kind == UnitKind.Tank)
        {
            return SelectTankAnimatorClip(unit, attacking, moving);
        }

        if (unit.kind == UnitKind.Giant)
        {
            return SelectGiantAnimatorClip(unit, attacking, moving);
        }

        if (attacking)
        {
            return FindAnimatorClip(unit, "Firing Rifle", "Idle_Gun", "Idle_Aiming", "Idle Aiming", "Idle_Shoot", "Shoot", "Attack", "Fire", "Idle");
        }

        if (!moving)
        {
            return FindAnimatorClip(unit, "Idle_Gun", "Idle_Aiming", "Idle Aiming", "Idle");
        }

        return FindAnimatorClip(unit, "Run_Gun", "Run Forward", "Running", "Run", "Walk_Gun", "Walk");
    }

    private AnimationClip SelectPterosaurAnimatorClip(BattleUnit unit)
    {
        return FindAnimatorClip(
            unit,
            "flying",
            "Flying",
            "Fly",
            "Flight",
            "Flap",
            "Glide",
            "Soar",
            "Hover",
            "Gliding",
            "Wing",
            "walking",
            "Idle");
    }

    private AnimationClip SelectTankAnimatorClip(BattleUnit unit, bool attacking, bool moving)
    {
        if (!moving || attacking)
        {
            return FindAnimatorClip(unit, "Tank_Idle", "Idle", "Tank_Forward", "Forward");
        }

        return FindAnimatorClip(unit, "Tank_Forward", "Forward", "Tank_Idle", "Idle");
    }

    private AnimationClip SelectGiantAnimatorClip(BattleUnit unit, bool attacking, bool moving)
    {
        if (unit.animatorClips != null && unit.animator == null)
        {
            return SelectGiantLocomotionClip(unit.animatorClips, moving && !attacking);
        }

        if (attacking)
        {
            return FindAnimatorClip(unit, "Punch", "Headbutt", "Bite", "Attack", "Weapon", "Fury", "fury", "Idle");
        }

        if (!moving)
        {
            return FindAnimatorClip(unit, "Idle", "Fury", "fury", "Walk", "Run");
        }

        bool running = unit.moveSpeed > unit.speed * 1.12f;
        return running
            ? FindAnimatorClip(unit, "Run", "Walk", "walk", "ZombieWalk", "Fury", "fury", "Idle")
            : FindAnimatorClip(unit, "Walk", "walk", "ZombieWalk", "Run", "Fury", "fury", "Idle");
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
        if (unit.kind == UnitKind.Aircraft && unit.combatVariant == UnitCombatVariant.Pterosaur)
        {
            string clipName = clip.name ?? string.Empty;
            if (clipName.IndexOf("fly", StringComparison.OrdinalIgnoreCase) >= 0
                || clipName.IndexOf("flap", StringComparison.OrdinalIgnoreCase) >= 0
                || clipName.IndexOf("glide", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Mathf.Clamp(normalizedSpeed, 0.88f, 1.18f);
            }

            return PterosaurWingFlapAnimSpeed * Mathf.Clamp(normalizedSpeed, 0.92f, 1.12f);
        }

        if (unit.kind == UnitKind.Tank)
        {
            return Mathf.Clamp(normalizedSpeed, 0.6f, 1.55f);
        }

        if (unit.kind == UnitKind.Giant)
        {
            return Mathf.Clamp(normalizedSpeed, 0.95f, 1.55f);
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
        if (unit == null
            || unit.animator == null
            || unit.animatorClips == null
            || unit.animatorClips.Length == 0)
        {
            return false;
        }

        if (unit.kind == UnitKind.Aircraft && unit.combatVariant == UnitCombatVariant.Pterosaur)
        {
            return PterosaurSupportsAnimatorPlayable(unit.animator);
        }

        return true;
    }

    private bool TryPlayPterosaurLegacyAnimation(BattleUnit unit)
    {
        if (unit == null || unit.animations == null || unit.animations.Length == 0)
        {
            return false;
        }

        AnimationClip clip = SelectPterosaurAnimatorClip(unit);
        if (clip == null)
        {
            return false;
        }

        string clipName = clip.name;
        for (int i = 0; i < unit.animations.Length; i++)
        {
            Animation animation = unit.animations[i];
            if (animation == null || animation.GetClip(clipName) == null)
            {
                continue;
            }

            if (!string.Equals(unit.currentAnimation, clipName, StringComparison.Ordinal))
            {
                animation.CrossFade(clipName, 0.12f, PlayMode.StopSameLayer);
                unit.currentAnimation = clipName;
            }
            else if (!animation.isPlaying)
            {
                animation.Play(clipName);
            }

            return true;
        }

        return false;
    }

    private static void DisposeUnitAnimator(BattleUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        if (unit.animationGraph.IsValid())
        {
            try
            {
                unit.animationGraph.Destroy();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ApocalypseKing] Skip stale PlayableGraph dispose: {ex.Message}");
            }
        }

        unit.animationPlayable = default;
        unit.animationOutput = default;
        unit.animator = null;
        unit.animatorClips = null;
        unit.currentAnimatorClip = string.Empty;
    }

    private string GetAnimationName(UnitKind kind, bool attacking, bool moving)
    {
        switch (kind)
        {
            case UnitKind.Soldier:
                if (attacking)
                {
                    return "Idle";
                }

                return moving ? "Run" : "Idle";
            case UnitKind.Tank:
                return moving && !attacking ? "TankArmature|Tank_Forward" : "TankArmature|Tank_Idle";
            case UnitKind.Giant:
                if (attacking)
                {
                    return "Fury";
                }

                return moving ? "walk" : "Fury";
            case UnitKind.Aircraft:
                return "Fly";
            default:
                return string.Empty;
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

    private int CountTankHelperRigs()
    {
        int count = 0;
        for (int i = 0; i < tanks.Count; i++)
        {
            var unit = tanks[i];
            if (!unit.active || unit.motionAccessoryRoot == null)
            {
                continue;
            }

            if (unit.motionAccessoryRoot.name.IndexOf("TankTrackMotionRig", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                count++;
            }
        }

        return count;
    }

    private float GetAverageActiveUnitRenderMetric(List<BattleUnit> units)
    {
        float total = 0f;
        int samples = 0;
        int maxSamples = 12;
        for (int i = 0; i < units.Count && samples < maxSamples; i++)
        {
            var unit = units[i];
            if (!unit.active || unit.modelInstance == null)
            {
                continue;
            }

            if (!TryComputeModelBounds(unit.modelInstance, out Bounds bounds, unit.kind != UnitKind.Tank))
            {
                continue;
            }

            float metric = unit.kind == UnitKind.Tank
                ? GetTankBoundsMetric(bounds)
                : Mathf.Max(0.001f, bounds.size.y);
            total += metric;
            samples++;
        }

        return samples > 0 ? total / samples : 0f;
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
            if (obstacle.Destroyed)
            {
                continue;
            }

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
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        material.color = color;
        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0.12f);
        }
        ApplyOpaqueDoubleSided(material);
        materialCache[key] = material;
        return material;
    }

    private Material GetTexturedOpaqueMaterial(string textureResourcePath, Color tint, Vector2 tiling, float glossiness)
    {
        string key = $"tex-opaque:{textureResourcePath}:{tint.r:F3}:{tint.g:F3}:{tint.b:F3}:{tiling.x:F2}:{tiling.y:F2}:{glossiness:F2}";
        Material material;
        if (materialCache.TryGetValue(key, out material))
        {
            return material;
        }

        Texture texture = Resources.Load<Texture>(textureResourcePath);
        if (texture == null)
        {
            return GetOpaqueMaterial(tint);
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 4;

        Shader shader = FindRuntimeShader(null, "Standard", "Legacy Shaders/Diffuse", "Unlit/Texture", "Sprites/Default");
        material = new Material(shader);
        material.color = tint;

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
            material.SetTextureScale("_MainTex", tiling);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", tiling);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", glossiness);
        }

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

        Shader shader = FindRuntimeShader("RuntimeMaterials/RuntimeUnlitTint", "ApocalypseKing/UnlitTint", "Unlit/Color", "Sprites/Default", "Standard");

        material = new Material(shader);
        material.color = color;
        material.renderQueue = 3000;
        EnableMaterialInstancing(material);
        materialCache[key] = material;
        return material;
    }

    private Shader FindRuntimeShader(string resourceMaterialPath, params string[] shaderNames)
    {
        if (!string.IsNullOrEmpty(resourceMaterialPath))
        {
            var resourceShader = Resources.Load<Shader>(resourceMaterialPath);
            if (resourceShader != null)
            {
                return resourceShader;
            }

            var resourceMaterial = Resources.Load<Material>(resourceMaterialPath);
            if (resourceMaterial != null && resourceMaterial.shader != null)
            {
                return resourceMaterial.shader;
            }
        }

        for (int i = 0; i < shaderNames.Length; i++)
        {
            var shader = Shader.Find(shaderNames[i]);
            if (shader != null && shader.isSupported)
            {
                return shader;
            }
        }

        if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
        {
            string[] urpShaderNames =
            {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Unlit",
                "Universal Render Pipeline/Particles/Unlit",
                "Sprites/Default",
            };
            for (int i = 0; i < urpShaderNames.Length; i++)
            {
                Shader urpShader = Shader.Find(urpShaderNames[i]);
                if (urpShader != null && urpShader.isSupported)
                {
                    return urpShader;
                }
            }
        }

        var errorShader = Shader.Find("Hidden/InternalErrorShader");
        if (errorShader != null)
        {
            return errorShader;
        }

        throw new InvalidOperationException("No usable Unity shader could be found for runtime materials.");
    }

    private static Texture GetImportedMaterialMainTexture(Material material)
    {
        if (material == null)
        {
            return null;
        }

        if (material.mainTexture != null)
        {
            return material.mainTexture;
        }

        if (material.HasProperty("_MainTex"))
        {
            Texture mainTex = material.GetTexture("_MainTex");
            if (mainTex != null)
            {
                return mainTex;
            }
        }

        if (material.HasProperty("_BaseMap"))
        {
            return material.GetTexture("_BaseMap");
        }

        return null;
    }

    private static bool ImportedMaterialNeedsRuntimeRemap(Material material)
    {
        if (material == null)
        {
            return true;
        }

        if (material.shader == null || !material.shader.isSupported)
        {
            return true;
        }

        string shaderName = material.shader.name;
        if (shaderName.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0
            || shaderName.IndexOf("GLTF", StringComparison.OrdinalIgnoreCase) >= 0
            || shaderName.IndexOf("glTF", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
            && (shaderName.StartsWith("Standard", StringComparison.OrdinalIgnoreCase)
                || shaderName.IndexOf("Autodesk", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            return true;
        }

        return GetImportedMaterialMainTexture(material) != null
            && !shaderName.StartsWith("Standard", StringComparison.OrdinalIgnoreCase)
            && shaderName.IndexOf("Legacy", StringComparison.OrdinalIgnoreCase) < 0;
    }

    private Material CreateRemappedOpaqueMaterialFromImported(Material source, Texture2D forceAlbedo = null, Texture2D forceNormal = null)
    {
        Texture main = forceAlbedo != null ? forceAlbedo : GetImportedMaterialMainTexture(source);
        Texture normal = forceNormal;
        if (normal == null && source != null)
        {
            if (source.HasProperty("_BumpMap"))
            {
                normal = source.GetTexture("_BumpMap");
            }
            else if (source.HasProperty("_NormalMap"))
            {
                normal = source.GetTexture("_NormalMap");
            }
        }

        string key = $"import-remap:{main?.GetInstanceID()}:{normal?.GetInstanceID()}:{source?.color}";
        if (materialCache.TryGetValue(key, out Material cached))
        {
            return cached;
        }

        Color tint = source != null ? source.color : Color.white;
        if (source != null && source.HasProperty("_BaseColor"))
        {
            tint = source.GetColor("_BaseColor");
        }

        Shader shader = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
            ? FindRuntimeShader(null, "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", "Standard", "Unlit/Texture")
            : FindRuntimeShader(
                "RuntimeMaterials/RuntimeGltfPbrMetallicRoughness",
                "GLTF/PbrMetallicRoughness",
                "Standard",
                "Legacy Shaders/Diffuse",
                "Unlit/Texture");
        bool invalidShader = shader == null
            || !shader.isSupported
            || shader.name.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0
            || shader.name.IndexOf("Hidden/InternalErrorShader", StringComparison.OrdinalIgnoreCase) >= 0;
        if (invalidShader)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Legacy Shaders/Diffuse");
        }
        if (shader == null)
        {
            Material fallback = GetOpaqueMaterial(tint);
            materialCache[key] = fallback;
            return fallback;
        }
        Material material = new Material(shader);
        material.color = tint;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }

        if (main != null)
        {
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", main);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", main);
            }
        }

        if (normal != null)
        {
            if (material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            if (material.HasProperty("_NormalMap"))
            {
                material.SetTexture("_NormalMap", normal);
            }
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0.22f);
        }

        if (material.HasProperty("_Metallic") && source != null && source.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", source.GetFloat("_Metallic"));
        }

        if (source != null)
        {
            Color emissionColor = source.HasProperty("_EmissionColor") ? source.GetColor("_EmissionColor") : Color.black;
            bool hasEmission = source.IsKeywordEnabled("_EMISSION")
                || Mathf.Max(emissionColor.r, emissionColor.g, emissionColor.b) > 0.02f;
            if (hasEmission)
            {
                emissionColor *= 1.35f;
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", emissionColor);
                }

                if (material.HasProperty("_EmissionMap") && source.HasProperty("_EmissionMap"))
                {
                    Texture emission = source.GetTexture("_EmissionMap");
                    if (emission != null)
                    {
                        material.SetTexture("_EmissionMap", emission);
                    }
                }
            }
        }

        ApplyOpaqueDoubleSided(material);
        materialCache[key] = material;
        return material;
    }

    private static bool IsRuntimeUsableShader(Shader shader)
    {
        if (shader == null || !shader.isSupported)
        {
            return false;
        }

        string shaderName = shader.name ?? string.Empty;
        return shaderName.IndexOf("Error", StringComparison.OrdinalIgnoreCase) < 0
            && shaderName.IndexOf("Hidden/InternalErrorShader", StringComparison.OrdinalIgnoreCase) < 0
            && shaderName.IndexOf("GLTF", StringComparison.OrdinalIgnoreCase) < 0
            && shaderName.IndexOf("glTF", StringComparison.OrdinalIgnoreCase) < 0;
    }

    private Material GetOrCreatePterosaurGlbMaterial(Texture2D albedo, Texture2D normal)
    {
        if (albedo == null)
        {
            return GetOpaqueMaterial(new Color(0.52f, 0.44f, 0.36f, 1f));
        }

        if (pterosaurGlbBodyMaterial != null
            && pterosaurGlbBodyAlbedo == albedo
            && pterosaurGlbBodyNormal == normal)
        {
            return pterosaurGlbBodyMaterial;
        }

        string cacheKey = $"pterosaur-glb:{albedo.GetInstanceID()}:{normal?.GetInstanceID() ?? 0}";
        if (materialCache.TryGetValue(cacheKey, out Material cached) && cached != null)
        {
            pterosaurGlbBodyMaterial = cached;
            pterosaurGlbBodyAlbedo = albedo;
            pterosaurGlbBodyNormal = normal;
            return cached;
        }

        Material material = GetOpaqueMaterial(Color.white);
        ApplyAlbedoToMaterial(material, albedo);
        if (normal != null)
        {
            if (material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            if (material.HasProperty("_NormalMap"))
            {
                material.SetTexture("_NormalMap", normal);
            }
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.28f);
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0.22f);
        }

        ApplyOpaqueDoubleSided(material);
        materialCache[cacheKey] = material;
        pterosaurGlbBodyMaterial = material;
        pterosaurGlbBodyAlbedo = albedo;
        pterosaurGlbBodyNormal = normal;
        return material;
    }

    private void CollectPterosaurTextureCandidates(string resourcePath, GameObject model, HashSet<Texture> textures)
    {
        if (textures == null)
        {
            return;
        }

        CollectPterosaurTexturesFromResource(resourcePath, textures);
        CollectPterosaurTexturesFromResource(PterosaurTextureResourcePath, textures);
        CollectPterosaurTexturesFromResource("Monsters/Pterosaur", textures);
        CollectPterosaurTexturesFromRenderers(model, textures);
    }

    private bool TryGetPterosaurAuthoredTextures(string resourcePath, GameObject model, out Texture2D albedo, out Texture2D normal)
    {
        var textures = new HashSet<Texture>();
        CollectPterosaurTextureCandidates(resourcePath, model, textures);
        albedo = SelectPterosaurAlbedoTexture(textures);
        normal = SelectPterosaurNormalTexture(textures);
        return albedo != null;
    }

    private void ApplyPterosaurAuthoredMaterials(GameObject model, string resourcePath)
    {
        if (model == null)
        {
            return;
        }

        if (!TryGetPterosaurAuthoredTextures(resourcePath, model, out Texture2D albedo, out Texture2D normal))
        {
            RemapPterosaurImportedMaterials(model);
            return;
        }

        Material shared = GetOrCreatePterosaurGlbMaterial(albedo, normal);
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
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
                materials[m] = shared;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private void ApplyPterosaurGltfTextures(GameObject model, string resourcePath)
    {
        ApplyPterosaurAuthoredMaterials(model, resourcePath);
    }

    private static void CollectPterosaurTexturesFromResource(string resourcePath, HashSet<Texture> textures)
    {
        if (textures == null || string.IsNullOrEmpty(resourcePath))
        {
            return;
        }

        Texture2D[] textureAssets = Resources.LoadAll<Texture2D>(resourcePath);
        for (int i = 0; i < textureAssets.Length; i++)
        {
            if (textureAssets[i] != null)
            {
                textures.Add(textureAssets[i]);
            }
        }

        Material[] materials = Resources.LoadAll<Material>(resourcePath);
        for (int i = 0; i < materials.Length; i++)
        {
            CollectPterosaurTexturesFromMaterial(materials[i], textures);
        }
    }

    private static void CollectPterosaurTexturesFromRenderers(GameObject model, HashSet<Texture> textures)
    {
        if (model == null || textures == null)
        {
            return;
        }

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i] != null ? renderers[i].sharedMaterials : null;
            if (materials == null)
            {
                continue;
            }

            for (int m = 0; m < materials.Length; m++)
            {
                CollectPterosaurTexturesFromMaterial(materials[m], textures);
            }
        }
    }

    private static void CollectPterosaurTexturesFromMaterial(Material material, HashSet<Texture> textures)
    {
        if (material == null || textures == null)
        {
            return;
        }

        Texture main = GetImportedMaterialMainTexture(material);
        if (main != null)
        {
            textures.Add(main);
        }

        string[] textureProps =
        {
            "_MainTex", "_BaseMap", "_BumpMap", "_NormalMap", "_MetallicGlossMap", "_OcclusionMap", "_EmissionMap",
        };
        for (int i = 0; i < textureProps.Length; i++)
        {
            if (!material.HasProperty(textureProps[i]))
            {
                continue;
            }

            Texture texture = material.GetTexture(textureProps[i]);
            if (texture != null)
            {
                textures.Add(texture);
            }
        }
    }

    private static Texture2D SelectPterosaurAlbedoTexture(IEnumerable<Texture> textures)
    {
        Texture2D best = null;
        int bestScore = -1;
        if (textures == null)
        {
            return null;
        }

        foreach (Texture texture in textures)
        {
            if (texture is not Texture2D candidate)
            {
                continue;
            }

            string name = candidate.name.ToLowerInvariant();
            if (name.Contains("normal") || name.Contains("nrm") || name.Contains("rough") || name.Contains("metal")
                || name.Contains("occlusion") || name.Contains("ao") || name.Contains("orm"))
            {
                continue;
            }

            int score = candidate.width * candidate.height;
            if (name.Contains("pteranodon") || name.Contains("pterosaur"))
            {
                score += 350000;
            }

            if (name.Contains("base") || name.Contains("color") || name.Contains("albedo") || name.Contains("diffuse"))
            {
                score += 500000;
            }

            if (score > bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    private static Texture2D SelectPterosaurNormalTexture(IEnumerable<Texture> textures)
    {
        if (textures == null)
        {
            return null;
        }

        foreach (Texture texture in textures)
        {
            if (texture is not Texture2D candidate)
            {
                continue;
            }

            string name = candidate.name.ToLowerInvariant();
            if (name.Contains("normal") || name.Contains("nrm"))
            {
                return candidate;
            }
        }

        return null;
    }

    private Material ResolvePterosaurSolidPartMaterial(string partName)
    {
        bool isWing = partName.IndexOf("wing", StringComparison.OrdinalIgnoreCase) >= 0;
        bool isCrest = partName.IndexOf("crest", StringComparison.OrdinalIgnoreCase) >= 0;
        bool isBeak = partName.IndexOf("beak", StringComparison.OrdinalIgnoreCase) >= 0
            || partName.IndexOf("skull", StringComparison.OrdinalIgnoreCase) >= 0;
        Color color = isCrest
            ? new Color(0.58f, 0.50f, 0.42f, 1f)
            : isBeak
                ? new Color(0.48f, 0.42f, 0.36f, 1f)
                : isWing
                    ? new Color(0.44f, 0.38f, 0.32f, 1f)
                    : new Color(0.52f, 0.44f, 0.36f, 1f);
        return GetOpaqueMaterial(color);
    }

    private void RemapPterosaurImportedMaterials(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        if (TryGetPterosaurAuthoredTextures(PterosaurPteranodonResourceModelPath, model, out Texture2D albedo, out Texture2D normal))
        {
            Material shared = GetOrCreatePterosaurGlbMaterial(albedo, normal);
            Renderer[] texturedRenderers = model.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < texturedRenderers.Length; i++)
            {
                Renderer renderer = texturedRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {
                    materials[m] = shared;
                }

                renderer.sharedMaterials = materials;
            }

            return;
        }

        RemoveSketchfabSceneExtras(model);
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            var materials = renderer.sharedMaterials;
            bool changed = false;
            for (int m = 0; m < materials.Length; m++)
            {
                Material source = materials[m];
                if (source == null)
                {
                    materials[m] = ResolvePterosaurSolidPartMaterial(renderer.gameObject.name);
                    changed = true;
                    continue;
                }

                Material remapped = CreateRemappedOpaqueMaterialFromImported(source);
                if (GetImportedMaterialMainTexture(source) == null && GetImportedMaterialMainTexture(remapped) == null)
                {
                    remapped = ResolvePterosaurSolidPartMaterial(renderer.gameObject.name);
                }
                else if (!IsRuntimeUsableShader(remapped != null ? remapped.shader : null))
                {
                    remapped = ResolvePterosaurSolidPartMaterial(renderer.gameObject.name);
                }

                ApplyOpaqueDoubleSided(remapped);
                materials[m] = remapped;
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = materials;
            }
        }
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
        EnableMaterialInstancing(material);
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
        EnableMaterialInstancing(material);
    }

    private static void EnableMaterialInstancing(Material material)
    {
        if (material != null)
        {
            material.enableInstancing = true;
        }
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
        selectedResolutionIndex = DefaultResolutionPresetIndex;
        if (!Application.isMobilePlatform)
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(DefaultPortraitScreenWidth, DefaultPortraitScreenHeight, FullScreenMode.Windowed);
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
        if (hudRoot == null && staticHudRoot == null)
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

        ApplySafeAreaToRoot(staticHudRoot, min, max);
        ApplySafeAreaToRoot(hudRoot, min, max);
    }

    private void UpdateSafeAreaIfNeeded()
    {
        if (hudRoot == null && staticHudRoot == null)
        {
            return;
        }

        if (Screen.width != (int)lastScreenSize.x || Screen.height != (int)lastScreenSize.y || Screen.safeArea != lastSafeArea)
        {
            ApplySafeArea();
        }
    }

    private static void ApplySafeAreaToRoot(RectTransform root, Vector2 min, Vector2 max)
    {
        if (root == null)
        {
            return;
        }

        root.anchorMin = min;
        root.anchorMax = max;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
    }

    private void CreateResolutionControls()
    {
        resolutionStrip = CreatePanel(staticHudRoot, "ResolutionStrip", new Color(0.03f, 0.04f, 0.05f, 0.82f));
        SetAnchors(resolutionStrip.rectTransform, 0.035f, 0.006f, 0.965f, 0.046f);

        resolutionButtons = new Button[ResolutionPresets.Length];
        resolutionButtonImages = new Image[ResolutionPresets.Length];

        const float buttonWidth = 0.19f;
        const float gap = 0.01f;

        for (int i = 0; i < ResolutionPresets.Length; i++)
        {
            float minX = 0.01f + i * (buttonWidth + gap);
            float maxX = minX + buttonWidth;
            var button = CreateResolutionButton(resolutionStrip.transform, $"Resolution_{i}", ResolutionPresets[i].Label, minX, maxX);
            int index = i;
            button.onClick.AddListener(() => ApplyResolutionPreset(index));
            resolutionButtons[i] = button;
            resolutionButtonImages[i] = button.GetComponent<Image>();
        }

        resolutionStrip.gameObject.SetActive(ShowResolutionDebugControls);
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

    private Canvas CreateHudCanvas(string name, int sortingOrder, bool raycaster)
    {
        var canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        var hudCanvas = canvasObject.GetComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.overrideSorting = true;
        hudCanvas.sortingOrder = sortingOrder;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(DefaultPortraitScreenWidth, DefaultPortraitScreenHeight);
        scaler.matchWidthOrHeight = 0.55f;

        if (raycaster)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        return hudCanvas;
    }

    private RectTransform CreateRectRoot(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
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
        float worldX = x * LogicalToWorld;
        float worldZ = z * LogicalToWorld;
        return new Vector3(worldX, SampleBattlefieldGroundHeightWorld(worldX, worldZ) + height, worldZ);
    }

    private bool TryGetUnitBodyLaunchLogical(BattleUnit unit, out float x, out float z, out float height)
    {
        x = unit.x;
        z = unit.z;
        height = unit.altitude;
        if (unit?.body == null)
        {
            return false;
        }

        Vector3 world = unit.body.position;
        x = world.x / LogicalToWorld;
        z = world.z / LogicalToWorld;
        height = world.y - SampleBattlefieldGroundHeightWorld(world.x, world.z);
        return true;
    }

    private void PlayBattleEffect(BattleEffectId id, float x, float z, float height, float scale, Quaternion rotation)
    {
        if (TryPlayBattleEffect(id, ToWorldPoint(x, z, height), rotation, scale))
        {
            return;
        }

        EffectKind fallback = IsSmokeFallback(id) ? EffectKind.Smoke : EffectKind.Fireball;
        SpawnEffect(x, z, Mathf.Max(0.1f, scale), fallback, IsSmokeFallback(id) ? 0.55f : 0.28f);
    }

    private void PlayBattleEffect(BattleEffectId id, Vector3 worldPosition, float scale, Quaternion rotation)
    {
        if (TryPlayBattleEffect(id, worldPosition, rotation, scale))
        {
            return;
        }

        SpawnEffect(worldPosition.x / LogicalToWorld, worldPosition.z / LogicalToWorld, Mathf.Max(0.1f, scale), IsSmokeFallback(id) ? EffectKind.Smoke : EffectKind.Fireball, IsSmokeFallback(id) ? 0.55f : 0.28f);
    }

    private void EnsureBattleEffectServices()
    {
        if (GetComponent<EffectManager>() == null)
        {
            gameObject.AddComponent<EffectManager>();
        }

        if (GetComponent<BattleAudioManager>() == null)
        {
            gameObject.AddComponent<BattleAudioManager>();
        }
    }

    private bool TryPlayBattleEffect(BattleEffectId id, Vector3 worldPosition, Quaternion rotation, float scale)
    {
        EnsureBattleEffectServices();
        if (EffectManager.Instance == null)
        {
            return false;
        }

        EffectManager.Instance.Play(EffectPlayback.Create(id, worldPosition, rotation, null, scale));
        return true;
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

    private Vector2 SoldierMuzzlePoint(BattleUnit unit, Vector2 direction)
    {
        if (TryGetSoldierMuzzleLogical(unit, out Vector2 muzzle))
        {
            return muzzle;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = DirectionFromYaw(unit.headingDegrees);
        }

        direction.Normalize();
        Vector2 side = new Vector2(direction.y, -direction.x);
        float shoulderOffset = unit.rank % 2 == 0 ? 3.2f : -3.2f;
        return new Vector2(
            unit.x + direction.x * 25f + side.x * shoulderOffset,
            unit.z + direction.y * 25f + side.y * shoulderOffset);
    }

    private bool TryGetSoldierMuzzleLogical(BattleUnit unit, out Vector2 logical)
    {
        logical = default;
        if (unit == null)
        {
            return false;
        }

        Transform muzzle = unit.soldierMuzzleVisual;
        if (muzzle == null && unit.modelInstance != null)
        {
            muzzle = ResolveSoldierMuzzleVisual(unit.modelInstance);
            unit.soldierMuzzleVisual = muzzle;
        }

        if (muzzle == null)
        {
            return false;
        }

        Vector3 world = muzzle.position;
        logical = new Vector2(world.x / LogicalToWorld, world.z / LogicalToWorld);
        return true;
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

}
