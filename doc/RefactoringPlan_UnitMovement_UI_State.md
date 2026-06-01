# ApocalypseKingUnityGame 维护重构收敛计划

本计划基于当前代码状态整理，不再重复创建已经存在的系统。目标是把已拆分出的移动、HUD、弹幕配置和单位运行状态继续收敛到清晰边界内，降低后续改动风险。

## 实施进度（2026-06-01）

| 阶段 | 状态 | 说明 |
|------|------|------|
| 1 建筑避让 | 已完成 | `BuildingAvoidanceSystem` 代理保留；空障碍物防护与绕行注释已补齐 |
| 2 HUD Prefab | 已完成 | 加载顺序：序列化 `hudPrefab` → `Resources/Apocalypse/ApocalypseHudPrefab` → 动态生成 HUD |
| 3 弹幕配置 | 已完成 | `DanmuSpawnMapping` 增加 `DisplayName`、文本匹配；Parser 人族 spawn/heal 走默认映射表 |
| 4 单位状态 | 已完成 | `UnitRuntimeState` 写入路径整理；动画层通过 `ShouldPresentUnitAsAttacking` 读状态 |
| 探针 | 已增强 | `-probeDanmu` 增加 tank；日志输出 HUD/弹幕/攻击态单位计数 |

待办（下一轮）：在 Unity 中执行 **Apocalypse King → Setup Project Assets** 生成 HUD prefab（或手动放到 `Resources/Apocalypse/ApocalypseHudPrefab.prefab`）；按需微调 `Assets/Settings/DanmuSpawnMappingConfig.asset`。

## 当前基线

> [!IMPORTANT]
> **单位移动 / 建筑避让已经完成第一轮拆分。** `MoveUnitToAvoidingBuildings`、`ResolveBuildingCollision`、`ApplyBuildingAvoidanceSteering` 等逻辑已经位于 `Assets/Scripts/ApocalypseKingUnityGame.BuildingAvoidance.cs`，并通过 `BuildingAvoidanceSystem` 及少量代理方法服务 `UnitRuntime`、`UnitSeparation` 和 `Projectiles`。后续不再新建重复的 `ApocalypseKingUnityGame.UnitMovement.cs`。
>
> **HUD Prefab fallback 已经存在。** `ApocalypseKingUnityGame.cs` 的 `CreateHud()` 已经先调用 `TryCreateHudFromPrefab()`，失败后回退到代码动态生成 HUD。当前路线以序列化的 `ApocalypseHudPrefab` 显式绑定为主，比 `GetComponentInChildren<Text>()` 扫描式绑定更稳定。
>
> **弹幕生成映射已经有配置表雏形。** `Assets/Scripts/Danmu/DanmuSpawnMappingConfig.cs` 已经负责将 `tank`、`medic` 等 key 映射到 `DanmuHumanSpawnAction`。后续应扩展这套配置，而不是新增职责重叠的 `DanmuGameConfig`。
>
> **单位运行状态已经有 enum。** `ApocalypseKingUnityGame.Types.cs` 中已有 `UnitRuntimeState { Inactive, Idle, Moving, Attacking, Dead }`，`BattleUnit.runtimeState` 也已存在。后续不新增第二套 `UnitState`，而是整理现有状态流转。

## 执行原则

- 只在现有边界上收敛，不并行引入第二套移动、状态或弹幕配置系统。
- 优先保留代理方法，避免一次性改穿 `UnitRuntime`、`UnitSeparation`、`Projectiles` 的调用链。
- 对 Unity 序列化字段和场景引用保持兼容，任何 prefab 路径或 ScriptableObject 变更都必须有 fallback。
- 每个阶段完成后都能编译并运行，避免把多个行为重构叠在同一次验证里。

## 阶段 1: 建筑避让系统边界收敛

### 目标

确认当前 `BuildingAvoidanceSystem` 已经承担完整的单位移动与建筑碰撞职责，并让外部调用点保持稳定。

### 建议改动

#### [KEEP] `Assets/Scripts/ApocalypseKingUnityGame.BuildingAvoidance.cs`

- 保留当前 `BuildingAvoidanceSystem` 文件和内部类命名。
- 保留 `MoveUnitToAvoidingBuildings`、`ResolveBuildingCollision`、`AvoidsBuildings`、`BuildingAvoidanceRadius`、`SegmentIntersectsBuilding` 这些代理方法，保证既有调用点稳定。
- 检查 `Projectiles` 对 `SegmentIntersectsBuilding` 的静态访问是否仍然合理；如果要改，只做一层明确代理，不让 projectile 直接依赖系统实例。
- 给建筑绕行相关复杂分支补少量注释，说明 road bypass、building bypass、collision resolve 的职责差异。

### 暂不做

- 不新建 `ApocalypseKingUnityGame.UnitMovement.cs`。
- 不把 `UnitSeparationSystem` 和 `BuildingAvoidanceSystem` 合并。二者一个负责单位间分离，一个负责建筑避让，当前边界可以保留。

## 阶段 2: HUD Prefab 路线固化

### 目标

把当前 `ApocalypseHudPrefab` 显式绑定方案写成稳定路径，同时保留代码生成 HUD 作为无 prefab 环境下的 fallback。

### 建议改动

#### [KEEP] `Assets/Scripts/ApocalypseKingUnityGame.HudPrefab.cs`

