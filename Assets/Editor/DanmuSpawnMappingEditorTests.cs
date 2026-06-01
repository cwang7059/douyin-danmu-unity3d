#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DanmuSpawnMappingEditorTests
{
    [MenuItem("Apocalypse King/Validate Danmu Spawn Mapping")]
    public static void ValidateDanmuSpawnMapping()
    {
        int failures = 0;
        failures += AssertMapping("tank", DanmuHumanSpawnAction.Tank);
        failures += AssertMapping("aircraft", DanmuHumanSpawnAction.Aircraft);
        failures += AssertMapping("medic", DanmuHumanSpawnAction.Heal);
        failures += AssertMapping("heal", DanmuHumanSpawnAction.Heal);
        failures += AssertMapping("unknown_unit_xyz", DanmuHumanSpawnAction.Soldier);
        failures += AssertTextKey("人族坦克", "tank");
        failures += AssertTextKey("human helicopter", "aircraft");
        failures += AssertTextKey("人族 medic", "medic");

        if (failures == 0)
        {
            Debug.Log("[ApocalypseKing] Danmu spawn mapping validation passed.");
        }
        else
        {
            Debug.LogError($"[ApocalypseKing] Danmu spawn mapping validation failed ({failures} checks).");
        }
    }

    private static int AssertMapping(string key, DanmuHumanSpawnAction expected)
    {
        var resolved = DanmuSpawnMappingConfig.ResolveDefaultHumanAction(key);
        if (resolved == expected)
        {
            return 0;
        }

        Debug.LogError($"[ApocalypseKing] Expected key '{key}' -> {expected}, got {resolved}.");
        return 1;
    }

    private static int AssertTextKey(string text, string expectedKey)
    {
        if (DanmuSpawnMapping.TryResolveHumanSpawnKeyFromText(text, out string key)
            && key == expectedKey)
        {
            return 0;
        }

        Debug.LogError($"[ApocalypseKing] Expected text '{text}' -> key '{expectedKey}'.");
        return 1;
    }
}
#endif
