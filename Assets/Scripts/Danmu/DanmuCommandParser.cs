using System;

public static class DanmuCommandParser
{
    public static bool TryParse(string userId, string userName, string rawText, out DanmuCommand command)
    {
        command = default;
        string text = Normalize(rawText);
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        BattleTeam team = ParseTeam(text);
        DanmuCommandType type = ParseType(text);
        string key = ParseKey(text, team, type);

        if (team == BattleTeam.Neutral || type == DanmuCommandType.None)
        {
            return false;
        }

        int value = type == DanmuCommandType.CastSkill ? 1 : 10;
        command = DanmuCommand.Create(userId, userName, team, type, key, value);
        return true;
    }

    public static bool TryParseGift(string userId, string userName, string giftName, int giftValue, out DanmuCommand command)
    {
        command = default;
        string text = Normalize(giftName);
        BattleTeam team = ParseTeam(text);
        if (team == BattleTeam.Neutral)
        {
            team = giftValue % 2 == 0 ? BattleTeam.Orc : BattleTeam.Human;
        }

        DanmuCommandType type = giftValue >= 100 ? DanmuCommandType.CastSkill : DanmuCommandType.AddEnergy;
        string key = type == DanmuCommandType.CastSkill
            ? team == BattleTeam.Human ? "air_strike" : "rage"
            : "gift_energy";
        command = DanmuCommand.Create(userId, userName, team, type, key, Math.Max(1, giftValue));
        return true;
    }

    private static string Normalize(string rawText)
    {
        return string.IsNullOrWhiteSpace(rawText)
            ? string.Empty
            : rawText.Trim().ToLowerInvariant();
    }

    private static BattleTeam ParseTeam(string text)
    {
        if (ContainsAny(text, "人族", "人", "蓝", "blue", "human", "humans", "1"))
        {
            return BattleTeam.Human;
        }

        if (ContainsAny(text, "兽族", "兽", "红", "orc", "orcs", "monster", "monsters", "2"))
        {
            return BattleTeam.Orc;
        }

        return BattleTeam.Neutral;
    }

    private static DanmuCommandType ParseType(string text)
    {
        if (ContainsAny(text, "空袭", "狂暴", "技能", "skill", "strike", "rage", "裂地"))
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

        if (ContainsAny(text, "兵", "坦克", "tank", "狼", "地狱犬", "dog", "spawn", "召唤", "人族", "兽族", "human", "orc", "1", "2"))
        {
            return DanmuCommandType.SpawnUnit;
        }

        return DanmuCommandType.None;
    }

    private static string ParseKey(string text, BattleTeam team, DanmuCommandType type)
    {
        if (type == DanmuCommandType.CastSkill)
        {
            if (ContainsAny(text, "空袭", "strike"))
            {
                return "air_strike";
            }

            if (ContainsAny(text, "裂地"))
            {
                return "earth_split";
            }

            return team == BattleTeam.Human ? "air_strike" : "rage";
        }

        if (ContainsAny(text, "坦克", "tank"))
        {
            return "tank";
        }

        if (ContainsAny(text, "治疗", "heal", "医疗"))
        {
            return "medic";
        }

        if (ContainsAny(text, "地狱犬", "dog", "狼"))
        {
            return "helldog";
        }

        return team == BattleTeam.Human ? "soldier" : "orc_grunt";
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

