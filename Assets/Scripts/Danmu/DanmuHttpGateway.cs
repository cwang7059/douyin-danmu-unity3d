using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public sealed class DanmuHttpGateway : MonoBehaviour
{
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 8765;
    [SerializeField] private int maxPendingHttpMessages = 1024;
    [SerializeField] private int maxMessagesPerFrame = 64;

    private readonly Queue<PendingHttpMessage> pendingMessages = new Queue<PendingHttpMessage>();
    private readonly object pendingLock = new object();

    private HttpListener listener;
    private Thread listenerThread;
    private DanmuCommandQueue commandQueue;
    private volatile bool running;
    private string lastBackgroundError;
    private string lastDropReason;

    public bool IsRunning => running;
    public int Port => port;
    public int ReceivedMessageCount { get; private set; }
    public int AcceptedMessageCount { get; private set; }
    public int DroppedMessageCount { get; private set; }
    public int PendingHttpMessageCount
    {
        get
        {
            lock (pendingLock)
            {
                return pendingMessages.Count;
            }
        }
    }

    private void Awake()
    {
        commandQueue = GetComponent<DanmuCommandQueue>();
        ApplyCommandLineOverrides();
        if (startOnAwake)
        {
            StartGateway();
        }
    }

    private void Update()
    {
        FlushBackgroundError();
        DrainPendingMessages();
    }

    private void OnDestroy()
    {
        StopGateway();
    }

    public void StartGateway()
    {
        if (running)
        {
            return;
        }

        try
        {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://{host}:{port}/");
            listener.Start();
            running = true;

            listenerThread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "DanmuHttpGateway",
            };
            listenerThread.Start();
            Debug.Log($"[DanmuHttpGateway] Listening on http://{host}:{port}/");
        }
        catch (Exception ex)
        {
            running = false;
            Debug.LogWarning($"[DanmuHttpGateway] Failed to start on http://{host}:{port}/ : {ex.Message}");
        }
    }

    public void StopGateway()
    {
        running = false;

        if (listener != null)
        {
            try
            {
                listener.Stop();
                listener.Close();
            }
            catch (Exception)
            {
            }

            listener = null;
        }

        if (listenerThread != null)
        {
            listenerThread.Join(250);
            listenerThread = null;
        }
    }

    private void ListenLoop()
    {
        while (running && listener != null && listener.IsListening)
        {
            HttpListenerContext context = null;
            try
            {
                context = listener.GetContext();
                HandleRequest(context);
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                lastBackgroundError = ex.Message;
                if (context != null)
                {
                    WriteResponse(context, 500, "{\"ok\":false,\"error\":\"server_error\"}");
                }
            }
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        string path = context.Request.Url.AbsolutePath.Trim('/').ToLowerInvariant();
        if (context.Request.HttpMethod == "GET" && path == "health")
        {
            WriteResponse(context, 200, BuildStatusJson());
            return;
        }

        if (context.Request.HttpMethod == "GET" && path == "stats")
        {
            WriteResponse(context, 200, BuildStatusJson());
            return;
        }

        if (context.Request.HttpMethod != "POST" || (path != "danmu" && path != "gift" && path != "command"))
        {
            WriteResponse(context, 404, "{\"ok\":false,\"error\":\"not_found\"}");
            return;
        }

        string body;
        using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8))
        {
            body = reader.ReadToEnd();
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            WriteResponse(context, 400, "{\"ok\":false,\"queued\":false,\"error\":\"empty_body\"}");
            return;
        }

        bool queued = QueueHttpMessage(path, body);
        WriteResponse(context, queued ? 202 : 429, queued ? BuildQueuedJson() : "{\"ok\":false,\"queued\":false,\"error\":\"queue_full\"}");
    }

    private bool QueueHttpMessage(string path, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        lock (pendingLock)
        {
            if (pendingMessages.Count >= maxPendingHttpMessages)
            {
                DroppedMessageCount++;
                return false;
            }

            pendingMessages.Enqueue(new PendingHttpMessage
            {
                path = path,
                body = body,
            });
            ReceivedMessageCount++;
            return true;
        }
    }

    private void DrainPendingMessages()
    {
        if (commandQueue == null)
        {
            commandQueue = GetComponent<DanmuCommandQueue>();
        }

        if (commandQueue == null)
        {
            return;
        }

        int limit = Mathf.Max(1, maxMessagesPerFrame);
        for (int i = 0; i < limit; i++)
        {
            PendingHttpMessage pending;
            lock (pendingLock)
            {
                if (pendingMessages.Count <= 0)
                {
                    return;
                }

                pending = pendingMessages.Dequeue();
            }

            string dropReason;
            if (ApplyHttpMessage(pending, out dropReason))
            {
                AcceptedMessageCount++;
            }
            else
            {
                DroppedMessageCount++;
                lastDropReason = string.IsNullOrEmpty(dropReason) ? "command_rejected" : dropReason;
            }
        }
    }

    private bool ApplyHttpMessage(PendingHttpMessage pending, out string dropReason)
    {
        dropReason = string.Empty;
        DanmuHttpPayload payload;
        try
        {
            payload = JsonUtility.FromJson<DanmuHttpPayload>(pending.body);
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

        string eventType = string.IsNullOrWhiteSpace(payload.eventType) ? pending.path : payload.eventType.Trim().ToLowerInvariant();
        if (eventType == "gift")
        {
            bool accepted = commandQueue.EnqueueGift(payload.userId, payload.userName, payload.giftName, payload.giftValue);
            dropReason = accepted ? string.Empty : CommandQueueDropReason();
            return accepted;
        }

        if (!string.IsNullOrWhiteSpace(payload.team) || !string.IsNullOrWhiteSpace(payload.commandType) || !string.IsNullOrWhiteSpace(payload.key))
        {
            BattleTeam team = ParseTeam(payload.team);
            DanmuCommandType type = ParseCommandType(payload.commandType);
            string key = string.IsNullOrWhiteSpace(payload.key) ? payload.text : payload.key;
            if (team != BattleTeam.Neutral && type != DanmuCommandType.None)
            {
                bool accepted = commandQueue.Enqueue(DanmuCommand.Create(payload.userId, payload.userName, team, type, key, payload.value));
                dropReason = accepted ? string.Empty : CommandQueueDropReason();
                return accepted;
            }
        }

        bool rawAccepted = commandQueue.EnqueueRawMessage(payload.userId, payload.userName, payload.text);
        dropReason = rawAccepted ? string.Empty : CommandQueueDropReason();
        return rawAccepted;
    }

    private void ApplyCommandLineOverrides()
    {
        string value = GetArgumentValue("-danmuHttpPort");
        int parsedPort;
        if (int.TryParse(value, out parsedPort) && parsedPort > 0 && parsedPort <= 65535)
        {
            port = parsedPort;
        }

        if (HasArgument("-danmuHttpOff"))
        {
            startOnAwake = false;
        }
    }

    private void FlushBackgroundError()
    {
        if (string.IsNullOrEmpty(lastBackgroundError))
        {
            return;
        }

        string error = lastBackgroundError;
        lastBackgroundError = null;
        Debug.LogWarning($"[DanmuHttpGateway] Request failed: {error}");
    }

    private string CommandQueueDropReason()
    {
        if (commandQueue == null || string.IsNullOrEmpty(commandQueue.LastDropReason))
        {
            return "command_rejected";
        }

        return commandQueue.LastDropReason;
    }

    private string BuildQueuedJson()
    {
        return "{\"ok\":true,\"queued\":true,\"pendingHttpMessages\":" + PendingHttpMessageCount + "}";
    }

    private string BuildStatusJson()
    {
        return "{"
            + "\"ok\":true,"
            + "\"service\":\"danmu-http-gateway\","
            + "\"running\":" + (running ? "true" : "false") + ","
            + "\"port\":" + port + ","
            + "\"pendingHttpMessages\":" + PendingHttpMessageCount + ","
            + "\"receivedHttpMessages\":" + ReceivedMessageCount + ","
            + "\"acceptedHttpMessages\":" + AcceptedMessageCount + ","
            + "\"droppedHttpMessages\":" + DroppedMessageCount + ","
            + "\"pendingCommands\":" + (commandQueue != null ? commandQueue.PendingCount : 0) + ","
            + "\"acceptedCommands\":" + (commandQueue != null ? commandQueue.AcceptedCommandCount : 0) + ","
            + "\"droppedCommands\":" + (commandQueue != null ? commandQueue.DroppedCommandCount : 0) + ","
            + "\"lastAcceptedCommand\":\"" + JsonEscape(commandQueue != null ? commandQueue.LastAcceptedCommand : string.Empty) + "\","
            + "\"lastDropReason\":\"" + JsonEscape(lastDropReason) + "\""
            + "}";
    }

    private static string JsonEscape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static void WriteResponse(HttpListenerContext context, int statusCode, string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        context.Response.OutputStream.Close();
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

    private static bool HasArgument(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetArgumentValue(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return string.Empty;
    }

    private struct PendingHttpMessage
    {
        public string path;
        public string body;
    }

    [Serializable]
    private sealed class DanmuHttpPayload
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
}
