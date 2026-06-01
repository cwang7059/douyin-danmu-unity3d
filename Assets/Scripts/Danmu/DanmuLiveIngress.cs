using System;

/// <summary>Routes live-platform JSON into <see cref="DanmuCommandQueue"/>.</summary>
public static class DanmuLiveIngress
{
    public static bool TryApplyJson(string body, string defaultEventType, DanmuCommandQueue queue, out string dropReason)
    {
        dropReason = string.Empty;
        if (queue == null)
        {
            dropReason = "missing_queue";
            return false;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            dropReason = "empty_body";
            return false;
        }

        DanmuLivePayload payload;
        try
        {
            payload = JsonUtility.FromJson<DanmuLivePayload>(body);
        }
        catch (Exception)
        {
            dropReason = "invalid_json";
            return false;
        }

        if (payload == null)
        {
            dropReason = "invalid_payload";
            return false;
        }

        return TryApplyPayload(payload, defaultEventType, queue, out dropReason);
    }

    public static bool TryApplyPayload(DanmuLivePayload payload, string defaultEventType, DanmuCommandQueue queue, out string dropReason)
    {
        dropReason = string.Empty;
        if (queue == null || payload == null)
        {
            dropReason = "invalid_payload";
            return false;
        }

        string eventType = string.IsNullOrWhiteSpace(payload.eventType)
            ? (string.IsNullOrWhiteSpace(defaultEventType) ? "danmu" : defaultEventType.Trim().ToLowerInvariant())
            : payload.eventType.Trim().ToLowerInvariant();

        if (eventType == "gift")
        {
            bool accepted = queue.EnqueueGift(payload.userId, payload.userName, payload.giftName, payload.giftValue);
            dropReason = accepted ? string.Empty : CommandQueueDropReason(queue);
            return accepted;
        }

        if (!string.IsNullOrWhiteSpace(payload.team)
            || !string.IsNullOrWhiteSpace(payload.commandType)
            || !string.IsNullOrWhiteSpace(payload.key))
        {
            BattleTeam team = ParseTeam(payload.team);
            DanmuCommandType type = ParseCommandType(payload.commandType);
            string key = string.IsNullOrWhiteSpace(payload.key) ? payload.text : payload.key;
            if (team != BattleTeam.Neutral && type != DanmuCommandType.None)
            {
                bool accepted = queue.Enqueue(DanmuCommand.Create(payload.userId, payload.userName, team, type, key, payload.value));
                dropReason = accepted ? string.Empty : CommandQueueDropReason(queue);
                return accepted;
            }
        }

        bool rawAccepted = queue.EnqueueRawMessage(payload.userId, payload.userName, payload.text);
        dropReason = rawAccepted ? string.Empty : CommandQueueDropReason(queue);
        return rawAccepted;
    }

    private static string CommandQueueDropReason(DanmuCommandQueue queue)
    {
        if (queue == null || string.IsNullOrEmpty(queue.LastDropReason))
        {
            return "command_rejected";
        }

        return queue.LastDropReason;
    }

    private static BattleTeam ParseTeam(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return BattleTeam.Neutral;
        }

        value = value.Trim().ToLowerInvariant();
        if (value == "1" || value == "human" || value == "humans" || value == "blue")
        {
            return BattleTeam.Human;
        }

        if (value == "2" || value == "orc" || value == "orcs" || value == "monster" || value == "monsters" || value == "red")
        {
            return BattleTeam.Orc;
        }

        return BattleTeam.Neutral;
    }

    private static DanmuCommandType ParseCommandType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DanmuCommandType.None;
        }

        value = value.Trim().ToLowerInvariant();
        switch (value)
        {
            case "spawn":
            case "spawnunit":
            case "spawn_unit":
                return DanmuCommandType.SpawnUnit;
            case "skill":
            case "cast":
            case "castskill":
            case "cast_skill":
                return DanmuCommandType.CastSkill;
            case "energy":
            case "addenergy":
            case "add_energy":
                return DanmuCommandType.AddEnergy;
            case "heal":
                return DanmuCommandType.Heal;
            case "buff":
                return DanmuCommandType.Buff;
            default:
                return DanmuCommandType.None;
        }
    }
}
