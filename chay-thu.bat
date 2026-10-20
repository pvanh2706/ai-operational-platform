@echo off
rem ======================================================================
rem  CHAY THU TOAN BO - nhan dup la chay.
rem
rem    1. kiem: dotnet, python, PostgreSQL (cong 5432)
rem    2. build app .NET   (bo qua neu app dang chay - file DLL bi khoa)
rem    3. build console Vue (chi khi chua co ban build va may co Node)
rem    4. mo 3 cua so:  app gia lap :5120 (kp_gialap)
rem                     app dev     :5119 (kp_dev)
rem                     giao dien   :8791
rem    5. cho app gia lap san sang, roi mo http://127.0.0.1:8791/console/gia-lap
rem
rem  Tat het: dung-thu.bat (hoac dong tung cua so).
rem  Thanh phan nao DANG chay roi thi khong mo lai - chay file nay nhieu lan vo hai.
rem
rem  Can dung truoc MOT lan tren moi may: database kp_dev (scripts\dev-db-setup.sql)
rem  va kp_gialap (scripts\gia-lap-db-setup.sql) - xem README.
rem ======================================================================
chcp 65001 >nul
setlocal
cd /d "%~dp0"
set "GOC=%~dp0"
set "APP=%GOC%src\KnowledgePlatform.Api"
set "EXE=%APP%\bin\Debug\net10.0\KnowledgePlatform.Api.exe"

echo.
echo [1/5] Kiem dieu kien...
where dotnet >nul 2>nul || (echo   THIEU dotnet - cai .NET 10 SDK. & goto :loi)
where python >nul 2>nul && (set "PY=python") || (where py >nul 2>nul && (set "PY=py") || (echo   THIEU python. & goto :loi))
powershell -NoProfile -Command "exit [int](-not (Test-NetConnection localhost -Port 5432 -InformationLevel Quiet -WarningAction SilentlyContinue))" >nul 2>nul
if errorlevel 1 (echo   PostgreSQL KHONG tra loi o cong 5432 - bat service postgresql-x64-18 roi chay lai. & goto :loi)
echo   OK - dotnet, %PY%, PostgreSQL

echo.
echo [2/5] Build app .NET...
tasklist /FI "IMAGENAME eq KnowledgePlatform.Api.exe" 2>nul | find /I "KnowledgePlatform.Api.exe" >nul
if not errorlevel 1 (
  echo   App dang chay - BO QUA build, dung ban build hien co. Muon build lai: chay dung-thu.bat truoc.
) else (
  dotnet build "%APP%" -v q -nologo || (echo   Build THAT BAI - xem loi o tren. & goto :loi)
)
if not exist "%EXE%" (echo   Khong thay %EXE% & goto :loi)

echo.
echo [3/5] Console Vue...
if exist "%GOC%scripts\giao-dien\console\dist\index.html" (
  echo   Da co ban build. Sua code Vue thi chay scripts\giao-dien\build-console.bat.
) else (
  where npm >nul 2>nul || (echo   Chua co ban build va KHONG co Node - console se bao chua build. Trang /gia-lap cu van dung duoc. & goto :buoc4)
  pushd "%GOC%scripts\giao-dien\console"
  call npm ci --no-fund --no-audit || (popd & echo   npm ci that bai. & goto :loi)
  call npm run build || (popd & echo   Build console that bai. & goto :loi)
  popd
)

:buoc4
echo.
echo [4/5] Mo cac thanh phan...
rem Moi thanh phan la MOT file .bat rieng, mo trong cua so rieng - khong long dau ngoac kep trong "cmd /c",
rem thu de vo khi duong dan co dau cach.
call :dang_nghe 5120 && (echo   app gia lap :5120 da chay san) || (
  start "KP app gia lap :5120" "%GOC%scripts\giao-dien\run-gia-lap-app.bat"
  echo   mo app gia lap :5120
)
call :dang_nghe 5119 && (echo   app dev :5119 da chay san) || (
  start "KP app dev :5119" "%GOC%scripts\giao-dien\run-dev-app.bat"
  echo   mo app dev :5119
)

echo.
echo [5/5] Cho app gia lap san sang (toi da 60 giay)...
powershell -NoProfile -Command "for($i=0;$i -lt 60;$i++){try{if((Invoke-WebRequest -UseBasicParsing http://localhost:5120/health/ready -TimeoutSec 2).StatusCode -eq 200){exit 0}}catch{}; Start-Sleep 1}; exit 1"
if errorlevel 1 (echo   App gia lap CHUA san sang - xem cua so "KP app gia lap :5120". Thuong la chua dung kp_gialap. & goto :loi)
echo   app gia lap san sang

call :dang_nghe 8791 && (
  echo   giao dien :8791 da chay san - mo trinh duyet
  if not "%KP_UI_KHONG_MO%"=="1" start "" "http://127.0.0.1:8791/console/gia-lap"
) || (
  rem Cua so con THUA HUONG bien moi truong nay - run-ui.bat khong can biet chay-thu.bat ton tai.
  set "KP_UI_MO_TRANG=/console/gia-lap"
  start "KP giao dien :8791" "%GOC%scripts\giao-dien\run-ui.bat"
)

echo.
echo ======================================================================
echo   Xong. Trinh duyet mo http://127.0.0.1:8791/console/gia-lap
echo     Buoc 0: bam "Thu khoa" truoc moi luot API.
echo     Console:  http://127.0.0.1:8791/console   (chon app :5119 / :5120 o goc phai)
echo   Tat het:  dung-thu.bat
echo ======================================================================
echo.
pause
exit /b 0

rem ---- tra 0 neu cong %1 dang co tien trinh nghe tren localhost ----
:dang_nghe
netstat -ano | findstr /R /C:"127.0.0.1:%1 .*LISTENING" /C:"\[::1\]:%1 .*LISTENING" /C:"0.0.0.0:%1 .*LISTENING" >nul
exit /b %errorlevel%

:loi
echo.
echo Dung lai vi loi o tren.
pause
exit /b 1
