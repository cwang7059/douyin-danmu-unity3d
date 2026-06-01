using System;
using System.Collections.Generic;

public static class ApocalypseGiftLabels
{
    private static readonly Dictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "like", "点赞" },
        { "666", "666" },
        { "xiannvbang", "仙女棒" },
        { "pill", "能力药丸" },
        { "mirror", "魔法镜" },
        { "donut", "甜甜圈" },
        { "battery", "能量电池" },
        { "bomb", "爱的炸弹" },
        { "airdrop", "神秘空投" },
        { "superjet", "超能喷射" },
        { "join", "加入阵营" },
        { "soldier", "步兵" },
        { "tank", "坦克" },
        { "air_strike", "空中支援" },
    };

    public static string GetDisplayName(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "弹幕";
        }

        return Labels.TryGetValue(key.Trim(), out string label) ? label : key;
    }

    public static string FormatSpawnToast(FactionId faction, string giftKey, int count)
    {
        string gift = GetDisplayName(giftKey);
        string side = faction switch
        {
            FactionId.Blue => "蓝军",
            FactionId.Green => "绿军",
            FactionId.Zombie => "丧尸",
            _ => "阵营",
        };
        return count > 1 ? $"{side} · {gift} x{count}" : $"{side} · {gift}";
    }
}
