param(
    [switch]$SkipAzurite,
    [switch]$SkipSql
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$frontendPath = Join-Path $repoRoot "Frontend/projectk-frontend"

$checks = New-Object System.Collections.Generic.List[object]

function Add-Check {
    param(
        [string]$Name,
        [bool]$Ok,
        [string]$Detail
    )

    $script:checks.Add([pscustomobject]@{
        Name = $Name
        Ok = $Ok
        Detail = $Detail
    })
}

function Test-Command {
    param([string]$Name)
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Get-CommandVersion {
    param(
        [string]$Command,
        [string[]]$Arguments = @("--version")
    )

    try {
        $output = & $Command @Arguments 2>$null | Select-Object -First 1
        return "$output".Trim()
    }
    catch {
        return ""
    }
}

function Test-Port {
    param([int]$Port)

    try {
        $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
        return $null -eq $connection
    }
    catch {
        return $true
    }
}

$dotnetOk = Test-Command "dotnet"
Add-Check ".NET SDK" $dotnetOk ($(if ($dotnetOk) { Get-CommandVersion "dotnet" @("--version") } else { "Install .NET SDK 10.x" }))

$nodeOk = Test-Command "node"
Add-Check "Node.js" $nodeOk ($(if ($nodeOk) { Get-CommandVersion "node" @("--version") } else { "Install Node.js 22.x" }))

$npmCommand = if (Test-Command "npm.cmd") { "npm.cmd" } elseif (Test-Command "npm") { "npm" } else { $null }
Add-Check "npm" ($null -ne $npmCommand) ($(if ($npmCommand) { Get-CommandVersion $npmCommand @("--version") } else { "Install npm with Node.js" }))

$nodeModulesPath = Join-Path $frontendPath "node_modules"
Add-Check "Frontend dependencies" (Test-Path $nodeModulesPath) ($(if (Test-Path $nodeModulesPath) { "node_modules found" } else { "Run npm install in Frontend/projectk-frontend" }))

if (-not $SkipAzurite) {
    $azuriteOk = Test-Command "azurite"
    Add-Check "Azurite" $azuriteOk ($(if ($azuriteOk) { Get-CommandVersion "azurite" @("--version") } else { "Install with: npm install -g azurite" }))
}

if (-not $SkipSql) {
    $sqlTools = (Test-Command "sqlcmd") -or (Test-Command "sqlcmd.exe")
    Add-Check "SQL tooling" $sqlTools ($(if ($sqlTools) { "sqlcmd found" } else { "Optional: install sqlcmd, or ensure appsettings connection string is valid" }))
}

foreach ($port in @(4200, 5205, 10000, 10001, 10002)) {
    $available = Test-Port $port
    Add-Check "Port $port" $available ($(if ($available) { "available" } else { "already in use" }))
}

$failed = $checks | Where-Object { -not $_.Ok }

foreach ($check in $checks) {
    $status = if ($check.Ok) { "OK" } else { "FAIL" }
    $color = if ($check.Ok) { "Green" } else { "Red" }
    Write-Host ("[{0}] {1}: {2}" -f $status, $check.Name, $check.Detail) -ForegroundColor $color
}

if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "Some checks failed. Fix them before running scripts/start-local.ps1." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "All local startup checks passed." -ForegroundColor Green
