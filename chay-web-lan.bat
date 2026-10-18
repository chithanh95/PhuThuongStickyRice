@echo off
REM One-click chay web cho ca nha - double-click file nay
REM Yeu cau: da chay publish-lan.ps1 1 lan truoc do

set PUB=%~dp0publish-lan
set IP=

REM Lay IP LAN dau tien (tim "IPv4" thay vi "IPv4 Address" de chay duoc ca Windows tieng Anh va tieng Viet)
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr "IPv4"') do (
    set IP=%%a
    goto :found
)
:found
set IP=%IP: =%

echo ============================================
echo  XOI PHU THUONG - Dang khoi dong...
echo ============================================
echo.
echo  Dien thoai mo: http://%IP%:5001
echo.

start "Xoi API :5000" cmd /k "cd /d %PUB%\api && dotnet PhuThuongStickyRice.WebAPI.dll --urls http://0.0.0.0:5000"
timeout /t 5 /nobreak >nul
start "Xoi Web :5001" cmd /k "cd /d %PUB%\web && dotnet PhuThuongStickyRice.Web.dll --urls http://0.0.0.0:5001"

echo.
echo  Da mo 2 cua so. De nguyen de web chay.
echo  Tat 2 cua so de dung web.
pause
