@echo off
REM ============================================================
REM  XOI PHU THUONG - ONE CLICK CHAY WEB CHO MANG LAN
REM  Double-click file nay: tu publish (neu can) + chay server
REM  Dien thoai/may tinh khac cung WiFi mo dia chi hien ra
REM ============================================================
setlocal EnableDelayedExpansion

set ROOT=%~dp0
set PUB=%ROOT%publish-lan

echo ============================================
echo  XOI PHU THUONG - One-click LAN server
echo ============================================
echo.

REM --- Lay IP LAN ---
set IP=
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr "IPv4"') do (
    set IP=%%a
    goto :found
)
:found
set IP=%IP: =%
REM Chi lay IP 192.168.x hoac 10.x (bo qua 127.0.0.1)
echo %IP% | findstr /r "^192\.168\." >nul
if %errorlevel%==0 goto :ipok
echo %IP% | findstr /r "^10\." >nul
if %errorlevel%==0 goto :ipok
REM Tim lai IP LAN neu IP dau tien khong phai LAN
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr "IPv4"') do (
    set _ip=%%a
    set _ip=!_ip: =!
    echo !_ip! | findstr /r "^192\.168\." >nul
    if !errorlevel!==0 ( set IP=!_ip! & goto :ipok )
    echo !_ip! | findstr /r "^10\." >nul
    if !errorlevel!==0 ( set IP=!_ip! & goto :ipok )
)
:ipok

REM --- Publish neu chua co hoac code moi hon ban publish ---
set NEED_PUBLISH=0
if not exist "%PUB%\api\PhuThuongStickyRice.WebAPI.dll" set NEED_PUBLISH=1
if not exist "%PUB%\web\PhuThuongStickyRice.Web.dll" set NEED_PUBLISH=1

if %NEED_PUBLISH%==1 (
    echo  [1/2] Chua co ban publish - dang publish (mat 1-2 phut)...
    echo.
    powershell -ExecutionPolicy Bypass -File "%ROOT%publish-lan.ps1"
    if %errorlevel% neq 0 (
        echo.
        echo  Publish THAT BAI. Nhan phim bat ky de thoat.
        pause >nul
        exit /b 1
    )
) else (
    echo  [1/2] Da co ban publish - bo qua (xoa thu muc publish-lan de publish lai).
)

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
pause
