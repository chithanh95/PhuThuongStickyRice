#requires -Version 5.1
#requires -RunAsAdministrator
<#
.SYNOPSIS
    Create the IIS app pool + the API and Web sites for PhuThuongStickyRice.

.DESCRIPTION
    - Creates one app pool (No Managed Code) shared by both sites.
    - Creates the API site and the Web (Blazor WASM) site on the given ports.
    - Sets ASPNETCORE_ENVIRONMENT=Production on the API site so it reads
      appsettings.Production.json.
    - Existing sites / app pool with the same names are removed first.

    Requires: IIS + ASP.NET Core Hosting Bundle (.NET 10) + URL Rewrite Module.
    Run from an elevated PowerShell prompt.

.EXAMPLE
    .\setup-iis.ps1 -ApiPath "C:\deploy\PhuThuongStickyRice\api" -WebPath "C:\deploy\PhuThuongStickyRice\web"
#>
[CmdletBinding()]
param(
    [string]$ApiSiteName = "PhuThuong.Api",
    [string]$WebSiteName = "PhuThuong.Web",
    [string]$AppPoolName = "PhuThuong",
    [string]$ApiPath = "",
    [string]$WebPath = "",
    [int]$ApiPort = 5001,
    [int]$WebPort = 5000
)

$ErrorActionPreference = "Stop"
Import-Module WebAdministration

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if ([string]::IsNullOrWhiteSpace($ApiPath)) {
    $ApiPath = Join-Path $repoRoot "deploy\iis\publish\api"
}
if ([string]::IsNullOrWhiteSpace($WebPath)) {
    $WebPath = Join-Path $repoRoot "deploy\iis\publish\web"
}

if (-not (Test-Path $ApiPath)) { throw "API path not found: $ApiPath (run publish.ps1 first)" }
if (-not (Test-Path $WebPath)) { throw "Web path not found: $WebPath (run publish.ps1 first)" }

# --- App pool (No Managed Code) ---
if (Test-Path "IIS:\AppPools\$AppPoolName") {
    Write-Host "Removing existing app pool $AppPoolName"
    Remove-WebAppPool -Name $AppPoolName
}
Write-Host "Creating app pool $AppPoolName (No Managed Code)"
New-WebAppPool -Name $AppPoolName | Out-Null
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""

# --- Sites ---
foreach ($site in @(
    @{ Name = $ApiSiteName; Path = $ApiPath; Port = $ApiPort },
    @{ Name = $WebSiteName; Path = $WebPath; Port = $WebPort }
)) {
    if (Get-Website -Name $site.Name -ErrorAction SilentlyContinue) {
        Write-Host "Removing existing site $($site.Name)"
        Remove-Website -Name $site.Name
    }
    Write-Host "Creating site $($site.Name) on port $($site.Port) -> $($site.Path)"
    New-Website -Name $site.Name -Port $site.Port -PhysicalPath $site.Path -ApplicationPool $AppPoolName | Out-Null
}

# --- API site reads appsettings.Production.json ---
Write-Host "Setting ASPNETCORE_ENVIRONMENT=Production on $ApiSiteName"
Add-WebConfigurationProperty -PSPath "MACHINE/WEBROOT/APPHOST/$ApiSiteName" `
    -Filter "system.webServer/aspNetCore/environmentVariables" `
    -Name "." -Value @{ name = "ASPNETCORE_ENVIRONMENT"; value = "Production" }

Write-Host ""
Write-Host "Done."
Write-Host "  API : http://localhost:$ApiPort  (edit its appsettings.Production.json first!)"
Write-Host "  Web : http://localhost:$WebPort"
Write-Host "Check: IIS Manager -> Sites -> $($ApiSiteName)/$($WebSiteName) should be Started."
