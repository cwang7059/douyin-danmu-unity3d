# 核爆蘑菇云照片（美国政府公版 / 维基共享）

## 素材

| 文件 | 来源 | 许可 |
|------|------|------|
| Castle Romeo | [File:Castle_Romeo.jpg](https://commons.wikimedia.org/wiki/File:Castle_Romeo.jpg) | 美国政府作品，公版 |
| Operation Crossroads Baker | [File:Operation_Crossroads_Baker_edit.jpg](https://commons.wikimedia.org/wiki/File:Operation_Crossroads_Baker_edit.jpg) | 美国政府作品，公版 |
| Trinity（可选） | [File:Trinity_shot_color.jpg](https://commons.wikimedia.org/wiki/File:Trinity_shot_color.jpg) | 美国政府作品，公版 |
| Ivy Mike（可选） | Commons 检索「Ivy Mike mushroom cloud」 | 美国政府作品，公版 |

可选照片下载失败时，演变图集自动复用 Romeo / Baker，不阻断构建。

## Kenney 烟雾粒子

- 包：`smokeParticleAssets.zip`（OpenGameArt / Kenney.nl）
- 许可：CC0
- 用于 `mushroom_smoke_atlas.png` 卷积烟雾

## 导入

```powershell
.\tools\import-nuclear-mushroom-cloud.ps1
```

构建脚本 `build-and-start.ps1` 会在打包前自动执行。

## 工程路径

- `Assets/Resources/VFX/Nuclear/mushroom_cloud_hero.png`
- `Assets/Resources/VFX/Nuclear/mushroom_cloud_evolution.png`（2×2 演变序列）
- `Assets/Resources/VFX/Nuclear/mushroom_smoke_atlas.png`（可选）

## 署名建议（发行说明）

> 核爆蘑菇云参考影像：美国能源部核试验档案照片（维基共享资源，公版）
