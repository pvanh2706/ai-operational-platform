@echo off
rem ================================================================
rem  MAU CAU HINH - dung dien vao file nay.
rem  run-export.bat se tu chep no thanh jira-config.bat (file do da
rem  nam trong .gitignore, nen PAT cua ban khong bao gio bi commit).
rem
rem  Ghi chu: file .bat phai la tieng Viet KHONG DAU - cmd.exe doc
rem  file theo codepage ANSI, chu co dau se lam hong ca file.
rem ================================================================

rem --- BAT BUOC: ba dong nay ---
set "JIRA_BASE_URL=https://jira.cua-cong-ty.vn"
set "JIRA_PAT=dan-personal-access-token-vao-day"
set "JIRA_JQL=project = OTA AND resolution IS NOT EMPTY ORDER BY created ASC"

rem --- Jira ban cu chua co PAT? Xoa dong JIRA_PAT o tren va dung hai dong nay ---
rem set "JIRA_USER=ten-dang-nhap"
rem set "JIRA_PASS=mat-khau"

rem --- Tuy chon, mac dinh da dung cho may dev ---
set "APP_BASE_URL=http://localhost:5119"
set "APP_SIGNAL_KEY="
set "MAX_ISSUES=0"
