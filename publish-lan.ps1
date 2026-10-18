# One-click publish & run cho mạng gia đình
# Chạy 1 phát: publish WebAPI + Web, mở port cho cả nhà dùng
# Usage: .\publish-lan.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishDir = Join-Path $root "publish-lan"

Write-Host "=== Publish Xoi Phu Thuong cho mang LAN ===" -ForegroundColor Green

# 1. Publish WebAPI
Write-Host "[1/5] Publishing WebAPI..." -ForegroundColor Yellow
dotnet publish "$root\src\Backend\PhuThuongStickyRice.WebAPI\PhuThuongStickyRice.WebAPI.csproj" `
    -c Release -o "$publishDir\api" --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Publish WebAPI that bai" }

# 2. Publish Web
Write-Host "[2/5] Publishing Web..." -ForegroundColor Yellow
dotnet publish "$root\src\Frontend\PhuThuongStickyRice.Web\PhuThuongStickyRice.Web.csproj" `
    -c Release -o "$publishDir\web" --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Publish Web that bai" }

# 3. Lay IP LAN
$ip = (Get-NetIPAddress -AddressFamily IPv4 |
    Where-Object { $_.IPAddress -like "192.168.*" -or $_.IPAddress -like "10.*" } |
    Select-Object -First 1).IPAddress
if (-not $ip) { $ip = "localhost" }

# 4. Cau hinh Web tro ve API tren LAN: ghi THAT vao ResourceServer.Endpoint
#    (ban cu chi them _lanNote nen Web van goi https://localhost:44312 -> chet tren dien thoai)
Write-Host "[4/5] Cau hinh Web tro ve API tren LAN..." -ForegroundColor Yellow
$webAppSettings = Join-Path "$publishDir\web\wwwroot" "appsettings.json"
if (Test-Path $webAppSettings) {
    $json = Get-Content $webAppSettings -Raw | ConvertFrom-Json
    $apiUrl = "http://${ip}:5000"
    if ($json.ResourceServer) {
        $json.ResourceServer.Endpoint = $apiUrl
        $json.ResourceServer.PublicEndpoint = $apiUrl
    }
    $json | ConvertTo-Json -Depth 10 | Set-Content $webAppSettings -Encoding UTF8
    Write-Host "    Web -> API: $apiUrl" -ForegroundColor DarkGray
}

# 5. Cau hinh API cho mang LAN
#    API chay o che do Production mac dinh; file Production goc de CORS chi cho localhost
#    va connection string dung sa@127.0.0.1 (sa dang disable) -> dien thoai bi chan, API khong noi DB
Write-Host "[5/5] Cau hinh API cho mang LAN..." -ForegroundColor Yellow
$devAppSettings = "$root\src\Backend\PhuThuongStickyRice.WebAPI\appsettings.json"
$apiProdSettings = Join-Path "$publishDir\api" "appsettings.Production.json"
if ((Test-Path $devAppSettings) -and (Test-Path $apiProdSettings)) {
    $devJson = Get-Content $devAppSettings -Raw | ConvertFrom-Json
    $apiJson = Get-Content $apiProdSettings -Raw | ConvertFrom-Json
    # Lay connection string dang chay duoc tren may nay (tu ban dev)
    if ($devJson.ConnectionStrings) {
        $apiJson | Add-Member -NotePropertyName "ConnectionStrings" -NotePropertyValue $devJson.ConnectionStrings -Force
    }
    # Mo CORS cho ca mang LAN (JWT di theo header Authorization nen AllowAnyOrigin van giu dang nhap duoc)
    if ($apiJson.CORS) { $apiJson.CORS.AllowAnyOrigin = $true }
    $apiJson | ConvertTo-Json -Depth 10 | Set-Content $apiProdSettings -Encoding UTF8
    Write-Host "    API: lay connection string tu ban dev + mo CORS cho LAN" -ForegroundColor DarkGray
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
