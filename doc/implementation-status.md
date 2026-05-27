# 实现进度记录

更新时间：2026-05-27

## 已完成

- 创建文档目录和素材/特效开发清单。
- 创建弹幕命令基础层：
  - `Assets/Scripts/Danmu/DanmuCommand.cs`
  - `Assets/Scripts/Danmu/DanmuCommandParser.cs`
  - `Assets/Scripts/Danmu/DanmuCommandQueue.cs`
- 创建特效基础层：
  - `Assets/Scripts/Effects/BattleEffectId.cs`
  - `Assets/Scripts/Effects/EffectConfig.cs`
  - `Assets/Scripts/Effects/EffectManager.cs`
  - `Assets/Scripts/Effects/PooledParticleEffect.cs`
  - `Assets/Scripts/Effects/ParticleCollisionRelay.cs`
- 创建音频基础层：
  - `Assets/Scripts/Audio/BattleAudioCueId.cs`
  - `Assets/Scripts/Audio/AudioCueConfig.cs`
  - `Assets/Scripts/Audio/BattleAudioManager.cs`
- 场景构建器已自动挂载：
  - `DanmuCommandQueue`
  - `DanmuHttpGateway`
  - `EffectManager`
  - `BattleAudioManager`
- 现有战斗已接入弹幕命令：
  - 人族弹幕：步兵/坦克增援、治疗、集火、空袭。
  - 兽族弹幕：巨人增援、治疗、狂暴、加速攻击。
- 运行探针已支持 `-probeDanmu`，可以自动注入模拟弹幕做回归测试。
- 本地 HTTP 弹幕网关已实现，默认地址为 `http://127.0.0.1:8765/`。

## 本地调试

运行游戏后可用键盘快速注入本地命令：

```text
1：人族步兵
2：兽族地狱犬/怪物增援
3：人族空袭
4：兽族狂暴
```

自动探针命令示例：

```powershell
.\Builds\Windows\ApocalypseKingUnity3D.exe -apocalypseProbe -probeDanmu -probeDelay 2.0 -probeTimeScale 1 -probeOutput preview-danmu-foundation.png -logFile player-danmu-foundation.log
```

验收日志关键字段：

```text
danmuAccepted=4
danmuPending=0
danmuDropped=0
fallback=False
captured=True
```

## 下一步

- 接入真实直播 SDK 或 WebSocket，把平台弹幕转成 `DanmuCommandQueue.EnqueueRawMessage(...)`。
- 或者把平台弹幕桥接到 HTTP 网关：`POST http://127.0.0.1:8765/danmu`。
- 从免费素材清单导入正式特效包，并创建 `EffectConfig` 资源。
- 创建 `AudioCueConfig` 资源，并配置 `AudioMixer` 分组。
- 把现有内置 `SpawnEffect` 逐步迁移到 `EffectManager`。
- 把士兵/坦克/巨人硬编码数值迁移到 ScriptableObject 配置。
