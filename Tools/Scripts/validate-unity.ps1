param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Compile", "EditMode", "PlayMode")]
    [string]$Mode,

    [int]$TimeoutSeconds = 180,

    [int]$PollMilliseconds = 500
)

$ErrorActionPreference = "Stop"

# ----------------------------------------
# Project paths
# ----------------------------------------

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

$AgentDirectory = Join-Path $ProjectRoot ".agent"
$RequestPath = Join-Path $AgentDirectory "request.json"
$RequestTempPath = Join-Path $AgentDirectory "request.tmp.json"
$ResultPath = Join-Path $AgentDirectory "result.json"

$CheckUnityScript = Join-Path $PSScriptRoot "check-unity.ps1"

# ----------------------------------------
# Ensure .agent directory exists
# ----------------------------------------

if (-not (Test-Path $AgentDirectory)) {
    New-Item `
        -Path $AgentDirectory `
        -ItemType Directory `
        -Force | Out-Null
}

# ----------------------------------------
# Check Unity Editor
# ----------------------------------------

Write-Host "Checking Unity Editor..."

& $CheckUnityScript

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[ERROR] Unity Editor is not available."
    Write-Host "Unity will NOT be started automatically."

    exit 20
}

# ----------------------------------------
# Convert mode to AgentBridge command
# ----------------------------------------

switch ($Mode) {
    "Compile" {
        $Command = "compile_status"
    }

    "EditMode" {
        $Command = "run_editmode_tests"
    }

    "PlayMode" {
        $Command = "run_playmode_tests"
    }
}

# ----------------------------------------
# Generate unique request ID
# ----------------------------------------

$RequestId = [Guid]::NewGuid().ToString()

$Request = [ordered]@{
    id          = $RequestId
    command     = $Command
    requestedAt = [DateTimeOffset]::Now.ToString("o")
}

# ----------------------------------------
# Write request atomically
# ----------------------------------------

$RequestJson = $Request | ConvertTo-Json -Depth 10

$RequestJson |
    Set-Content `
        -Path $RequestTempPath `
        -Encoding UTF8

Move-Item `
    -Path $RequestTempPath `
    -Destination $RequestPath `
    -Force

Write-Host ""
Write-Host "Unity validation requested."
Write-Host "Request ID : $RequestId"
Write-Host "Mode       : $Mode"
Write-Host "Command    : $Command"
Write-Host ""

# ----------------------------------------
# Wait for AgentBridge result
# ----------------------------------------

$Deadline = [DateTimeOffset]::Now.AddSeconds($TimeoutSeconds)

while ([DateTimeOffset]::Now -lt $Deadline) {

    if (Test-Path $ResultPath) {

        try {
            $RawResult = Get-Content $ResultPath -Raw -Encoding UTF8

            if (-not [string]::IsNullOrWhiteSpace($RawResult)) {

                $Result = $RawResult | ConvertFrom-Json

                # Ignore old results from previous requests
                if ($Result.id -eq $RequestId) {

                    Write-Host "Unity returned a result:"
                    Write-Host ""

                    $Result |
                        ConvertTo-Json -Depth 20 |
                        Write-Host

                    Write-Host ""

                    if ($Result.status -eq "success") {
                        Write-Host "[OK] Unity validation succeeded."

                        exit 0
                    }
                    else {
                        Write-Host "[ERROR] Unity validation failed."

                        exit 30
                    }
                }
            }
        }
        catch {
            # AgentBridge may still be writing the file.
            # Ignore temporary parse failures and retry.
        }
    }

    Start-Sleep -Milliseconds $PollMilliseconds
}

# ----------------------------------------
# Timeout
# ----------------------------------------

Write-Host ""
Write-Host "[ERROR] Timed out waiting for Unity AgentBridge."
Write-Host "Request ID: $RequestId"
Write-Host "Timeout   : $TimeoutSeconds seconds"

exit 31