- 保留 `TryCreateHudFromPrefab()` 作为 prefab 入口。
- 保留 `ApocalypseHudPrefab` 上的显式字段绑定，避免依赖名称或层级扫描。
- 当 prefab 缺少核心 canvas/root 绑定时，继续销毁实例并回退到动态 HUD。

#### [OPTIONAL] Resources fallback

- 只有在确实需要自动构建环境免场景引用时，再增加 `Resources.Load<ApocalypseHudPrefab>(...)` 作为第二入口。
- 加载顺序建议为：序列化字段 `hudPrefab` -> 可选 `Resources` -> 动态生成 HUD。
- 不建议使用 `GetComponentInChildren<Text>()` 自动猜测 UI 字段；如果要支持 prefab，应该继续通过 `ApocalypseHudPrefab` 明确绑定。

## 阶段 3: 弹幕配置表扩展

### 目标

扩展已有 `DanmuSpawnMappingConfig`，逐步减少 parser 和 spawn 逻辑中的硬编码 key，同时不新增重复配置类。

### 建议改动

#### [MODIFY] `Assets/Scripts/Danmu/DanmuSpawnMappingConfig.cs`

- 扩展 `DanmuSpawnMapping`，必要时增加显示名、队伍、默认数值或未来 prefab/config 引用字段。
- 保留 `CreateDefaultHumanMappings()`，让没有 asset 的场景仍然能工作。
- 若新增字段，确保默认值能覆盖 `tank`、`aircraft`、`medic/heal` 的现有行为。

#### [MODIFY] `Assets/Scripts/Danmu/DanmuCommandParser.cs`

- `ParseType` 中仍可保留基础中文/英文关键词判断，用于判断这是 spawn、heal、buff 还是 skill。
- `ParseKey` 中的具体单位 key 应逐步向配置迁移，减少散落硬编码。

#### [MODIFY] `Assets/Scripts/ApocalypseKingUnityGame.Danmu.cs`

- 继续通过 `ResolveHumanDanmuSpawnAction(command.key)` 进入 spawn 逻辑。
- 优先复用 `DanmuSpawnMappingConfig.TryResolveHumanAction`，未知 key 是否降级到 soldier 由配置控制。

### 暂不做

- 不新增 `DanmuGameConfig.cs`，除非后续要统一弹幕、单位属性、prefab 和技能配置，并明确替代现有 `DanmuSpawnMappingConfig`。

## 阶段 4: UnitRuntimeState 流转统一

### 目标

利用已有 `UnitRuntimeState`，把单位运行态从“由计时器和移动速度临时推导”逐步收敛为清晰、单一的状态写入路径。

### 建议改动

#### [KEEP] `Assets/Scripts/ApocalypseKingUnityGame.Types.cs`

- 保留 `UnitRuntimeState`，不新增 `UnitState`。
- 当前基础状态保持为 `Inactive`、`Idle`、`Moving`、`Attacking`、`Dead`。
- 暂不加入 `Stunned`、`Retreating`。等技能、控制效果或 AI 撤退真正进入需求后再扩展。

#### [MODIFY] `Assets/Scripts/ApocalypseKingUnityGame.UnitRuntime.cs`

- 保留 `RefreshRuntimeStateFromMovement()`，但让它只负责移动/待机判断。
- 攻击、死亡、失活状态由对应行为入口明确写入，例如 `FireHumanWeapon`、`PerformGiantMeleeAttack`、`DeactivateHumanUnit`、`DefeatGiant`。
- 避免在同一帧内先写 `Attacking` 又被移动刷新覆盖；`attackVisualTimer > 0f` 的优先级需要保持明确。
- 如果后续引入 `switch (unit.runtimeState)`，先从动画/表现层开始，不要一次性改掉移动和攻击判定。

## 建议执行顺序

1. 更新并提交本计划文档，作为后续重构基线。
2. 做一次 Unity 编译检查，确认当前 partial 拆分状态可编译。
3. 小步整理 `BuildingAvoidanceSystem` 注释和代理边界。
4. 固化 HUD prefab/fallback 规则，必要时补一个缺失 prefab 的启动验证。
5. 扩展 `DanmuSpawnMappingConfig`，把单位 key 映射收口。
6. 梳理 `UnitRuntimeState` 写入路径，优先保证状态不会被同帧误覆盖。

## 验证方案

### 自动化验证

- Unity 编译检查：确认所有 partial class 和 ScriptableObject 变更无编译错误。
- 弹幕解析/映射测试：覆盖 `tank`、`aircraft`、`medic`、未知 key fallback。
- 若有运行时探针，记录单位移动、建筑重叠数、HUD 初始化状态和弹幕处理数量。

### 人工验证

- 在 Unity Editor 中运行游戏，确认 HUD 能正常生成或从 prefab 绑定。
- 在不提供 HUD prefab 的场景中运行，确认动态 HUD fallback 成功。
- 验证士兵、坦克、飞机和巨人仍能移动、攻击、死亡并更新动画表现。
- 验证单位仍然能绕开建筑，坦克和士兵不会持续卡进建筑碰撞范围。
- 模拟弹幕命令，确认 `Idle -> Moving -> Attacking -> Dead/Inactive` 的状态表现符合预期。

## 当前不纳入本轮的事项

- 不做大规模 AI 状态机重写。
- 不引入对象型 State pattern。
- 不把所有单位属性、弹幕、prefab、技能一次性塞进一个大配置表。
- 不移除现有 fallback。线上或自动构建场景仍需要无资源/无 prefab 时可运行。
