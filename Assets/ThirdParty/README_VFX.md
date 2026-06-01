# 第三方战斗特效（VFX）

将 Unity Asset Store 资源导入后，建议移动到下列目录，便于 `Apocalypse King` 菜单自动绑定：

```text
Assets/ThirdParty/UnityAssetStore/
  WarFX/              ← War FX (免费, package 5669)
  CartoonFXRemasterFree/  ← Cartoon FX Remaster Free (109565)
```

也可保留默认路径 `Assets/JMO Assets/...`，绑定器会一并扫描。

## 已自动导入（Kenney CC0）

```powershell
.\tools\import-free-vfx.ps1
```

会安装 `Assets/Kenney/Particle samples/` 并写入 `Resources/VFX/Online/Selected/*.png`。

## Unity Asset Store（需你本机登录）

War FX / Cartoon FX **无法在无 Unity 账号时代下**，请：

1. 菜单 **Apocalypse King → Import Asset Store VFX (Open Download Pages)**
2. Unity：**Window → Package Manager → My Assets → Download → Import**
3. **Apocalypse King → Bind Store VFX Prefabs to Effect Configs**（覆盖 Kenney 为商店特效）

## 验收

```powershell
.\build-and-start.bat
```

详见 `doc/炫酷特效素材清单与接入指南.md`、`doc/许可证/UnityAssetStore_VFX_WarFX_CFXR.md`。
