using System;

public static class DanmuCommandParser
{
    private static DanmuSpawnMapping[] humanSpawnMappings;

    public static void ConfigureHumanSpawnMappings(DanmuSpawnMapping[] mappings)
    {
        humanSpawnMappings = mappings != null && mappings.Length > 0
            ? mappings
            : DanmuSpawnMapping.CreateDefaultHumanMappings();
    }

    private static DanmuSpawnMapping[] ActiveHumanSpawnMappings()
    {
        return humanSpawnMappings ?? DanmuSpawnMapping.CreateDefaultHumanMappings();
    }

    public static bool TryParse(string userId, string userName, string rawText, out DanmuCommand command)
    {
        command = default;
        string text = Normalize(rawText);
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (TryParseJoinOnly(text, userId, userName, out command))
        {
            return true;
        }

        if (TryParseLike(text, userId, userName, out command))
        {
            return true;
        }

        FactionId faction = ParseFaction(text);
        BattleTeam team = DanmuCommand.TeamFromFaction(faction);
        DanmuCommandType type = ParseType(text);
        string key = ParseKey(text, faction, team, type);

        if (faction == FactionId.Neutral && team == BattleTeam.Neutral)
        {
            return false;
        }

        if (type == DanmuCommandType.None)
        {
            return false;
        }

        int value = ResolveValue(text, type, key);
        command = DanmuCommand.Create(userId, userName, team, faction, type, key, value);
        return true;
    }

    public static bool TryParseGift(string userId, string userName, string giftName, int giftValue, out DanmuCommand command)
    {
        command = default;
        string text = Normalize(giftName);
        FactionId faction = ParseFaction(text);
        if (faction == FactionId.Neutral)
        {
            faction = (FactionId)(Math.Abs(giftValue) % 3 + 1);
        }

        if (ApocalypseGiftCatalog.TryResolveGiftKey(text, out string giftKey)
            && string.Equals(giftKey, "superjet", StringComparison.OrdinalIgnoreCase))
        {
            command = DanmuCommand.Create(userId, userName, BattleTeam.Human, FactionId.Blue, DanmuCommandType.CastSkill, giftKey, Math.Max(1, giftValue));
            return true;
        }

        BattleTeam team = DanmuCommand.TeamFromFaction(faction);
        command = DanmuCommand.Create(userId, userName, team, faction, DanmuCommandType.SpawnUnit, ResolveGiftKey(text), Math.Max(1, giftValue));
        return true;
    }

    private static string ResolveGiftKey(string text)
    {
        return ApocalypseGiftCatalog.TryResolveGiftKey(text, out string key) ? key : text;
    }

    private static bool TryParseJoinOnly(string text, string userId, string userName, out DanmuCommand command)
    {
        command = default;
        FactionId faction = FactionId.Neutral;

        if (text == "1" || text == "加入蓝军" || text == "蓝军")
        {
            faction = FactionId.Blue;
        }
        else if (text == "2" || text == "加入绿军" || text == "绿军")
        {
            faction = FactionId.Green;
        }
        else if (text == "3" || text == "加入丧尸" || text == "丧尸" || text == "丧尸大军")
        {
            faction = FactionId.Zombie;
        }

        if (faction == FactionId.Neutral)
        {
            return false;
        }

        command = DanmuCommand.Create(userId, userName, DanmuCommand.TeamFromFaction(faction), faction, DanmuCommandType.JoinFaction, "join", 1);
        return true;
    }

    private static bool TryParseLike(string text, string userId, string userName, out DanmuCommand command)
    {
        command = default;
        if (!ContainsAny(text, "点赞", "like", "赞"))
        {
            return false;
        }

        FactionId faction = ParseFaction(text);
        if (faction == FactionId.Neutral)
        {
            faction = FactionId.Blue;
        }

        command = DanmuCommand.Create(userId, userName, DanmuCommand.TeamFromFaction(faction), faction, DanmuCommandType.Like, "like", 1);
        return true;
    }

