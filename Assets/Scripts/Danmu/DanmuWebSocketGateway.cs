using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Connects to an external live bridge over WebSocket and enqueues danmu JSON into <see cref="DanmuCommandQueue"/>.
/// Default URL: ws://127.0.0.1:8766/danmu — use <c>tools/live-danmu-bridge/ws-relay.ps1</c> or your SDK adapter.
/// </summary>
public sealed class DanmuWebSocketGateway : MonoBehaviour
{
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private string webSocketUrl = "ws://127.0.0.1:8766/danmu";
    [SerializeField] private int maxPendingMessages = 1024;
    [SerializeField] private int maxMessagesPerFrame = 64;
    [SerializeField] private float reconnectDelaySeconds = 2f;

    private readonly Queue<PendingWsMessage> pendingMessages = new Queue<PendingWsMessage>();
    private readonly object pendingLock = new object();

    private DanmuCommandQueue commandQueue;
    private CancellationTokenSource connectionCts;
    private Task connectionTask;
    private volatile bool running;
    private string lastBackgroundError;
    private string lastDropReason;

    public bool IsRunning => running;
    public string WebSocketUrl => webSocketUrl;
    public int ReceivedMessageCount { get; private set; }
    public int AcceptedMessageCount { get; private set; }
    public int DroppedMessageCount { get; private set; }
    public int PendingMessageCount
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
        if (running || string.IsNullOrWhiteSpace(webSocketUrl))
        {
            return;
        }

        running = true;
        connectionCts = new CancellationTokenSource();
        connectionTask = Task.Run(() => ConnectionLoop(connectionCts.Token));
        Debug.Log($"[DanmuWebSocketGateway] Connecting to {webSocketUrl}");
    }

    public void StopGateway()
    {
        running = false;
        if (connectionCts != null)
        {
            try
            {
                connectionCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            connectionCts.Dispose();
            connectionCts = null;
        }
    }

    private async Task ConnectionLoop(CancellationToken token)
    {
        while (running && !token.IsCancellationRequested)
        {
            try
            {
                using (var socket = new ClientWebSocket())
                {
                    var uri = new Uri(webSocketUrl);
                    await socket.ConnectAsync(uri, token).ConfigureAwait(false);
                    Debug.Log($"[DanmuWebSocketGateway] Connected to {webSocketUrl}");

                    var buffer = new byte[8192];
                    while (running && !token.IsCancellationRequested && socket.State == WebSocketState.Open)
                    {
                        var segment = new ArraySegment<byte>(buffer);
                        WebSocketReceiveResult result = await socket.ReceiveAsync(segment, token).ConfigureAwait(false);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }

                        if (result.MessageType != WebSocketMessageType.Text || result.Count <= 0)
                        {
                            continue;
                        }

                        string body = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        if (!result.EndOfMessage)
                        {
                            body = await ReadFullTextMessage(socket, buffer, result, body, token).ConfigureAwait(false);
                        }

                        EnqueueBackgroundMessage(body);
                    }

                    if (socket.State == WebSocketState.Open)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "shutdown", CancellationToken.None).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                lastBackgroundError = ex.Message;
            }

            if (!running || token.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Mathf.Max(0.5f, reconnectDelaySeconds)), token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static async Task<string> ReadFullTextMessage(
        ClientWebSocket socket,
        byte[] buffer,
        WebSocketReceiveResult firstResult,
        string initialText,
        CancellationToken token)
    {
        var builder = new StringBuilder(initialText);
        WebSocketReceiveResult result = firstResult;
        while (!result.EndOfMessage && socket.State == WebSocketState.Open)
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            if (result.Count > 0)
            {
                builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
        }

        return builder.ToString();
    }

    private void EnqueueBackgroundMessage(string body)
    {
        ReceivedMessageCount++;
        lock (pendingLock)
        {
            if (pendingMessages.Count >= maxPendingMessages)
            {
                DroppedMessageCount++;
                lastDropReason = "ws_queue_full";
                return;
            }

            pendingMessages.Enqueue(new PendingWsMessage { body = body });
        }
    }

    private void DrainPendingMessages()
    {
        int processed = 0;
        while (processed < maxMessagesPerFrame)
        {
            PendingWsMessage pending;
            lock (pendingLock)
            {
                if (pendingMessages.Count == 0)
                {
                    break;
                }

                pending = pendingMessages.Dequeue();
            }

            processed++;
            string dropReason;
            if (DanmuLiveIngress.TryApplyJson(pending.body, "danmu", commandQueue, out dropReason))
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

    private void FlushBackgroundError()
    {
        if (string.IsNullOrEmpty(lastBackgroundError))
        {
            return;
        }

        string error = lastBackgroundError;
        lastBackgroundError = null;
        Debug.LogWarning($"[DanmuWebSocketGateway] {error}");
    }

    private void ApplyCommandLineOverrides()
    {
        string url = GetArgumentValue("-danmuWsUrl");
        if (!string.IsNullOrWhiteSpace(url))
        {
            webSocketUrl = url;
        }

        if (HasArgument("-danmuWsOff"))
        {
            startOnAwake = false;
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

    private struct PendingWsMessage
    {
        public string body;
    }
}
