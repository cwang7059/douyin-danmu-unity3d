param(
    [string]$HostUrl = "http://127.0.0.1:8765",
    [switch]$Gift
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

Send-Danmu -Path "danmu" -Payload @{
    eventType = "danmu"
    userId = "tester-human"
    userName = "Tester Human"
    text = "human soldier"
}

Start-Sleep -Milliseconds 300

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

