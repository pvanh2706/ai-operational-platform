@echo off
rem ================================================================
rem  App THU HAI cho trang gia lap (O6-1): database kp_gialap,
rem  khach hang gia-lap, cong 5120. App that (kp_dev, 5119) khong bi dung toi.
rem
rem    run-gia-lap-app.bat
rem
rem  Can dung truoc MOT lan: scripts\gia-lap-db-setup.sql (doc dau file do).
rem  Can build truoc:        dotnet build src\KnowledgePlatform.Api
rem
rem  Khoa model: doc tu appsettings.Local.json o goc repo (O2-1).
rem  Dinh tuyen bang model con can "DinhTuyen": { "GoiModel": true } trong
rem  CHINH file do - mot cho duy nhat, de app nay va cac lenh xuat/nap ma
rem  trang gia lap goi thay cung mot cau hinh. Sua xong phai CHAY LAI file nay.
rem ================================================================
chcp 65001 >nul
setlocal
set "HERE=%~dp0"
set "APP=%HERE%..\..\src\KnowledgePlatform.Api"

set "ASPNETCORE_ENVIRONMENT=Development"
set "ASPNETCORE_URLS=http://localhost:5120"
set "ConnectionStrings__Default=Host=localhost;Port=5432;Database=kp_gialap;Username=kp_app;Password=123456"
set "Tenancy__Mode=DedicatedSingleTenant"
set "Tenancy__TenantExternalKey=gia-lap"

if not exist "%APP%\bin\Debug\net10.0\KnowledgePlatform.Api.exe" (
  echo Chua co ban build. Chay truoc: dotnet build src\KnowledgePlatform.Api
  pause
  exit /b 1
)

cd /d "%APP%"
"bin\Debug\net10.0\KnowledgePlatform.Api.exe" --kiem-cau-hinh
echo.
echo App gia lap: http://localhost:5120   (Ctrl+C de tat)
"bin\Debug\net10.0\KnowledgePlatform.Api.exe"

echo.
pause
