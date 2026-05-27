# Unity3D 弹幕式人族与兽族大战开发方案

## 1. 项目目标

开发一个适合直播间互动的弹幕式对抗游戏。玩家通过弹幕、礼物、点赞或房间事件影响战场，人族和兽族持续生成单位并自动战斗，画面重点是大规模单位、技能爆发、粒子碰撞、声音反馈和清晰的战况展示。

核心目标：

- 直播观众输入弹幕即可参与战斗。
- 人族与兽族拥有不同兵种、技能、建筑和战斗节奏。
- 战斗自动推进，观众行为改变局势。
- 画面要能承载大量单位、投射物、爆炸、受击和阵营特效。
- 所有高频对象必须对象池化，保证移动端和低配电脑也能稳定运行。

## 2. 推荐技术栈

Unity 版本：

- Unity 2022 LTS 或 Unity 6 LTS。
- 当前项目已使用 Unity 2022.3，可继续沿用。

渲染管线：

- 简单低模和直播互动类项目：Built-in 或 URP 都可以。
- 大量粒子、后处理和移动端适配：推荐 URP。
- 已有项目如果是 Built-in，优先保持现状，避免为换管线引入额外风险。

核心 Unity 模块：

- `ParticleSystem`：大部分爆炸、火焰、烟雾、命中、治疗、召唤特效。
- `VFX Graph`：高端版本可用于大量 GPU 粒子，例如魔法风暴、虫群、能量雨。
- `AudioMixer`：管理 BGM、战斗音效、UI 音效、语音播报。
- `Addressables` 或 `StreamingAssets`：加载外部模型和可替换资源。
- `Cinemachine`：镜头跟随、震屏和演出镜头。
- `Input System`：PC 调试、移动端触控和快捷键控制。

直播接入：

- 弹幕 SDK 或本地 WebSocket 服务。
- 游戏内只接收统一后的事件，不直接依赖某个平台字段。

## 3. 推荐工程目录

```text
Assets/
  Art/
    Characters/
      Human/
      Orc/
    Environment/
    Effects/
    UI/
  Audio/
    BGM/
    SFX/
    Voice/
    Mixer/
  Prefabs/
    Units/
    Projectiles/
    Effects/
    UI/
  Scenes/
  Scripts/
    Core/
    Battle/
    Danmu/
    Units/
    Skills/
    Effects/
    Audio/
    UI/
    Data/
  ScriptableObjects/
    Units/
    Skills/
    Waves/
    Balance/
  StreamingAssets/
    Models/
doc/
  README.md
  danmu-battle-unity3d-development.md
  free-assets-and-vfx-setup.md
```

免费素材、官方下载地址、许可证注意点、Unity 特效/声音/粒子碰撞环境搭建步骤，单独维护在：[免费素材地址与特效环境搭建清单](free-assets-and-vfx-setup.md)。开发人员开始导入资源前先看这份清单。

## 4. 游戏循环

基础循环：

1. 直播间弹幕或礼物进入事件队列。
2. 解析事件，转换成游戏指令。
3. 指令影响战场，例如召唤士兵、强化兽族、释放技能。
4. 战斗系统每帧更新单位 AI、移动、攻击、伤害和死亡。
5. 特效系统播放命中、爆炸、治疗、召唤和阵营技能。
6. 声音系统根据事件重要度播放音效或播报。
7. UI 刷新阵营人数、血量、积分池、观众贡献榜和倒计时。
8. 战斗结束后结算胜负，展示 MVP 和下一局倒计时。

一局推荐时长：

- 短局：90 到 180 秒，适合高频直播互动。
- 标准局：3 到 5 分钟，适合阵营对抗。
- 长局：8 到 15 分钟，需要阶段机制和 Boss。

## 5. 阵营设计

### 人族

定位：

- 阵型稳定，远程火力强，单位较脆。
- 依赖坦克、炮塔、治疗和火力覆盖。

兵种示例：

- 步兵：低成本、高数量、自动射击。
- 盾兵：吸收伤害，挡在前排。
- 火枪手：中距离输出，适合弹幕召唤。
- 坦克：高血量，高爆发，慢速。
- 直升机：空中单位，打击兽族后排。
- 祭司或医疗兵：治疗友军，净化减速。

技能示例：

- 空袭：大范围爆炸。
- 集火：一段时间内所有远程单位锁定 Boss。
- 护盾墙：前排获得护盾。
- 炮火支援：连续落弹。

### 兽族

定位：

- 近战冲锋强，单位血厚，视觉压迫感强。
- 依赖冲锋、撕咬、践踏、召唤和群体狂暴。

兵种示例：

