@echo off
rem ======================================================================
rem  TAT moi thanh phan ma chay-thu.bat mo:
rem    - moi tien trinh KnowledgePlatform.Api.exe (app gia lap :5120, app dev :5119)
rem    - server giao dien ui_server.py (:8791)
rem  PostgreSQL KHONG bi dong.
rem ======================================================================
chcp 65001 >nul
echo Dong cac cua so "KP ..." ma chay-thu.bat mo (kem tien trinh ben trong)...
taskkill /FI "WINDOWTITLE eq KP *" /T /F >nul 2>nul && echo   da dong || echo   khong co cua so nao
echo Tat app .NET...
taskkill /IM KnowledgePlatform.Api.exe /F >nul 2>nul && echo   da tat || echo   khong co app nao dang chay
echo Tat server giao dien...
powershell -NoProfile -Command "$p = Get-CimInstance Win32_Process -Filter \"Name='python.exe' OR Name='py.exe'\" | Where-Object { $_.CommandLine -like '*ui_server.py*' }; if ($p) { $p | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }; '  da tat' } else { '  khong co server giao dien nao dang chay' }"
echo.
echo Xong.
pause
