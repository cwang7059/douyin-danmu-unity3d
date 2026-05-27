using System.Collections.Generic;
using UnityEngine;

public sealed class DanmuCommandQueue : MonoBehaviour
{
    [SerializeField] private int maxPendingCommands = 512;
    [SerializeField] private int maxCommandsPerFrame = 24;
    [SerializeField] private float defaultUserCooldown = 1.25f;

    private readonly Queue<DanmuCommand> pendingCommands = new Queue<DanmuCommand>();
    private readonly Dictionary<string, float> lastCommandTimes = new Dictionary<string, float>();

    public int PendingCount => pendingCommands.Count;
    public int MaxCommandsPerFrame => maxCommandsPerFrame;
    public int DroppedCommandCount { get; private set; }
    public int AcceptedCommandCount { get; private set; }

    public bool Enqueue(DanmuCommand command)
    {
        if (!command.IsValid || IsCoolingDown(command))
        {
            return false;
        }

        if (pendingCommands.Count >= maxPendingCommands)
        {
            DroppedCommandCount++;
            return false;
        }

        pendingCommands.Enqueue(command);
        AcceptedCommandCount++;
        RememberCooldown(command);
        return true;
    }

    public bool EnqueueRawMessage(string userId, string userName, string rawText)
    {
        DanmuCommand command;
        return DanmuCommandParser.TryParse(userId, userName, rawText, out command) && Enqueue(command);
    }

    public bool EnqueueGift(string userId, string userName, string giftName, int giftValue)
    {
        DanmuCommand command;
        return DanmuCommandParser.TryParseGift(userId, userName, giftName, giftValue, out command) && Enqueue(command);
    }

    public bool TryDequeue(out DanmuCommand command)
    {
        if (pendingCommands.Count <= 0)
        {
            command = default;
            return false;
        }

        command = pendingCommands.Dequeue();
        return true;
    }

    public void Clear()
    {
        pendingCommands.Clear();
        lastCommandTimes.Clear();
        DroppedCommandCount = 0;
        AcceptedCommandCount = 0;
    }

    private bool IsCoolingDown(DanmuCommand command)
    {
        if (defaultUserCooldown <= 0f)
        {
            return false;
        }

        string key = CooldownKey(command);
        float lastTime;
        return lastCommandTimes.TryGetValue(key, out lastTime) && Time.realtimeSinceStartup - lastTime < defaultUserCooldown;
    }

    private void RememberCooldown(DanmuCommand command)
    {
        lastCommandTimes[CooldownKey(command)] = Time.realtimeSinceStartup;
    }

    private static string CooldownKey(DanmuCommand command)
    {
        return $"{command.userId}:{command.team}:{command.type}:{command.key}";
    }
}

