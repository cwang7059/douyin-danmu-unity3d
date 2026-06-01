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
        string eventType = string.IsNullOrWhiteSpace(pending.path) ? "danmu" : pending.path.Trim().ToLowerInvariant();
        return DanmuLiveIngress.TryApplyJson(pending.body, eventType, commandQueue, out dropReason);
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
}
