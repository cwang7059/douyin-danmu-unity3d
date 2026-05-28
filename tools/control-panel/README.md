# 手机端模拟控制面板

这是一个独立的 Electron 控制面板，用来模拟手机端直播平台按钮，并通过 Unity 本地弹幕网关发送增援、技能、治疗、加速和礼物事件。

## 启动

项目根目录双击：

```powershell
.\start-control-panel.bat
```

或手动启动：

```powershell
cd tools/control-panel
npm install
npm start
```

默认网关地址是：

```text
http://127.0.0.1:8765
```

运行 Unity 游戏后，面板顶部状态显示“在线”即可发送按钮事件。

## 按钮映射

| 面板按钮 | HTTP 接口 | 结构化命令 |
| --- | --- | --- |
| 士兵 | `POST /command` | `team=human`, `commandType=spawn`, `key=soldier` |
| 坦克 | `POST /command` | `team=human`, `commandType=spawn`, `key=tank` |
| 直升机 | `POST /command` | `team=human`, `commandType=spawn`, `key=aircraft` |
| 空袭 | `POST /command` | `team=human`, `commandType=skill`, `key=air_strike` |
| 治疗 | `POST /command` | `team=human`, `commandType=heal`, `key=medic` |
| 集火 | `POST /command` | `team=human`, `commandType=buff`, `key=focus_fire` |
| 怪物 | `POST /command` | `team=orc`, `commandType=spawn`, `key=orc_grunt` |
| 狂暴 | `POST /command` | `team=orc`, `commandType=skill`, `key=rage` |
| 回血 | `POST /command` | `team=orc`, `commandType=heal`, `key=heal` |
| 加速 | `POST /command` | `team=orc`, `commandType=buff`, `key=haste` |
| 礼物 | `POST /gift` | `giftName=<team> mobile panel gift`, `giftValue=面板输入值` |

数量按钮 `1 / 5 / 10` 会连续发送多次请求，保证兼容当前 Unity 侧逐条处理弹幕命令的逻辑。
