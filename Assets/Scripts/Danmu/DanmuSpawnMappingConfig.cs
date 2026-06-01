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
    public string DisplayName;
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

    public bool MatchesText(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || Keys == null)
        {
            return false;
        }

        string normalizedText = text.Trim().ToLowerInvariant();
        for (int i = 0; i < Keys.Length; i++)
        {
            string key = Keys[i];
            if (!string.IsNullOrWhiteSpace(key) && normalizedText.IndexOf(key.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    public string ResolvePrimaryKey()
    {
        if (Keys == null || Keys.Length == 0)
        {
            return string.Empty;
        }

        for (int i = 0; i < Keys.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(Keys[i]))
            {
                return Keys[i].Trim().ToLowerInvariant();
            }
        }

        return string.Empty;
    }

    public static bool TryResolveHumanSpawnKeyFromText(string text, DanmuSpawnMapping[] mappings, out string key)
    {
        key = string.Empty;
        if (string.IsNullOrWhiteSpace(text) || mappings == null)
        {
            return false;
        }

        DanmuSpawnMapping bestMatch = null;
        int bestLength = -1;
        for (int i = 0; i < mappings.Length; i++)
        {
            var mapping = mappings[i];
            if (mapping == null || !mapping.MatchesText(text))
            {
                continue;
            }

            string candidate = mapping.ResolvePrimaryKey();
            if (string.IsNullOrEmpty(candidate))
            {
                continue;
            }

            int length = candidate.Length;
            if (length > bestLength)
            {
                bestLength = length;
                bestMatch = mapping;
            }
        }

        if (bestMatch == null)
        {
            return false;
        }

        key = bestMatch.ResolvePrimaryKey();
        return !string.IsNullOrEmpty(key);
    }

    public static bool TryResolveHumanSpawnKeyFromText(string text, out string key)
    {
        return TryResolveHumanSpawnKeyFromText(text, CreateDefaultHumanMappings(), out key);
    }

    public static DanmuSpawnMapping[] CreateDefaultHumanMappings()
    {
        return new[]
        {
            new DanmuSpawnMapping
            {
                DisplayName = "Soldier",
                Action = DanmuHumanSpawnAction.Soldier,
                Keys = new[] { "soldier", "infantry", "步兵", "兵" },
            },
            new DanmuSpawnMapping
            {
                DisplayName = "Tank",
                Action = DanmuHumanSpawnAction.Tank,
                Keys = new[] { "tank", "坦克" },
            },
            new DanmuSpawnMapping
            {
                DisplayName = "Aircraft",
                Action = DanmuHumanSpawnAction.Aircraft,
                Keys = new[] { "aircraft", "plane", "helicopter", "heli", "飞机", "直升机" },
            },
            new DanmuSpawnMapping
            {
                DisplayName = "Medic",
                Action = DanmuHumanSpawnAction.Heal,
                Keys = new[] { "medic", "heal", "治疗", "医疗", "回血" },
            },
        };
    }
}