- 小兽人：廉价近战单位。
- 狼骑兵：快速突进，绕后攻击。
- 巨魔投手：投掷石块，远程溅射。
- 地狱犬：高移速，撕咬前排。
- 萨满：给兽族加攻速或回血。
- 巨兽 Boss：高血量，多阶段技能。

技能示例：

- 狂暴：兽族攻速和移速提高。
- 裂地：地面冲击波，击退人族。
- 兽群召唤：短时间内连续刷怪。
- 毒雾：区域持续伤害。

## 6. 弹幕互动设计

弹幕命令要短、清晰、容错强。

推荐命令：

```text
人族
1
人
步兵
坦克
治疗
空袭

兽族
2
兽
狼
地狱犬
狂暴
裂地
```

事件映射：

| 事件 | 玩法效果 |
| --- | --- |
| 普通弹幕 | 给对应阵营加积分或召唤小单位 |
| 指定关键词 | 召唤指定兵种 |
| 点赞累计 | 小幅提升阵营能量 |
| 礼物 | 释放技能、召唤精英、回血或强化 |
| 关注 | 触发一次阵营增援 |
| 加入直播间 | 生成一个名字牌单位 |

限流规则：

- 同一用户每 1 到 3 秒只处理一次普通召唤。
- 礼物和付费事件优先级最高。
- 事件进入队列后分帧消化，避免一瞬间卡顿。
- 大量重复弹幕可以聚合成一次批量生成。

建议数据结构：

```csharp
public enum DanmuCommandType
{
    SpawnUnit,
    CastSkill,
    AddEnergy,
    Heal,
    Buff
}

public struct DanmuCommand
{
    public string UserId;
    public string UserName;
    public int Team;
    public DanmuCommandType Type;
    public string Key;
    public int Value;
    public float ReceivedTime;
}
```

## 7. 战斗系统架构

推荐拆分：

- `BattleManager`：控制开局、结算、暂停、阶段推进。
- `TeamManager`：管理人族和兽族资源、能量和单位列表。
- `UnitManager`：生成、回收、查找单位。
- `UnitController`：单个单位的移动、攻击、受击和死亡。
- `TargetingSystem`：寻找目标，做距离判断和优先级排序。
- `DamageSystem`：统一计算伤害、暴击、护甲、溅射和治疗。
- `ProjectileSystem`：子弹、炮弹、箭矢、火球等飞行物。
- `SkillSystem`：技能释放、冷却、蓄力、目标区域。
- `EffectManager`：播放粒子、拖尾、震屏和贴花。
- `AudioManager`：播放音效、混音、限频。
- `DanmuManager`：接收弹幕并转换成游戏命令。

核心原则：

- 玩法判定和视觉特效分离。
- 伤害不要依赖粒子是否碰撞成功。
- 高频对象使用对象池。
- 所有数值用配置驱动，避免写死在代码里。

## 8. 单位 AI

基础状态机：

```text
Spawn
  -> MoveToBattleLine
  -> SearchTarget
  -> MoveToTarget
  -> Attack
  -> Dead
```

远程单位：

- 保持攻击距离。
- 优先打最近目标或威胁最高目标。
- 攻击时播放射击动画和枪口特效。
- 子弹命中后由 `DamageSystem` 结算伤害。

近战单位：

- 向最近敌人推进。
- 到达攻击范围后停止移动。
- 攻击前摇播放动作，命中点触发伤害。
- 可以加入击退和硬直。

Boss 单位：

- 使用阶段状态机。
- 血量到 70%、40%、15% 时切换技能。
- 技能要有明显前摇和地面提示，方便观众理解。

## 9. 投射物系统

投射物类型：

- 即时命中：枪械、激光。
- 直线飞行：箭、火球、炮弹。
- 抛物线：巨石、迫击炮、炸弹。
- 区域持续：毒雾、火墙、冰霜领域。

推荐做法：

- 每个投射物是轻量对象，不挂复杂 AI。
- 初始化时写入起点、终点、速度、伤害、半径和阵营。
- 每帧只做位置推进和命中检查。
- 命中时通知 `DamageSystem`，然后回收到对象池。

抛物线公式：

```csharp
Vector3 pos = Vector3.Lerp(start, end, t);
pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
```

爆炸判定：

```csharp
Collider[] hits = Physics.OverlapSphere(center, radius, enemyLayer);
foreach (var hit in hits)
{
    var unit = hit.GetComponentInParent<UnitController>();
    if (unit != null)
    {
        damageSystem.ApplyDamage(unit, damage, DamageType.Explosion);
    }
}
```

## 10. 粒子特效开发

### 特效分类

生成类：

