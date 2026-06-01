using System;
using UnityEngine;

/// <summary>Shared JSON payload for HTTP / WebSocket / external live SDK bridges.</summary>
[Serializable]
public sealed class DanmuLivePayload
{
    public string eventType;
    public string userId;
    public string userName;
    public string text;
    public string team;
    public string commandType;
    public string key;
    public int value;
    public string giftName;
    public int giftValue;
}
