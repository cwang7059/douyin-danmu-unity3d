#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Opens Unity Asset Store pages for War FX / Cartoon FX (requires user login to download).</summary>
public static class ApocalypseKingAssetStoreVfxImport
{
    private const string WarFxUrl = "https://assetstore.unity.com/packages/vfx/particles/war-fx-5669";
    private const string CfxrFreeUrl = "https://assetstore.unity.com/packages/vfx/particles/cartoon-fx-remaster-free-109565";
    private const string FireballPackUrpUrl =
        "https://assetstore.unity.com/packages/vfx/particles/fire-explosions/free-asset-vfx-particles-fireball-pack-263814";

    [MenuItem("Apocalypse King/Import Asset Store VFX (Open Download Pages)")]
    public static void OpenAssetStorePages()
    {
        Application.OpenURL(WarFxUrl);
        Application.OpenURL(CfxrFreeUrl);
        Application.OpenURL(FireballPackUrpUrl);
        Debug.Log(
            "[ApocalypseKing] Opened War FX + Cartoon FX Remaster Free + Fireball Pack (URP) in browser. "
            + "In Unity: Window > Package Manager > My Assets > Download > Import. "
            + "For URP fireball: Apocalypse King > URP > Run Full URP + Fireball Setup. "
            + "For War/CFXR: Apocalypse King > Bind Store VFX Prefabs to Effect Configs.");
    }

    [MenuItem("Apocalypse King/Import Kenney VFX (Already in repo if import-free-vfx.ps1 ran)")]
    public static void LogKenneyImportHint()
    {
        bool hasKenney = AssetDatabase.IsValidFolder("Assets/Kenney");
        if (hasKenney)
        {
            int bound = ApocalypseKingVfxPrefabBinder.TryBindAllEffectConfigs(logDetails: true);
            AssetDatabase.SaveAssets();
            ApocalypseKingVfxPrefabBinder.ValidateNuclearVfxBinding(logDetails: true);
            Debug.Log($"[ApocalypseKing] Kenney folder present. Re-bound {bound} EffectConfig(s).");
            return;
        }

        Debug.Log(
            "[ApocalypseKing] Kenney not in project. From repo root run: .\\tools\\import-free-vfx.ps1");
    }
}
#endif
