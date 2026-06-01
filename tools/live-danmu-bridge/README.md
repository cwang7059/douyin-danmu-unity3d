# 直播弹幕桥接

游戏内已提供两种入队方式，最终都进入 `DanmuCommandQueue`：

| 方式 | 地址 | 说明 |
| --- | --- | --- |
| HTTP | `POST http://127.0.0.1:8765/danmu` | 默认开启，见 `doc/本地弹幕网关.md` |
| WebSocket | `ws://127.0.0.1:8766/danmu` | `DanmuWebSocketGateway`，需外部桥接服务推送 JSON |

## JSON 格式

与 HTTP 网关相同，例如：

```json
{"eventType":"danmu","userId":"u1","userName":"测试","text":"human soldier"}
```

结构化命令：

```json
{"eventType":"command","userId":"u2","userName":"测试","team":"orc","commandType":"spawn","key":"helldog","value":1}
```

## 测试 WebSocket

1. 启动游戏（确保未加 `-danmuWsOff`）。
2. 运行：

```powershell
.\tools\live-danmu-bridge\ws-relay.ps1
```

按提示输入 JSON 行即可。

同时镜像到 HTTP（双通道调试）：

```powershell
.\tools\live-danmu-bridge\ws-relay.ps1 -MirrorToHttp
```

从文件批量发送：

```powershell
.\tools\live-danmu-bridge\ws-relay.ps1 -InputFile .\samples.json
```

## 命令行

```text
-danmuWsUrl ws://127.0.0.1:9000/live
-danmuWsOff
```

## 接入真实直播 SDK

在 Node/Python/Go 中连接平台 WebSocket，收到弹幕后 `ws.send(JSON.stringify(payload))` 到游戏地址即可。也可只走 HTTP：`POST /danmu`，无需 WebSocket。
