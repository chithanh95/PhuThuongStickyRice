#requires -Version 5.1
<#
.SYNOPSIS
    Publish PhuThuongStickyRice (WebAPI + Blazor WASM) for IIS deployment.

.DESCRIPTION
    1. dotnet publish WebAPI            -> <OutputDir>\api
    2. dotnet publish Blazor WebAssembly -> <OutputDir>\web
    3. Copy the SPA fallback web.config into the web output
       (needs the IIS URL Rewrite Module, otherwise /shop, /orders, ... return 404 on refresh).
    4. Patch the published Blazor wwwroot\appsettings.json so the frontend
       calls the API at -ApiUrl.
    5. Unless -SkipMigrator, publish + run the Migrator once so EF migrations
       and seed data (5 xoi products + admin account) are applied.
       The Migrator keeps running as a web host after migrating, so the script
       stops it after 60 seconds. Check its console output for DbUp errors.

.PARAMETER ApiUrl
    Public URL of the API site on IIS, e.g. https://localhost:5001.
    Written into the published Blazor appsettings.json.

.PARAMETER OutputDir
    Publish output folder. Default: <repo>\deploy\iis\publish

.PARAMETER Configuration
    Build configuration. Default: Release

.PARAMETER ConnectionString
    SQL Server connection string used ONLY for the Migrator run
    (passed as the ConnectionStrings__PhuThuongStickyRice env var, which
    overrides the Migrator's own appsettings.json). If empty, the Migrator
    uses the connection string from its own appsettings.json.

.PARAMETER SkipMigrator
    Skip the publish + run Migrator step.

.EXAMPLE
    .\publish.ps1 -ApiUrl http://localhost:5001 -ConnectionString "Server=127.0.0.1;Database=PhuThuongStickyRice;User Id=sa;Password=YourPassword;MultipleActiveResultSets=true;Encrypt=False"
#>
[CmdletBinding()]
param(
    [string]$ApiUrl = "http://localhost:5001",
    [string]$OutputDir = "",
    [string]$Configuration = "Release",
    [string]$ConnectionString = "",
    [switch]$SkipMigrator
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet CLI not found. Install the .NET 10 SDK first."
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "deploy\iis\publish"
}

$apiOut = Join-Path $OutputDir "api"
$webOut = Join-Path $OutputDir "web"

Write-Host "Publishing WebAPI -> $apiOut"
dotnet publish (Join-Path $repoRoot "src\Backend\PhuThuongStickyRice.WebAPI\PhuThuongStickyRice.WebAPI.csproj") `
    -c $Configuration -o $apiOut --nologo

Write-Host "Publishing Blazor WebAssembly -> $webOut"
dotnet publish (Join-Path $repoRoot "src\Frontend\PhuThuongStickyRice.BlazorWebAssembly\PhuThuongStickyRice.BlazorWebAssembly.csproj") `
    -c $Configuration -o $webOut --nologo

Write-Host "Copying SPA fallback web.config (needs IIS URL Rewrite Module)"
Copy-Item (Join-Path $PSScriptRoot "web-spa.web.config") (Join-Path $webOut "web.config") -Force

$wasmSettings = Join-Path $webOut "wwwroot\appsettings.json"
if (Test-Path $wasmSettings) {
    Write-Host "Patching Blazor API endpoint -> $ApiUrl"
    $json = Get-Content $wasmSettings -Raw | ConvertFrom-Json
    $json.ResourceServer.Endpoint = $ApiUrl
    $json.ResourceServer.PublicEndpoint = $ApiUrl
    $json | ConvertTo-Json -Depth 5 | Set-Content $wasmSettings -Encoding UTF8
}

if (-not $SkipMigrator) {
    $migOut = Join-Path $OutputDir "migrator"
    Write-Host "Publishing Migrator -> $migOut"
    dotnet publish (Join-Path $repoRoot "src\Backend\PhuThuongStickyRice.Migrator\PhuThuongStickyRice.Migrator.csproj") `
        -c $Configuration -o $migOut --nologo

    $migDll = Join-Path $migOut "PhuThuongStickyRice.Migrator.dll"
    Write-Host "Running Migrator (EF migrations + seed). It stays up as a web host, so it will be stopped after 60s."

    $oldEnv = $env:ASPNETCORE_ENVIRONMENT
    $oldCs = $env:ConnectionStrings__PhuThuongStickyRice
    try {
        $env:ASPNETCORE_ENVIRONMENT = "Production"
        if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
            Write-Warning "No -ConnectionString: Migrator will use its own appsettings.json (currently points at a machine-specific SQL Server). Pass -ConnectionString to be safe."
        }
        else {
            $env:ConnectionStrings__PhuThuongStickyRice = $ConnectionString
        }
        $proc = Start-Process dotnet -ArgumentList "`"$migDll`"" -PassThru -NoNewWindow
        Start-Sleep -Seconds 60
        if (-not $proc.HasExited) {
            Stop-Process -Id $proc.Id -Force
            Write-Host "Migrator stopped. Check the output above for DbUp/EF errors."
        }
        elseif ($proc.ExitCode -ne 0) {
            throw "Migrator exited with code $($proc.ExitCode). See its output above."
        }
    }
    finally {
        $env:ASPNETCORE_ENVIRONMENT = $oldEnv
        $env:ConnectionStrings__PhuThuongStickyRice = $oldCs
    }
}

Write-Host ""
Write-Host "Done."
Write-Host "  API : $apiOut"
Write-Host "  Web : $webOut"
Write-Host "Next: edit $apiOut\appsettings.Production.json (connection string + Jwt key + CORS),"
Write-Host "then run .\setup-iis.ps1 -ApiPath `"$apiOut`" -WebPath `"$webOut`" (as Administrator)."
