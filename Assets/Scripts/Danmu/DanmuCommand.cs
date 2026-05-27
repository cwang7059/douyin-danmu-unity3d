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
    public DanmuCommandType type;
    public string key;
    public int value;
    public float receivedTime;

    public bool IsValid => type != DanmuCommandType.None && team != BattleTeam.Neutral;

    public static DanmuCommand Create(string userId, string userName, BattleTeam team, DanmuCommandType type, string key, int value)
    {
        return new DanmuCommand
        {
            userId = userId ?? string.Empty,
            userName = userName ?? string.Empty,
            team = team,
            type = type,
            key = key ?? string.Empty,
            value = Mathf.Max(0, value),
            receivedTime = Time.realtimeSinceStartup,
        };
    }
}

