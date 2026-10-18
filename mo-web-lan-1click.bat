@echo off
REM ============================================================
REM  XOI PHU THUONG - ONE CLICK CHAY WEB CHO MANG LAN
REM  Double-click file nay: tu publish + chay server
REM  Dien thoai/may tinh khac cung WiFi mo dia chi hien ra
REM ============================================================
setlocal

set ROOT=%~dp0
set PUB=%ROOT%publish-lan

echo ============================================
echo  XOI PHU THUONG - One-click LAN server
echo ============================================
echo.

REM --- Lay IP LAN ---
set IP=
for /f "usebackq delims=" %%a in (`powershell -NoProfile -Command "(Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -like '192.168.*' -or $_.IPAddress -like '10.*' } | Select-Object -First 1).IPAddress"`) do set IP=%%a
if "%IP%"=="" set IP=localhost
echo  IP LAN: %IP%
echo.

REM --- Publish WebAPI ---
echo  [1/4] Publishing WebAPI...
dotnet publish "%ROOT%src\Backend\PhuThuongStickyRice.WebAPI\PhuThuongStickyRice.WebAPI.csproj" -c Release -o "%PUB%\api" --nologo -v q
if errorlevel 1 goto :fail

REM --- Publish Web ---
echo  [2/4] Publishing Web...
dotnet publish "%ROOT%src\Frontend\PhuThuongStickyRice.Web\PhuThuongStickyRice.Web.csproj" -c Release -o "%PUB%\web" --nologo -v q
if errorlevel 1 goto :fail

REM --- Cau hinh Web tro ve API tren LAN ---
echo  [3/4] Cau hinh Web -^> API...
powershell -NoProfile -Command ^
  "$p='%PUB%\web\wwwroot\appsettings.json';" ^
  "$j=Get-Content $p -Raw | ConvertFrom-Json;" ^
  "$u='http://%IP%:5000';" ^
  "if ($j.ResourceServer) { $j.ResourceServer.Endpoint=$u; $j.ResourceServer.PublicEndpoint=$u };" ^
  "$j | ConvertTo-Json -Depth 10 | Set-Content $p -Encoding UTF8"

REM --- Cau hinh API: connection string + CORS ---
echo  [4/4] Cau hinh API cho LAN...
powershell -NoProfile -Command ^
  "$dev=Get-Content '%ROOT%src\Backend\PhuThuongStickyRice.WebAPI\appsettings.json' -Raw | ConvertFrom-Json;" ^
  "$p='%PUB%\api\appsettings.Production.json';" ^
  "$j=Get-Content $p -Raw | ConvertFrom-Json;" ^
  "if ($dev.ConnectionStrings) { $j | Add-Member -NotePropertyName 'ConnectionStrings' -NotePropertyValue $dev.ConnectionStrings -Force };" ^
  "if ($j.CORS) { $j.CORS.AllowAnyOrigin=$true };" ^
  "$j | ConvertTo-Json -Depth 10 | Set-Content $p -Encoding UTF8"

echo.
echo  Publish xong. Dang khoi dong server...
echo.
echo ============================================
echo  Dien thoai / may khac mo:
echo.
echo    http://%IP%:5001
echo.
echo  (Phai cung WiFi voi may nay)
echo ============================================
echo.

start "Xoi API :5000" cmd /k "cd /d %PUB%\api && dotnet PhuThuongStickyRice.WebAPI.dll --urls http://0.0.0.0:5000"
timeout /t 5 /nobreak >nul
start "Xoi Web :5001" cmd /k "cd /d %PUB%\web && dotnet PhuThuongStickyRice.Web.dll --urls http://0.0.0.0:5001"

echo  Da mo 2 cua so server. De nguyen de web chay.
echo  Dong 2 cua so de dung web.
echo.
echo  Luu y: lan dau Windows hoi Firewall thi bam Allow.
echo.
pause
exit /b 0

:fail
echo.
echo  THAT BAI. Xem loi o tren.
pause
exit /b 1
