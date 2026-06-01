param(
    [string]$WebSocketUrl = "ws://127.0.0.1:8766/danmu",
    [string]$HttpDanmuUrl = "http://127.0.0.1:8765/danmu",
    [string]$InputFile = "",
    [switch]$MirrorToHttp
)

$ErrorActionPreference = "Stop"

function Send-DanmuHttp {
    param([string]$JsonBody)
    if (-not $MirrorToHttp) { return }
    Invoke-RestMethod -Uri $HttpDanmuUrl -Method Post -ContentType "application/json; charset=utf-8" -Body $JsonBody | Out-Null
}

function Send-DanmuWebSocket {
    param([System.Net.WebSockets.ClientWebSocket]$Socket, [string]$JsonBody)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($JsonBody)
    $segment = [ArraySegment[byte]]::new($bytes)
    $null = $Socket.SendAsync($segment, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, [Threading.CancellationToken]::None).GetAwaiter().GetResult()
}

$socket = [System.Net.WebSockets.ClientWebSocket]::new()
$uri = [Uri]$WebSocketUrl
Write-Host "[WS] Connecting to $WebSocketUrl"
$socket.ConnectAsync($uri, [Threading.CancellationToken]::None).GetAwaiter().GetResult()
Write-Host "[WS] Connected. Send JSON lines (Ctrl+C to exit)."

if (-not [string]::IsNullOrWhiteSpace($InputFile)) {
    Get-Content -LiteralPath $InputFile | ForEach-Object {
        $line = $_.Trim()
        if ([string]::IsNullOrWhiteSpace($line)) { return }
        Send-DanmuWebSocket $socket $line
        Send-DanmuHttp $line
        Write-Host "[OK] $line"
    }
    $socket.Dispose()
    return
}

while ($true) {
    $line = Read-Host "danmu-json"
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    Send-DanmuWebSocket $socket $line
    Send-DanmuHttp $line
    Write-Host "[OK] queued"
}
