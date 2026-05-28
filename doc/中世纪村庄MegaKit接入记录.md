# 中世纪村庄 MegaKit 接入记录

更新时间：2026-05-27

## 资源结论

Quaternius `Medieval Village MegaKit[Standard].zip` 是 CC0 免费资源包。Standard 版包含 176 个 glTF 建筑模块，但没有整栋房屋成品 Prefab，所以不能简单替换成一个 `House.glb`。正确做法是把全部模块导入 Unity，再由场景代码组合出房屋、塔楼、围栏和村庄道具。

## 当前实现

- 资源目录：`Assets/Resources/Quaternius/MedievalVillageMegaKit`
- 代码入口：`Assets/Scripts/ApocalypseKingUnityGame.cs`
- 资源加载：`Resources.LoadAll<GameObject>("Quaternius/MedievalVillageMegaKit")`
- 村庄生成：`CreateMegaKitMedievalVillage()`
- 诊断字段：探针日志会输出 `villageAssets`

## 避坑点

- 不要把 Standard 当成整栋建筑包，它是墙、屋顶、门窗、楼梯等模块。
- 可见建筑和逻辑阻挡区必须同一处创建：每栋组合建筑结束时调用 `AddBuildingObstacle()`。
- 本项目单位移动不是 Rigidbody/Collider 驱动，Unity Collider 不能作为唯一碰撞来源。
- 地面碎块不要再加回来：已移除 `VillageRoadStone`、`MeadowPatch`、`DistantStoneFence`、`CreateLowVillageDebris()` 等零散方块生成。
- 当前建筑阻挡仍是逻辑坐标 AABB。建筑可以小角度旋转；如果之后做大角度斜放建筑，要同步扩大 `Padding` 或改成旋转矩形检测。
