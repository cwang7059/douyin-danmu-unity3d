# 本地 HTTP 弹幕网关

更新时间：2026-05-27

游戏启动后会默认监听：

```text
http://127.0.0.1:8765/
```

外部直播 SDK、Python/Node 桥接服务、OBS 插件或测试脚本，只要把弹幕事件 POST 到游戏本地端口，就能驱动当前战场。

## 启动

默认启动：

```powershell
.\start-game.ps1
```

指定端口：

```powershell
.\start-game.ps1 -DanmuHttpPort 8787
```

关闭 HTTP 弹幕网关：

```powershell
.\start-game.ps1 -DisableDanmuHttp
```

## 健康检查

```powershell
Invoke-RestMethod http://127.0.0.1:8765/health
```

返回：

```json
{"ok":true,"service":"danmu-http-gateway"}
```

## 普通弹幕

接口：

```text
POST /danmu
```

请求：

```json
{
  "eventType": "danmu",
  "userId": "u001",
  "userName": "Alice",
  "text": "human soldier"
}
```

也可以发送：

```json
{
  "eventType": "danmu",
  "userId": "u002",
  "userName": "Bob",
  "text": "orc helldog"
}
```

## 结构化命令

接口：

```text
POST /command
```

人族空袭：

```json
{
  "eventType": "command",
  "userId": "u003",
  "userName": "Cathy",
  "team": "human",
  "commandType": "skill",
  "key": "air_strike",
  "value": 100
}
```

兽族狂暴：

```json
{
  "eventType": "command",
  "userId": "u004",
  "userName": "Dan",
  "team": "orc",
  "commandType": "skill",
  "key": "rage",
  "value": 100
}
```

字段说明：

| 字段 | 说明 |
| --- | --- |
| `team` | `human` / `orc` / `blue` / `red` / `1` / `2` |
| `commandType` | `spawn` / `skill` / `energy` / `heal` / `buff` |
| `key` | `soldier` / `tank` / `helldog` / `air_strike` / `rage` |
| `value` | 权重或礼物价值，默认可传 `1` 或 `100` |

## 礼物

接口：

```text
POST /gift
```

请求：

```json
{
  "eventType": "gift",
  "userId": "u005",
  "userName": "Eva",
  "giftName": "human air strike gift",
  "giftValue": 120
}
```

当前规则：

- `giftValue >= 100` 会按技能处理。
- 礼物名称里能解析出 `human` 或 `orc` 就按对应阵营处理。
- 没有阵营信息时按礼物值奇偶分配阵营。

## 测试脚本

启动游戏后运行：

```powershell
.\tools\send-danmu-test.ps1
```

包含礼物测试：

```powershell
.\tools\send-danmu-test.ps1 -Gift
```

指定端口：

```powershell
.\tools\send-danmu-test.ps1 -HostUrl http://127.0.0.1:8787
```