- 召唤光圈。
- 传送门。
- 地面符文。
- 出生烟尘。

攻击类：

- 枪口火光。
- 炮口火焰。
- 爪击风刃。
- 魔法弹拖尾。

命中类：

- 火花。
- 血雾。
- 石屑。
- 护盾裂纹。

范围技能：

- 爆炸火球。
- 冲击波圆环。
- 毒雾。
- 雷电链。
- 火墙。

死亡类：

- 爆炸。
- 灵魂光点。
- 尸体消散。
- 烟尘和碎片。

### ParticleSystem 标准结构

一个完整特效 prefab 推荐结构：

```text
FX_Explosion_Fire_Large
  CoreFlash
  FireBurst
  Smoke
  Sparks
  Shockwave
  Debris
  PointLight
  AudioSource
```

每层职责：

- `CoreFlash`：0.05 到 0.15 秒，瞬间亮光。
- `FireBurst`：主体火焰，0.3 到 0.8 秒。
- `Smoke`：延迟出现，1 到 3 秒淡出。
- `Sparks`：小颗粒高速飞散。
- `Shockwave`：地面圆环扩散。
- `Debris`：少量碎石或碎片。
- `PointLight`：短时间照亮周围。
- `AudioSource`：播放爆炸声，可由 AudioManager 控制。

### 粒子参数建议

爆炸：

- Duration：0.4 到 1.2 秒。
- Start Lifetime：0.2 到 2 秒。
- Start Speed：5 到 20。
- Simulation Space：World。
- Emission：Burst。
- Shape：Sphere 或 Cone。
- Size over Lifetime：先大后小。
- Color over Lifetime：白黄到橙红，再到透明黑烟。

烟雾：

- Start Lifetime：2 到 5 秒。
- Start Speed：0.4 到 2。
- Noise：开启，Strength 0.3 到 1.2。
- Renderer：Soft Particles 开启。
- Color：灰黑，Alpha 缓慢淡出。

冲击波：

- 使用扁平圆环 mesh 或粒子 billboard。
- Size over Lifetime 从小到大。
- Alpha 快速衰减。
- 可配合地面 decal。

### 粒子碰撞

粒子的 Collision Module 适合做视觉反馈，不适合做核心伤害判定。

适合用粒子碰撞的场景：

- 火花打到地面反弹。
- 碎石落地。
- 血滴或泥土喷溅。
- 魔法粒子碰到护盾后消失。

不推荐用粒子碰撞做：

- 子弹真实伤害。
- 大范围爆炸伤害。
- 高频弹幕命中。
- 复杂 Boss 技能判定。

原因：

- 粒子碰撞事件数量很大，性能不可控。
- 不同设备帧率下结果可能不同。
- 伤害结算很难调试和回放。

推荐做法：

- 玩法伤害使用 Collider、Raycast、OverlapSphere 或自定义距离判断。
- 粒子碰撞只生成火花、反弹和消失效果。
- 命中事件由 `DamageSystem` 广播给 `EffectManager`。

粒子碰撞视觉示例：

```csharp
private readonly List<ParticleCollisionEvent> events = new List<ParticleCollisionEvent>();

private void OnParticleCollision(GameObject other)
{
    int count = particleSystem.GetCollisionEvents(other, events);
    for (int i = 0; i < count; i++)
    {
        effectManager.Play("FX_Spark_Hit", events[i].intersection);
    }
}
```

注意：

- 给粒子碰撞单独设置 Layer。
- 限制 `Max Collision Shapes`。
- 高频特效不要全部开启 Collision。
- 只对近镜头或重要技能开启粒子碰撞。

## 11. 技能特效设计

### 人族空袭

流程：

1. 地面出现红色警戒圈。
2. 0.8 秒后从天上落下多枚炮弹。
3. 每枚炮弹落点播放火光、烟尘、碎片和震屏。
4. 使用 `Physics.OverlapSphere` 对范围内兽族造成伤害。
5. AudioManager 播放远处呼啸声和爆炸声。

Prefab：

```text
Skill_AirStrike
  FX_WarningCircle
  Projectile_Bomb
  FX_Explosion_Fire_Large
  SFX_AirWhistle
  SFX_Explosion
```

### 兽族裂地

流程：

1. Boss 抬手或跺脚。
2. 地面出现裂纹线。
3. 冲击波沿直线推进。
4. 命中的人族被击退并短暂眩晕。
5. 地面留下短暂燃烧或尘土。

判定：

- 用胶囊区域或矩形区域判断。
- 视觉冲击波和实际判定可以同步推进。

### 兽族狂暴

流程：

