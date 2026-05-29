using System.Collections.Generic;
using UnityEngine;

public sealed class DanmuCommandQueue : MonoBehaviour
{
    [SerializeField] private int maxPendingCommands = 512;
    [SerializeField] private int maxCommandsPerFrame = 24;
    [SerializeField] private float defaultUserCooldown = 1.25f;

    private readonly Queue<DanmuCommand> pendingCommands = new Queue<DanmuCommand>();
    private readonly Dictionary<string, float> lastCommandTimes = new Dictionary<string, float>();
    private readonly object pendingLock = new object();

    public int PendingCount
    {
        get
        {
            lock (pendingLock)
            {
                return pendingCommands.Count;
            }
        }
    }
    public int MaxCommandsPerFrame => maxCommandsPerFrame;
    public int DroppedCommandCount { get; private set; }
    public int AcceptedCommandCount { get; private set; }
    public string LastDropReason { get; private set; }
    public string LastAcceptedCommand { get; private set; }

    public bool Enqueue(DanmuCommand command)
    {
        if (!command.IsValid)
        {
            DroppedCommandCount++;
            LastDropReason = "invalid_command";
            return false;
        }

        if (IsCoolingDown(command))
        {
            DroppedCommandCount++;
            LastDropReason = "user_cooldown";
            return false;
        }

        lock (pendingLock)
        {
            if (pendingCommands.Count >= maxPendingCommands)
            {
                DroppedCommandCount++;
                LastDropReason = "queue_full";
                return false;
            }

            pendingCommands.Enqueue(command);
            AcceptedCommandCount++;
            LastAcceptedCommand = $"{command.team}:{command.type}:{command.key}";
        }
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
        lock (pendingLock)
        {
            if (pendingCommands.Count <= 0)
            {
                command = default;
                return false;
            }

            command = pendingCommands.Dequeue();
            return true;
        }
    }

    public void Clear()
    {
        lock (pendingLock)
        {
            pendingCommands.Clear();
            lastCommandTimes.Clear();
            DroppedCommandCount = 0;
            AcceptedCommandCount = 0;
            LastDropReason = string.Empty;
            LastAcceptedCommand = string.Empty;
        }
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
