# 第三方战斗特效（VFX）

将 Unity Asset Store 资源导入后，建议移动到下列目录，便于 `Apocalypse King` 菜单自动绑定：

```text
Assets/ThirdParty/UnityAssetStore/
  WarFX/              ← War FX (免费, package 5669)
  CartoonFXRemasterFree/  ← Cartoon FX Remaster Free (109565)
```

也可保留默认路径 `Assets/JMO Assets/...`，绑定器会一并扫描。

## 操作步骤

1. Unity：**Window → Package Manager → My Assets**，下载并 Import **War FX**、**Cartoon FX Remaster Free**。
2. 菜单：**Apocalypse King → Bind Store VFX Prefabs to Effect Configs**（或 **Setup Project Assets**，已包含绑定）。
3. 运行 `.\build-and-start.bat` 验收。

详见 `doc/炫酷特效素材清单与接入指南.md`、`doc/许可证/UnityAssetStore_VFX_WarFX_CFXR.md`。
