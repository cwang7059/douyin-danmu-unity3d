param(
    [string]$HostUrl = "http://127.0.0.1:8765",
    [switch]$Gift,
    [switch]$Full,
    [switch]$Apocalypse
)

$ErrorActionPreference = "Stop"

function Send-Danmu {
    param(
        [string]$Path,
        [hashtable]$Payload
    )

    $json = $Payload | ConvertTo-Json -Compress
    $url = "$HostUrl/$Path"
    Write-Host "POST $url $json"
    Invoke-RestMethod -Method Post -Uri $url -Body $json -ContentType "application/json; charset=utf-8" | Out-Host
}

Invoke-RestMethod -Method Get -Uri "$HostUrl/health" | Out-Host

if ($Apocalypse) {
    Write-Host "[Apocalypse] 请先在游戏内按 Enter 开战，再发送下列弹幕…"
    foreach ($pair in @(
            @{ u = "apk-blue"; n = "蓝军"; t = "1" },
            @{ u = "apk-green"; n = "绿军"; t = "2" },
            @{ u = "apk-zombie"; n = "丧尸"; t = "3" }
        )) {
        Send-Danmu -Path "danmu" -Payload @{
            eventType = "danmu"
            userId = $pair.u
            userName = $pair.n
            text = $pair.t
        }
        Start-Sleep -Milliseconds 250
    }

    Send-Danmu -Path "danmu" -Payload @{ eventType = "danmu"; userId = "apk-like"; userName = "点赞"; text = "点赞" }
    Start-Sleep -Milliseconds 250
    Send-Danmu -Path "danmu" -Payload @{ eventType = "danmu"; userId = "apk-666"; userName = "666"; text = "666" }
    Start-Sleep -Milliseconds 250
    Send-Danmu -Path "danmu" -Payload @{ eventType = "danmu"; userId = "apk-gift"; userName = "仙女棒"; text = "仙女棒" }
    Start-Sleep -Milliseconds 250
    Send-Danmu -Path "gift" -Payload @{
        eventType = "gift"
        userId = "apk-super"
        userName = "超能喷射"
        giftName = "超能喷射"
        giftValue = 1200
    }
    Write-Host "[OK] Apocalypse King test sequence sent."
    exit 0
}

Send-Danmu -Path "danmu" -Payload @{
    eventType = "danmu"
    userId = "tester-human"
    userName = "Tester Human"
    text = "human soldier"
}

Start-Sleep -Milliseconds 300

if ($Full) {
    Send-Danmu -Path "danmu" -Payload @{
        eventType = "danmu"
        userId = "tester-tank"
        userName = "Tester Tank"
        text = "人族坦克"
    }

    Start-Sleep -Milliseconds 300

    Send-Danmu -Path "danmu" -Payload @{
        eventType = "danmu"
        userId = "tester-air"
        userName = "Tester Air"
        text = "human helicopter"
    }

    Start-Sleep -Milliseconds 300

    Send-Danmu -Path "danmu" -Payload @{
        eventType = "danmu"
        userId = "tester-medic"
        userName = "Tester Medic"
        text = "人族 medic heal"
    }

    Start-Sleep -Milliseconds 300

    Send-Danmu -Path "danmu" -Payload @{
        eventType = "danmu"
        userId = "tester-unknown"
        userName = "Tester Unknown"
        text = "human mystery_unit"
    }

    Start-Sleep -Milliseconds 300
}

Send-Danmu -Path "danmu" -Payload @{
    eventType = "danmu"
    userId = "tester-orc"
    userName = "Tester Orc"
    text = "orc helldog"
}

Start-Sleep -Milliseconds 300

Send-Danmu -Path "command" -Payload @{
    eventType = "command"
    userId = "tester-skill"
    userName = "Tester Skill"
    team = "human"
    commandType = "skill"
    key = "air_strike"
    value = 100
}

if ($Gift) {
    Start-Sleep -Milliseconds 300

    Send-Danmu -Path "gift" -Payload @{
        eventType = "gift"
        userId = "tester-gift"
        userName = "Tester Gift"
        giftName = "orc rage gift"
        giftValue = 120
    }
}

if ($Full) {
    Write-Host "[OK] Full danmu mapping test sequence sent."
}
