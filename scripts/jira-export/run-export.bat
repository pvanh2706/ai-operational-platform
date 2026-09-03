@echo off
rem ================================================================
rem  Xuat issue + comment tu Jira vao Kenh 1 (buoc b cua Path A).
rem
rem  Cach dung:
rem    run-export.bat         chay THU (dry-run): chi doc Jira, ghi 2 file
rem                           JSON canh script de soi bang mat, KHONG gui gi
rem    run-export.bat send    gui that vao app (app phai dang chay:
rem                           dotnet run --project src/KnowledgePlatform.Api --launch-profile http)
rem
rem  Lan chay dau se tu tao jira-config.bat va mo Notepad cho ban dien.
rem  File .bat nay la tieng Viet KHONG DAU co chu dich - cmd.exe khong
rem  doc duoc UTF-8 co dau. Thong bao cua script Python thi van co dau.
rem ================================================================
chcp 65001 >nul
setlocal
set "HERE=%~dp0"

if not exist "%HERE%jira-config.bat" (
    copy "%HERE%jira-config.example.bat" "%HERE%jira-config.bat" >nul
    echo Lan dau chay: da tao file cau hinh jira-config.bat - file nay nam trong
    echo .gitignore nen PAT cua ban se khong bi commit.
    echo.
    echo Dien JIRA_BASE_URL, JIRA_PAT, JIRA_JQL trong Notepad vua mo, luu lai,
    echo roi chay lai file nay.
    start notepad "%HERE%jira-config.bat"
    pause
    exit /b 1
)

call "%HERE%jira-config.bat"

if "%JIRA_PAT%"=="dan-personal-access-token-vao-day" (
    echo Ban chua dien thong tin trong jira-config.bat - mo lai Notepad de dien.
    start notepad "%HERE%jira-config.bat"
    pause
    exit /b 1
)

where python >nul 2>nul
if errorlevel 1 (set "PY=py") else (set "PY=python")

if /i "%~1"=="send" (
    echo === GUI THAT vao %APP_BASE_URL% - app phai dang chay ===
    %PY% "%HERE%export_jira_to_channel1.py"
) else (
    echo === CHAY THU - dry-run: chi doc Jira va ghi JSON, chua gui gi ===
    echo === Muon gui that: run-export.bat send ===
    %PY% "%HERE%export_jira_to_channel1.py" --dry-run
)

echo.
pause