1. 兽族单位身上出现红色光环。
2. 头顶或身体周围有向上飘的能量粒子。
3. 攻速、移速提升 5 到 10 秒。
4. 结束时播放淡出特效。

实现：

- 给单位添加 Buff。
- `UnitStats` 临时乘以系数。
- 特效挂在单位身上，并由 Buff 生命周期控制。

## 12. 声音系统

### AudioMixer 分组

```text
Master
  BGM
  SFX
    Weapon
    Explosion
    Creature
    UI
  Voice
  Ambience
```

声音设计原则：

- 高频小声音要限制播放数量。
- 大爆炸和 Boss 技能要有优先级。
- 同类音效随机选择多个变体，避免重复刺耳。
- 重要事件可以配合 UI 播报。

音效分类：

- 武器：枪声、炮声、箭矢、魔法弹。
- 命中：金属、肉体、护盾、建筑。
- 爆炸：小爆炸、大爆炸、远处爆炸。
- 单位：兽吼、冲锋、死亡、受击。
- 技能：蓄力、释放、持续、结束。
- UI：按钮、倒计时、阵营选择、胜负结算。

### 音效限频

大量单位同时攻击时不能每个单位都播放完整音效。

做法：

- 同一音效每 0.05 到 0.2 秒最多播放一次。
- 近镜头单位优先播放。
- 重要技能绕过普通限频。
- 可以把 20 个枪声合并成一段“枪阵”循环声。

示例：

```csharp
public bool CanPlay(string key, float interval)
{
    float now = Time.time;
    if (lastPlayTime.TryGetValue(key, out float last) && now - last < interval)
    {
        return false;
    }

    lastPlayTime[key] = now;
    return true;
}
```

### 3D 声音

适合 3D 的声音：

- 爆炸。
- Boss 咆哮。
- 大型投射物。
- 建筑倒塌。

适合 2D 的声音：

- UI。
- 直播礼物提示。
- 胜负播报。
- 全屏技能提示。

## 13. 碰撞与伤害判定

建议分层：

```text
Layer_Human
Layer_Orc
Layer_HumanProjectile
Layer_OrcProjectile
Layer_EffectCollision
Layer_Terrain
Layer_IgnoreRaycast
```

玩法碰撞：

- 单位之间可以不用真实 Rigidbody 互推，使用自定义分离算法更稳定。
- 投射物可用 SphereCast 或自定义距离检测。
- 范围技能用 OverlapSphere、OverlapBox 或 OverlapCapsule。
- 近战攻击用扇形、圆形或胶囊区域。

视觉碰撞：

- 粒子 Collision Module。
- 碎片 Rigidbody。
- 地面 decal。
- 碰撞火花。

伤害流程：

```text
Attack Event
  -> Hit Query
  -> Damage Request
  -> DamageSystem
  -> Unit Health Change
  -> Hit Event
  -> EffectManager + AudioManager + UI
```

不要让每个系统互相直接调用太多。推荐用事件或消息：

```csharp
public struct HitEvent
{
    public Vector3 Position;
    public Vector3 Normal;
    public int AttackerTeam;
    public int TargetTeam;
    public DamageType DamageType;
    public float Damage;
}
```

## 14. 对象池

必须池化的对象：

- 单位。
- 子弹、炮弹、箭、火球。
- 爆炸、火花、烟雾。
- 伤害数字。
- UI 飘字。
- 地面警戒圈。
- 临时音源。

对象池规则：

- 开局预热常用对象。
- 高峰时不频繁 Instantiate 和 Destroy。
- 粒子播放完成后自动回池。
- 音效播放完成后音源回池。
- 池不够时可以扩容，但要记录日志方便调优。

示例：

```csharp
public interface IPoolable
{
    void OnSpawned();
    void OnDespawned();
}
```

## 15. UI 与直播表现

核心 UI：

- 顶部阵营血条。
- 当前局倒计时。
- 人族人数、兽族人数。
- 能量条和技能冷却。
- 弹幕贡献榜。
- 礼物触发提示。
- 战场事件播报。
- 胜负结算面板。

直播画面原则：

- 字要大，信息密度适中。
- 关键技能要有明显提示。
- 阵营颜色固定，例如人族蓝色、兽族红色。
- 观众名字可以出现在单位头顶，但要限量显示。
- 不要让飘字挡住主战场。

## 16. 数值配置

建议使用 ScriptableObject：

```text
UnitConfig
  Id
  DisplayName
  Team
  Prefab
  MaxHp
  Attack
  Defense
  MoveSpeed
  AttackRange
  AttackInterval
  ProjectileId
  DeathEffectId
  SpawnCost

SkillConfig
  Id
  DisplayName
  Team
  Cooldown
  EnergyCost
  CastDelay
  Radius
  Damage
  EffectId
  SoundId
```

