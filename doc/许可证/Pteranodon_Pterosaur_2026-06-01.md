# 翼龙（Pteranodon）素材记录

## 推荐（侏罗纪公园 / Primal Ops 风格）

- 模型：JW Primal Ops Pteranodon（Sketchfab 粉丝导出）
- 页面：https://sketchfab.com/3d-models/jw-primal-ops-pteranodon-c9d423e6e27d4334963c6abe86bbf85d
- 许可：**CC Attribution**（需署名作者 SymbboySltunkio2019 / rig Francoraptor2018）
- 导入路径：`Assets/Resources/Monsters/Pterosaur/Pteranodon.glb`

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
# 可选: $env:SKETCHFAB_API_TOKEN 自动从 Sketchfab 拉取 JW Primal Ops
```

Unity 中 Reimport `Assets/Resources/Monsters/Pterosaur`，Stop → Play。
