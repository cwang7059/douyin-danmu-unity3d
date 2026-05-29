using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Apocalypse/Danmu Spawn Mapping", fileName = "DanmuSpawnMappingConfig")]
public sealed class DanmuSpawnMappingConfig : ScriptableObject
{
    public DanmuSpawnMapping[] HumanSpawnMappings = DanmuSpawnMapping.CreateDefaultHumanMappings();
    public DanmuHumanSpawnAction DefaultHumanAction = DanmuHumanSpawnAction.Soldier;
    public bool UseDefaultActionForUnknownKeys = true;

    public bool TryResolveHumanAction(string key, out DanmuHumanSpawnAction action)
    {
        string normalizedKey = NormalizeKey(key);
        if (!string.IsNullOrEmpty(normalizedKey) && HumanSpawnMappings != null)
        {
            for (int i = 0; i < HumanSpawnMappings.Length; i++)
            {
                var mapping = HumanSpawnMappings[i];
                if (mapping != null && mapping.Matches(normalizedKey))
                {
                    action = mapping.Action;
                    return true;
                }
            }
        }

        action = DefaultHumanAction;
        return UseDefaultActionForUnknownKeys;
    }

    public static DanmuHumanSpawnAction ResolveDefaultHumanAction(string key)
    {
        string normalizedKey = NormalizeKey(key);
        var mappings = DanmuSpawnMapping.CreateDefaultHumanMappings();
        for (int i = 0; i < mappings.Length; i++)
        {
            if (mappings[i].Matches(normalizedKey))
            {
                return mappings[i].Action;
            }
        }

        return DanmuHumanSpawnAction.Soldier;
    }

    private static string NormalizeKey(string key)
    {
        return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim().ToLowerInvariant();
    }
}

public enum DanmuHumanSpawnAction
{
    Soldier,
    Tank,
    Aircraft,
    Heal,
}

[Serializable]
public sealed class DanmuSpawnMapping
{
    public string[] Keys;
    public DanmuHumanSpawnAction Action;

    public bool Matches(string normalizedKey)
    {
        if (string.IsNullOrEmpty(normalizedKey) || Keys == null)
        {
            return false;
        }

        for (int i = 0; i < Keys.Length; i++)
        {
            string key = Keys[i];
            if (!string.IsNullOrWhiteSpace(key) && string.Equals(key.Trim(), normalizedKey, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static DanmuSpawnMapping[] CreateDefaultHumanMappings()
    {
        return new[]
        {
            new DanmuSpawnMapping
            {
                Action = DanmuHumanSpawnAction.Tank,
                Keys = new[] { "tank" },
            },
            new DanmuSpawnMapping
            {
                Action = DanmuHumanSpawnAction.Aircraft,
                Keys = new[] { "aircraft", "plane", "helicopter", "heli" },
            },
            new DanmuSpawnMapping
            {
                Action = DanmuHumanSpawnAction.Heal,
                Keys = new[] { "medic", "heal" },
            },
        };
    }
}
