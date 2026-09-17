@echo off
rem ================================================================
rem  Dem so issue cua tung CUA SO THOI GIAN - KHONG keo case nao ve.
rem
rem  Cach dung:
rem    run-dem.bat              dem 13w / 26w / 52w (3 / 6 / 12 thang)
rem    run-dem.bat 4w 13w       dem cua so tu chon
rem
rem  Script chi gui maxResults=0 va doc truong total, nen khong sinh
rem  corpus, khong dung toi dry-run-*.json.
rem ================================================================
chcp 65001 >nul
setlocal
set "HERE=%~dp0"
set "PYTHONIOENCODING=utf-8"

if not exist "%HERE%jira-config.bat" (
    echo Chua co jira-config.bat - chay run-export.bat mot lan de tao.
    pause
    exit /b 1
)

call "%HERE%jira-config.bat"

where python >nul 2>nul
if errorlevel 1 (set "PY=py") else (set "PY=python")

%PY% "%HERE%dem_cua_so.py" %*

echo.
pause
