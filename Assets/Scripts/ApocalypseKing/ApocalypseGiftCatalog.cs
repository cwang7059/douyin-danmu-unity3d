using System;
using UnityEngine;

[Serializable]
public struct ApocalypseSpawnBatch
{
    public ApocalypseUnitRole Role;
    public int Count;
}

[Serializable]
public sealed class ApocalypseGiftEntry
{
    public string GiftKey;
    public string[] Aliases;
    public int CoinValue;
    public ApocalypseSpawnBatch[] BlueSpawns;
    public ApocalypseSpawnBatch[] GreenSpawns;
    public ApocalypseSpawnBatch[] ZombieSpawns;
    public bool HumanOnlySkill;
}

[CreateAssetMenu(menuName = "Apocalypse/Gift Catalog", fileName = "ApocalypseGiftCatalog")]
public sealed class ApocalypseGiftCatalog : ScriptableObject
{
    public ApocalypseGiftEntry[] Entries = CreateDefaultEntries();

    public bool TryResolve(string text, out ApocalypseGiftEntry entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(text) || Entries == null)
        {
            return false;
        }

        string normalized = text.Trim().ToLowerInvariant();
        for (int i = 0; i < Entries.Length; i++)
        {
            var e = Entries[i];
            if (e == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(e.GiftKey) && normalized.IndexOf(e.GiftKey, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                entry = e;
                return true;
            }

            if (e.Aliases != null)
            {
                for (int a = 0; a < e.Aliases.Length; a++)
                {
                    string alias = e.Aliases[a];
                    if (!string.IsNullOrWhiteSpace(alias) && normalized.IndexOf(alias.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        entry = e;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public ApocalypseSpawnBatch[] GetSpawns(FactionId faction, ApocalypseGiftEntry entry)
    {
        if (entry == null)
        {
            return Array.Empty<ApocalypseSpawnBatch>();
        }

        switch (faction)
        {
            case FactionId.Blue:
                return entry.BlueSpawns ?? Array.Empty<ApocalypseSpawnBatch>();
            case FactionId.Green:
                return entry.GreenSpawns ?? Array.Empty<ApocalypseSpawnBatch>();
            case FactionId.Zombie:
                return entry.ZombieSpawns ?? Array.Empty<ApocalypseSpawnBatch>();
            default:
                return Array.Empty<ApocalypseSpawnBatch>();
        }
    }

    public static ApocalypseGiftEntry[] CreateDefaultEntries()
    {
        return new[]
        {
            Entry("like", new[] { "点赞", "like" }, 0,
                Batch(ApocalypseUnitRole.Survivor, 10), null, Batch(ApocalypseUnitRole.ZombieGrunt, 10)),
            Entry("666", new[] { "666" }, 0,
                Batch(ApocalypseUnitRole.Survivor, 100), null, Batch(ApocalypseUnitRole.ZombieGrunt, 100)),
            Entry("xiannvbang", new[] { "仙女棒", "仙女" }, 1,
                Batch(ApocalypseUnitRole.MeleeGrunt, 80), Batch(ApocalypseUnitRole.MeleeGrunt, 80), Batch(ApocalypseUnitRole.ZombieHound, 80)),
            Entry("pill", new[] { "能力药丸", "药丸" }, 10,
                Batch(ApocalypseUnitRole.RangedGrunt, 80), Batch(ApocalypseUnitRole.RangedGrunt, 80), Batch(ApocalypseUnitRole.RangedGrunt, 80)),
            Entry("mirror", new[] { "魔法镜", "魔镜" }, 19,
                Batch(ApocalypseUnitRole.RushVehicle, 20), Batch(ApocalypseUnitRole.RushVehicle, 20), Batch(ApocalypseUnitRole.RushVehicle, 20)),
            Entry("donut", new[] { "甜甜圈" }, 52,
                Batch(ApocalypseUnitRole.ShieldTank, 20), Batch(ApocalypseUnitRole.ShieldTank, 20), Batch(ApocalypseUnitRole.ShieldTank, 20)),
            Entry("battery", new[] { "能量电池", "电池" }, 99,
                Batch(ApocalypseUnitRole.AirUnit, 12), Batch(ApocalypseUnitRole.AirUnit, 12), Batch(ApocalypseUnitRole.AirUnit, 12)),
            Entry("bomb", new[] { "爱的炸弹", "爱的爆炸", "炸弹" }, 199,
                Batch(ApocalypseUnitRole.Artillery, 4), Batch(ApocalypseUnitRole.Artillery, 4), Batch(ApocalypseUnitRole.Artillery, 4)),
            Entry("airdrop", new[] { "神秘空投", "空投" }, 520,
                Batch(ApocalypseUnitRole.SuperHeavy, 3), Batch(ApocalypseUnitRole.SuperHeavy, 3), Batch(ApocalypseUnitRole.SuperHeavy, 3)),
            SkillEntry("superjet", new[] { "超能喷射", "喷射" }, 1200,
                Batch(ApocalypseUnitRole.Survivor, 0)),
        };
    }

    private static ApocalypseGiftEntry Entry(string key, string[] aliases, int coin,
        ApocalypseSpawnBatch[] blue, ApocalypseSpawnBatch[] green, ApocalypseSpawnBatch[] zombie)
    {
        return new ApocalypseGiftEntry
        {
            GiftKey = key,
            Aliases = aliases,
            CoinValue = coin,
            BlueSpawns = blue,
            GreenSpawns = green ?? blue,
            ZombieSpawns = zombie,
            HumanOnlySkill = false,
        };
    }

    private static ApocalypseGiftEntry SkillEntry(string key, string[] aliases, int coin, ApocalypseSpawnBatch[] blue)
    {
        return new ApocalypseGiftEntry
        {
            GiftKey = key,
            Aliases = aliases,
            CoinValue = coin,
            BlueSpawns = blue,
            GreenSpawns = blue,
            ZombieSpawns = Array.Empty<ApocalypseSpawnBatch>(),
            HumanOnlySkill = true,
        };
    }

    private static ApocalypseSpawnBatch[] Batch(ApocalypseUnitRole role, int count)
    {
        return new[] { new ApocalypseSpawnBatch { Role = role, Count = count } };
    }
}
