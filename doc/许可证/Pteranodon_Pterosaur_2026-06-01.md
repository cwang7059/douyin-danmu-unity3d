# 翼龙（Pteranodon）素材记录

## 当前默认（带扇翼动画，已自动下载）

- 模型：Pteranodon (Animated)（Sketchfab）
- 页面：https://sketchfab.com/3d-models/pteranodon-animated-7d7683df41d1405283f160e81a5dff1b
- 动画片段：`flying` / `walking` / `standing`（战斗显示为绑定姿态 + 骨骼程序化扇翼；片段挂载备用）
- 许可：**CC Attribution**（按 Sketchfab 页面署名 Chistodrako / oscar.lopez.riviello 等作者）
- 导入路径：`Assets/Resources/Monsters/Pterosaur/Pteranodon.glb`（约 7.5MB）
- 导入：`.\tools\import-pterosaur-pteranodon.ps1 -PreferAnimated`（需 `SKETCHFAB_API_TOKEN` 或保留已有 GLB）
- 游戏内：优先 GLB 网格显示；喷火为嘴部火球 VFX（非模型内嵌火焰）

## 备选（侏罗纪公园 / Primal Ops 风格，无翅膀动画）

- 模型：JW Primal Ops Pteranodon（Sketchfab 粉丝导出）
- 页面：https://sketchfab.com/3d-models/jw-primal-ops-pteranodon-c9d423e6e27d4334963c6abe86bbf85d
- 许可：**CC Attribution**（需署名作者 SymbboySltunkio2019 / rig Francoraptor2018）

## 程序生成兜底（仓库自带）

- 脚本：`tools/generate-pteranodon-glb.py`
- 许可：项目自有代码生成，无第三方网格

## OpenGameArt 低模翼龙（可选）

- 页面：https://opengameart.org/content/low-spec-pterosaur
- 许可：见 OGA 条目（通常为 CC0 / CC-BY，以页面为准）
- 文件：`low_spec_pterosaur_oga.zip`（Blender 源文件，需 Blender 导出 GLB）

## 自定义 fierce 模型（黑身红翼等）

将下载的 GLB 放到任一位置后执行：

```powershell
.\tools\import-pterosaur-pteranodon.ps1 -GlbPath "D:\Downloads\你的模型.glb"
```

或复制到 `Assets/Resources/Monsters/Pterosaur/Incoming/Pteranodon.glb` 后：

```powershell
.\tools\import-pterosaur-pteranodon.ps1
```

## 使用

```powershell
.\tools\import-pterosaur-pteranodon.ps1
# 可选: $env:SKETCHFAB_API_TOKEN 自动从 Sketchfab 拉取（默认优先 Animated 版）
```

Unity 中 Reimport `Assets/Resources/Monsters/Pterosaur`，Stop → Play。
