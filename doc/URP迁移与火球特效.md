# URP 迁移与翼龙火球（Fireball Pack）

更新时间：2026-06-01

工程已切换为 **URP**（方案 A）：

- `Packages/manifest.json`：URP 14.0.11
- `Assets/Settings/URP/ApocalypseKing_URP.asset`：管线资产
- `Project Settings > Graphics / Quality`：已指向上述 URP 资产

在 Unity 中**重新打开工程或切回 Unity 窗口**后，编辑器会重新导入资产；若仍显示 Built-in，检查 Graphics 是否指向 `ApocalypseKing_URP`。

## 1. 一键菜单（推荐顺序）

Unity 顶部菜单：

| 步骤 | 菜单项 | 作用 |
| --- | --- | --- |
| 0 | `Apocalypse King/URP/Open Fireball Pack (Asset Store)` | 打开 [Fireball Pack 263814](https://assetstore.unity.com/packages/vfx/particles/fire-explosions/free-asset-vfx-particles-fireball-pack-263814) |
| 1 | `Apocalypse King/URP/1. Create and Assign URP Pipeline` | 若仓库内已有 `ApocalypseKing_URP.asset` 可跳过；否则创建并写入 Graphics + Quality |
| 2 | `Apocalypse King/URP/2. Upgrade Project Materials to URP` | 全项目材质升级（Built-in → URP） |
| 3 | `Apocalypse King/URP/3. Install Pterosaur Fireball Prefab to Resources` | 从已导入包复制 Prefab 到 `Resources/Battle/VFX/` |
| 快捷 | `Apocalypse King/URP/Run Full URP + Fireball Setup` | 依次执行 1→2；若已导入 Fireball Pack 则再执行 3 |

生成文件：

- `Assets/Settings/URP/ApocalypseKing_URP.asset`
- `Assets/Settings/URP/ApocalypseKing_ForwardRenderer.asset`
- `Assets/Resources/Battle/VFX/UrpPterosaurFireball.prefab`（弹道）
- `Assets/Resources/Battle/VFX/UrpPterosaurFireballHit.prefab`（命中）

## 2. 运行时行为

- 若存在 `Resources/Battle/VFX/UrpPterosaurFireball`：翼龙弹道使用该 URP Prefab，**不再**创建橙色 `FireballGlow` 球体，**不再**挂 `TrailRenderer` 与 `Point Light`。
- 未安装 URP 火球时：仍用 `EffectManager.BuildPterosaurFireballProjectileVfx` 程序化粒子（无点光源、无发光核）。
- 命中：`EffectManager` 优先 `UrpPterosaurFireballHit`，否则走程序化 `PterosaurFireballImpact`。

深红调色在 `ApocalypseKingUnityGame.PterosaurFireballProjectile.cs` 中对粒子 `startColor` 做运行时混合；可在 Inspector 调 `PterosaurFireballUrpVisualScale`（默认 `1.15`）。

## 3. 导入 Fireball Pack

1. Asset Store 下载 **VFX Particles: Fireball Pack**（仅 URP/HDRP）。
2. `Window > Package Manager > My Assets > Import`。
3. 执行菜单 **URP/3**（或完整 Setup）。
4. **Stop → Play**，翼龙吐火应为火球+尾迹，Scene 视图可关闭 Light Gizmo 减少干扰。

## 4. 迁移后检查清单

- [ ] `Project Settings > Graphics` 的 Scriptable Render Pipeline Settings 为 `ApocalypseKing_URP`
- [ ] 各 Quality 档也指向同一 URP Asset
- [ ] Quaternius / WarFX / Cartoon FX 材质无洋红（粉红）缺失 shader
- [ ] `RuntimeUnlitTint`、战场地面、单位 glTF 正常显示
- [ ] 翼龙火球无「太阳」图标占位（旧 Built-in 占位已移除）

## 5. 命令行（需关闭 Unity 编辑器）

```powershell
.\tools\setup-urp.ps1
.\tools\setup-urp.ps1 -InstallFireballIfPresent
```

## 6. 回退

若需临时回 Built-in：将 Graphics / Quality 的 Render Pipeline Asset 设为 **None**，并恢复 Built-in 材质备份分支。本仓库 URP 包可保留在 manifest 中，仅不指派 Pipeline。

## 6. 相关文档

- `doc/免费素材与特效环境搭建.md` §4.3（URP 通用说明）
- `doc/炫酷特效素材清单与接入指南.md`（素材选型）
