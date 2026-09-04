param(
    [int]$MaxHeartbeatAgeSeconds = 15,
    [string]$ExpectedUnityVersion = "2022.3.62f1"
)

$ErrorActionPreference = "Stop"

# ----------------------------------------
# Project paths
# ----------------------------------------

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$AgentDirectory = Join-Path $ProjectRoot ".agent"
$HeartbeatPath = Join-Path $AgentDirectory "heartbeat.json"

$ExpectedProjectName = Split-Path $ProjectRoot -Leaf

# ----------------------------------------
# heartbeat.json existence check
# ----------------------------------------

if (-not (Test-Path $HeartbeatPath)) {
    Write-Host "[ERROR] heartbeat.json was not found."
    Write-Host "Unity Editor may not be running."
    Write-Host "Expected path:"
    Write-Host $HeartbeatPath

    exit 10
}

# ----------------------------------------
# Read JSON
# ----------------------------------------

try {
    $RawHeartbeat = Get-Content $HeartbeatPath -Raw -Encoding UTF8

    if ([string]::IsNullOrWhiteSpace($RawHeartbeat)) {
        Write-Host "[ERROR] heartbeat.json is empty."
        Write-Host "AgentBridge may not be running."

        exit 11
    }

    $Heartbeat = $RawHeartbeat | ConvertFrom-Json
}
catch {
    Write-Host "[ERROR] heartbeat.json could not be parsed."
    Write-Host $_.Exception.Message

    exit 12
}

# ----------------------------------------
# Editor status
# ----------------------------------------

if ($Heartbeat.editorRunning -ne $true) {
    Write-Host "[ERROR] Unity Editor is not reported as running."

    exit 13
}

# ----------------------------------------
# Project check
# ----------------------------------------

if ($Heartbeat.project -ne $ExpectedProjectName) {
    Write-Host "[ERROR] A different Unity project appears to be running."
    Write-Host "Expected : $ExpectedProjectName"
    Write-Host "Actual   : $($Heartbeat.project)"

    exit 14
}

# ----------------------------------------
# Unity version check
# ----------------------------------------

if (
    -not [string]::IsNullOrWhiteSpace($ExpectedUnityVersion) -and
    $Heartbeat.unityVersion -ne $ExpectedUnityVersion
) {
    Write-Host "[ERROR] Unexpected Unity version."
    Write-Host "Expected : $ExpectedUnityVersion"
    Write-Host "Actual   : $($Heartbeat.unityVersion)"

    exit 15
}

# ----------------------------------------
# Heartbeat age check
# ----------------------------------------

try {
    $HeartbeatTime = [DateTimeOffset]::Parse($Heartbeat.timestamp)
}
catch {
    Write-Host "[ERROR] Invalid heartbeat timestamp."
    Write-Host "Value: $($Heartbeat.timestamp)"

    exit 16
}

$Now = [DateTimeOffset]::Now
$HeartbeatAge = ($Now - $HeartbeatTime).TotalSeconds

if ($HeartbeatAge -lt -30) {
    Write-Host "[ERROR] heartbeat timestamp is unexpectedly in the future."

    exit 17
}

if ($HeartbeatAge -gt $MaxHeartbeatAgeSeconds) {
    Write-Host "[ERROR] Unity heartbeat is stale."
    Write-Host ("Age: {0:N1} seconds" -f $HeartbeatAge)
    Write-Host "Unity Editor may have stopped responding."

    exit 18
}

# ----------------------------------------
# Success
# ----------------------------------------

Write-Host "[OK] Unity Editor is available."
Write-Host "Project       : $($Heartbeat.project)"
Write-Host "Unity version : $($Heartbeat.unityVersion)"
Write-Host "Compiling     : $($Heartbeat.isCompiling)"
Write-Host "Playing       : $($Heartbeat.isPlaying)"
Write-Host ("Heartbeat age : {0:N1} seconds" -f $HeartbeatAge)

exit 0