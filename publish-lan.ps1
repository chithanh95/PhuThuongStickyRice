# One-click publish & run cho mạng gia đình
# Chạy 1 phát: publish WebAPI + Web, mở port cho cả nhà dùng
# Usage: .\publish-lan.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishDir = Join-Path $root "publish-lan"

Write-Host "=== Publish Xoi Phu Thuong cho mang LAN ===" -ForegroundColor Green

# 1. Publish WebAPI
Write-Host "[1/4] Publishing WebAPI..." -ForegroundColor Yellow
dotnet publish "$root\src\Backend\PhuThuongStickyRice.WebAPI\PhuThuongStickyRice.WebAPI.csproj" `
    -c Release -o "$publishDir\api" --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Publish WebAPI that bai" }

# 2. Publish Web
Write-Host "[2/4] Publishing Web..." -ForegroundColor Yellow
dotnet publish "$root\src\Frontend\PhuThuongStickyRice.Web\PhuThuongStickyRice.Web.csproj" `
    -c Release -o "$publishDir\web" --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Publish Web that bai" }

# 3. Lay IP LAN
$ip = (Get-NetIPAddress -AddressFamily IPv4 |
    Where-Object { $_.IPAddress -like "192.168.*" -or $_.IPAddress -like "10.*" } |
    Select-Object -First 1).IPAddress
if (-not $ip) { $ip = "localhost" }

# 4. Ghi appsettings cho Web tro ve API tren LAN
$webAppSettings = Join-Path "$publishDir\web\wwwroot" "appsettings.json"
if (Test-Path $webAppSettings) {
    $json = Get-Content $webAppSettings -Raw | ConvertFrom-Json
    # Cap nhat endpoint API - giu nguyen cac field khac
    $json | Add-Member -NotePropertyName "_lanNote" -NotePropertyValue "API: http://$($ip):5000" -Force
    $json | ConvertTo-Json -Depth 10 | Set-Content $webAppSettings -Encoding UTF8
}

Write-Host ""
Write-Host "=== XONG! Mo 2 cua so terminal chay lenh sau: ===" -ForegroundColor Green
Write-Host ""
Write-Host "Cua so 1 (API):" -ForegroundColor Cyan
Write-Host "  cd $publishDir\api; dotnet PhuThuongStickyRice.WebAPI.dll --urls http://0.0.0.0:5000"
Write-Host ""
Write-Host "Cua so 2 (Web):" -ForegroundColor Cyan
Write-Host "  cd $publishDir\web; dotnet PhuThuongStickyRice.Web.dll --urls http://0.0.0.0:5001"
Write-Host ""
Write-Host "Dien thoai trong nha mo: " -NoNewline -ForegroundColor Green
Write-Host "http://${ip}:5001" -ForegroundColor Yellow
Write-Host ""
Write-Host "Luu y: Mo firewall cho port 5000, 5001 neu dien thoai khong ket noi duoc." -ForegroundColor DarkYellow
