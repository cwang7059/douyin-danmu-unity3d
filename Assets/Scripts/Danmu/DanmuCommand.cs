using System;
using UnityEngine;

public enum BattleTeam
{
    Neutral = 0,
    Human = 1,
    Orc = 2,
}

public enum DanmuCommandType
{
    None = 0,
    JoinFaction,
    Like,
    SpawnUnit,
    CastSkill,
    AddEnergy,
    Heal,
    Buff,
}

[Serializable]
public struct DanmuCommand
{
    public string userId;
    public string userName;
    public BattleTeam team;
    public FactionId faction;
    public DanmuCommandType type;
    public string key;
    public int value;
    public float receivedTime;

    public bool IsValid =>
        type != DanmuCommandType.None
        && (type == DanmuCommandType.JoinFaction ? faction != FactionId.Neutral : team != BattleTeam.Neutral || faction != FactionId.Neutral);

    public static DanmuCommand Create(string userId, string userName, BattleTeam team, DanmuCommandType type, string key, int value)
    {
        return Create(userId, userName, team, FactionFromTeam(team), type, key, value);
    }

    public static DanmuCommand Create(string userId, string userName, BattleTeam team, FactionId faction, DanmuCommandType type, string key, int value)
    {
        return new DanmuCommand
        {
            userId = userId ?? string.Empty,
            userName = userName ?? string.Empty,
            team = team,
            faction = faction,
            type = type,
            key = key ?? string.Empty,
            value = Mathf.Max(0, value),
            receivedTime = Time.realtimeSinceStartup,
        };
    }

    public static FactionId FactionFromTeam(BattleTeam team)
    {
        switch (team)
        {
            case BattleTeam.Human:
                return FactionId.Blue;
            case BattleTeam.Orc:
                return FactionId.Zombie;
            default:
                return FactionId.Neutral;
        }
    }

    public static BattleTeam TeamFromFaction(FactionId faction)
    {
        switch (faction)
        {
            case FactionId.Blue:
            case FactionId.Green:
                return BattleTeam.Human;
            case FactionId.Zombie:
                return BattleTeam.Orc;
            default:
                return BattleTeam.Neutral;
        }
    }
}

