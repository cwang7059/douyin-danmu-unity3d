# 人族火箭炮车（RocketTruck）素材记录

## 推荐（红警风格：苏式卡车 + 后部火箭发射架）

- 模型：БМ-21 «Град» (BM-21 Grad)
- 页面：https://sketchfab.com/3d-models/21-bm-21-grad-c90559a30e6a414d993a0d3bdf6c5ff8
- 作者：Basic Hsu (@Hsu.Pei.Ge)
- 许可：**CC Attribution**（游戏中需署名）
- 导入路径：`Assets/Resources/Vehicles/RocketTruck/RocketTruck.glb`

```powershell
$env:SKETCHFAB_API_TOKEN = "你的Token"
.\tools\import-rocket-truck.ps1
```

## 备选 Sketchfab（需自行确认页面许可）

| 说明 | UID |
|------|-----|
| Katyusha BM-13 | `bb46c77dc61b46a0be797fe12aa9a36e` |
| V3 火箭发射车造型 | `8820550131e54ec8ac0f0712a85bc1b9` |
| 多管火箭发射卡车 | `303c46b5ede748288ae6ca6d085aae79` |

## 程序生成兜底（仓库自带）

- 脚本：`tools/generate-rocket-truck-glb.py`
- 轮廓：6×6 底盘 + 驾驶室 + 后部倾斜发射架 + 16 管火箭（近似红警 V3 / 冰雹车）
- 许可：项目自有代码生成，无第三方网格

## 手动替换

将下载的 GLB 放到 `Assets/Resources/Vehicles/RocketTruck/Incoming/RocketTruck.glb` 后：

```powershell
.\tools\import-rocket-truck.ps1
```

或：

```powershell
.\tools\import-rocket-truck.ps1 -GlbPath "D:\Downloads\你的火箭车.glb"
```