    private static int ResolveValue(string text, DanmuCommandType type, string key)
    {
        if (text.IndexOf("666", StringComparison.Ordinal) >= 0)
        {
            return 100;
        }

        if (type == DanmuCommandType.Like)
        {
            return 10;
        }

        if (type == DanmuCommandType.CastSkill)
        {
            return 1;
        }

        return 10;
    }

    private static string Normalize(string rawText)
    {
        return string.IsNullOrWhiteSpace(rawText)
            ? string.Empty
            : rawText.Trim().ToLowerInvariant();
    }

    private static FactionId ParseFaction(string text)
    {
        if (text == "1" || ContainsAny(text, "蓝军", "蓝", "blue", "人族", "human"))
        {
            return FactionId.Blue;
        }

        if (text == "2" || ContainsAny(text, "绿军", "绿", "green"))
        {
            return FactionId.Green;
        }

        if (text == "3" || ContainsAny(text, "丧尸", "尸", "zombie", "兽族", "兽", "orc", "monster"))
        {
            return FactionId.Zombie;
        }

        return FactionId.Neutral;
    }

    private static DanmuCommandType ParseType(string text)
    {
        if (text.IndexOf("666", StringComparison.Ordinal) >= 0)
        {
            return DanmuCommandType.SpawnUnit;
        }

        if (ContainsAny(text, "点赞", "like", "赞"))
        {
            return DanmuCommandType.Like;
        }

        if (ContainsAny(text, "空袭", "狂暴", "技能", "skill", "strike", "rage", "裂地", "超能喷射", "喷射", "空中支援"))
        {
            return DanmuCommandType.CastSkill;
        }

        if (ContainsAny(text, "治疗", "回血", "heal"))
        {
            return DanmuCommandType.Heal;
        }

        if (ContainsAny(text, "buff", "强化", "加攻", "加速"))
        {
            return DanmuCommandType.Buff;
        }

        if (ContainsAny(text, "能量", "energy", "充能"))
        {
            return DanmuCommandType.AddEnergy;
        }

        if (ApocalypseGiftCatalog.TryResolveGiftKey(text, out _))
        {
            return DanmuCommandType.SpawnUnit;
        }

        if (ContainsAny(text, "兵", "坦克", "tank", "狼", "地狱犬", "dog", "spawn", "召唤", "人族", "兽族", "human", "orc"))
        {
            return DanmuCommandType.SpawnUnit;
        }

        return DanmuCommandType.None;
    }

    private static string ParseKey(string text, FactionId faction, BattleTeam team, DanmuCommandType type)
    {
        if (text.IndexOf("666", StringComparison.Ordinal) >= 0)
        {
            return "666";
        }

        if (type == DanmuCommandType.Like)
        {
            return "like";
        }

        if (ApocalypseGiftCatalog.TryResolveGiftKey(text, out string giftKey))
        {
            return giftKey;
        }

        if (type == DanmuCommandType.CastSkill)
        {
            if (ContainsAny(text, "超能喷射", "喷射", "空中支援"))
            {
                return "superjet";
            }

            if (ContainsAny(text, "空袭", "strike"))
            {
                return "air_strike";
            }

            if (ContainsAny(text, "裂地"))
            {
                return "earth_split";
            }

            return faction == FactionId.Zombie ? "rage" : "air_strike";
        }

        if ((faction == FactionId.Blue || faction == FactionId.Green)
            && (type == DanmuCommandType.SpawnUnit || type == DanmuCommandType.Heal))
        {
            if (DanmuSpawnMapping.TryResolveHumanSpawnKeyFromText(text, ActiveHumanSpawnMappings(), out string humanKey))
            {
                return humanKey;
            }

            return type == DanmuCommandType.Heal ? "medic" : "soldier";
        }

        if (ContainsAny(text, "地狱犬", "dog", "狼"))
        {
            return "helldog";
        }

        return faction == FactionId.Zombie ? "orc_grunt" : "soldier";
    }

    private static bool ContainsAny(string text, params string[] tokens)
    {
        for (int i = 0; i < tokens.Length; i++)
        {
            if (text.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