调参流程：

1. 先做 3 个基础兵种和 2 个技能。
2. 用固定 AI 跑 100 局模拟胜率。
3. 调整单位成本、血量和攻击。
4. 接入弹幕后再调礼物和弹幕的权重。
5. 用直播真实节奏测试高峰压力。

## 17. 性能优化

单位规模建议：

- PC 普通配置：100 到 300 个活跃单位。
- 移动端：50 到 150 个活跃单位。
- 直播推流机器：优先稳定 30 或 60 FPS。

优化重点：

- 模型尽量低面数。
- 骨骼动画数量要控制。
- 远处单位减少动画更新频率。
- 粒子数量设置上限。
- 避免每帧 `FindObjectsOfType`、`GetComponent` 大量调用。
- 避免频繁 LINQ 和字符串拼接。
- 使用对象池。
- 使用 LayerMask 减少物理查询范围。
- UI 不要每帧重建大量文本。

LOD 策略：

- 近处：完整模型、动画、阴影。
- 中处：低模、简单阴影。
- 远处：billboard 或简化模型。
- 超远：只显示阵营图标或不显示。

粒子预算：

- 小命中特效：20 到 80 粒子。
- 中型爆炸：100 到 300 粒子。
- 大型技能：300 到 1000 粒子。
- 全屏技能：控制同时存在数量，必要时用贴图动画代替粒子。

## 18. 开发里程碑

### M1：核心原型

目标：

- 人族和兽族各 1 个兵种。
- 自动寻敌、移动、攻击、死亡。
- 简单弹幕命令生成单位。
- 基础 UI 显示双方人数和胜负。

验收：

- 一局可以自动打完。
- 弹幕命令能改变战局。
- 没有明显卡顿和报错。

### M2：战斗体验

目标：

- 增加 3 到 5 个兵种。
- 增加投射物、范围伤害和近战技能。
- 加入对象池。
- 加入基础音效和特效。

验收：

- 100 个单位同时战斗稳定。
- 爆炸、命中、死亡都有清楚反馈。
- 声音不混乱、不爆音。

### M3：直播互动

目标：

- 接入真实弹幕服务。
- 礼物触发技能。
- 加入贡献榜和事件播报。
- 加入限流和事件队列。

验收：

- 高峰弹幕不会卡死。
- 礼物技能触发稳定。
- UI 能看清是谁影响了战局。

### M4：美术和演出

目标：

- 替换正式模型。
- 做完整技能特效。
- 加入镜头震动、屏幕闪光、地面提示。
- 加入 BGM、环境音和语音播报。

验收：

- 技能释放前后清晰。
- 人族和兽族视觉风格明显。
- 直播间观众能一眼看懂战况。

### M5：性能和发布

目标：

- 压力测试。
- 降级配置。
- 打包和自动化验证。
- 崩溃日志和运行日志。

验收：

- 目标机器运行 2 小时不崩溃。
- 直播弹幕高峰不掉到不可接受帧率。
- 资源缺失时有 fallback 或报错提示。

## 19. 测试清单

功能测试：

- 弹幕命令解析是否正确。
- 单位是否正确生成到对应阵营。
- 技能冷却和能量是否正确。
- 战斗胜负是否正确结算。
- 单位死亡是否回池。

特效测试：

- 爆炸位置是否与伤害范围一致。
- 地面警戒圈是否提前出现。
- 粒子是否能自动销毁或回池。
- 粒子碰撞是否只影响视觉。
- 大量技能同时释放是否卡顿。

声音测试：

- BGM、SFX、UI 音量是否可单独调节。
- 高频枪声是否限频。
- 大爆炸是否有优先级。
- 直播提示音是否不会被战斗声盖住。

性能测试：

- 50、100、200、300 单位压力测试。
- 10、30、60 个爆炸同时播放。
- 1000 条弹幕事件排队处理。
- 长时间运行内存是否上涨。

## 20. 推荐先做的最小版本

第一版不要一开始就做全部兵种。推荐先做：

- 人族：步兵、坦克、医疗兵。
- 兽族：小兽人、地狱犬、巨兽 Boss。
- 技能：人族空袭、兽族狂暴。
- 特效：枪口火光、炮弹爆炸、爪击命中、死亡烟尘、召唤光圈。
- 声音：BGM、枪声、炮声、爆炸、兽吼、UI 提示。
- 弹幕：选择阵营、召唤单位、释放技能。

这个版本能验证核心乐趣：观众发弹幕之后，战场立即有变化，并且变化能被看见、听见、理解。
