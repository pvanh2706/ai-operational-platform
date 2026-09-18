@echo off
rem ================================================================
rem  Giao dien web cho vong: quet Jira -> AI phan tich -> nap lai.
rem
rem    run-ui.bat        mo http://127.0.0.1:8791
rem
rem  KHONG can jira-config.bat: thong tin ket noi go thang tren trang.
rem  Token chi nam trong RAM cua tien trinh nay, tat la het.
rem ================================================================
chcp 65001 >nul
setlocal
set "HERE=%~dp0"
set "PYTHONIOENCODING=utf-8"

where python >nul 2>nul
if errorlevel 1 (set "PY=py") else (set "PY=python")

%PY% "%HERE%ui_server.py"

echo.
pause
