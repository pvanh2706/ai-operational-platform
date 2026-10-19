@echo off
rem ================================================================
rem  Build console Vue (docs/18 nhom P) ra scripts\giao-dien\console\dist.
rem  run-ui.bat phuc vu ban build o http://127.0.0.1:8791/console
rem
rem  Can Node 20.19+ hoac 22.12+ (Vite 8). Chi can luc BUILD, khong can luc chay.
rem  Dung "npm ci": cai DUNG phien ban trong package-lock.json, khong tu nang.
rem ================================================================
chcp 65001 >nul
setlocal
cd /d "%~dp0console"

where npm >nul 2>nul
if errorlevel 1 (
  echo Khong thay npm. Cai Node 22 LTS roi chay lai.
  pause
  exit /b 1
)

call npm ci --no-fund --no-audit
if errorlevel 1 (
  echo npm ci that bai.
  pause
  exit /b 1
)

call npm run build
if errorlevel 1 (
  echo Build that bai - xem loi o tren.
  pause
  exit /b 1
)

echo.
echo Xong. Chay run-ui.bat roi mo http://127.0.0.1:8791/console
pause
