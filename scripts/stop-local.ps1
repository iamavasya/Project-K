$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$runRoot = Join-Path $repoRoot ".tmp/local-run"

if (-not (Test-Path $runRoot)) {
    Write-Host "No local run state found."
    exit 0
}

Get-ChildItem -LiteralPath $runRoot -Filter "*.pid" -ErrorAction SilentlyContinue | ForEach-Object {
    $name = $_.BaseName
    $pidValue = (Get-Content -LiteralPath $_.FullName -ErrorAction SilentlyContinue | Select-Object -First 1)

    if (-not $pidValue) {
        return
    }

    $process = Get-Process -Id ([int]$pidValue) -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "Stopping $name (PID $pidValue)"
        Stop-Process -Id ([int]$pidValue) -Force -ErrorAction SilentlyContinue
    }
    else {
        Write-Host "$name is not running."
    }

    Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue
}

Write-Host "Local Project-K stack stopped."
