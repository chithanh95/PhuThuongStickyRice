@echo off
REM ============================================================
REM  XOI PHU THUONG - ONE CLICK CHAY WEB CHO MANG LAN
REM  Double-click file nay: tu publish (neu can) + chay server
REM  Dien thoai/may tinh khac cung WiFi mo dia chi hien ra
REM ============================================================
setlocal

set ROOT=%~dp0
set PUB=%ROOT%publish-lan

echo ============================================
echo  XOI PHU THUONG - One-click LAN server
echo ============================================
echo.

REM --- Lay IP LAN bang PowerShell (on dinh hon findstr) ---
set IP=
for /f "usebackq delims=" %%a in (`powershell -NoProfile -Command "(Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -like '192.168.*' -or $_.IPAddress -like '10.*' } | Select-Object -First 1).IPAddress"`) do set IP=%%a
if "%IP%"=="" set IP=localhost

echo  IP LAN: %IP%
echo.

REM --- Publish neu chua co ---
if not exist "%PUB%\api\PhuThuongStickyRice.WebAPI.dll" goto :dopublish
if not exist "%PUB%\web\PhuThuongStickyRice.Web.dll" goto :dopublish
echo  [1/2] Da co ban publish - bo qua.
goto :runserver

:dopublish
echo  [1/2] Chua co ban publish - dang publish (mat 1-2 phut)...
echo.
powershell -ExecutionPolicy Bypass -File "%ROOT%publish-lan.ps1"
if errorlevel 1 (
    echo.
    echo  Publish THAT BAI. Xem loi o tren.
    echo  Nhan phim bat ky de thoat.
    pause >nul
    exit /b 1
)
echo.
echo  Publish xong.

:runserver
echo.
echo  [2/2] Dang khoi dong server...
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
