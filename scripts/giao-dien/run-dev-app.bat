@echo off
rem ================================================================
rem  App DEV: database kp_dev, khach dev-tenant, cong 5119 - cau hinh
rem  mac dinh cua appsettings.Development.json. Chay ban da build (exe),
rem  khong qua "dotnet run" - de build lai khong dung nhau voi app dang chay.
rem
rem    run-dev-app.bat
rem ================================================================
chcp 65001 >nul
setlocal
set "APP=%~dp0..\..\src\KnowledgePlatform.Api"
set "ASPNETCORE_ENVIRONMENT=Development"
set "ASPNETCORE_URLS=http://localhost:5119"

if not exist "%APP%\bin\Debug\net10.0\KnowledgePlatform.Api.exe" (
  echo Chua co ban build. Chay truoc: dotnet build src\KnowledgePlatform.Api
  pause
  exit /b 1
)

cd /d "%APP%"
echo App dev: http://localhost:5119   (Ctrl+C de tat)
"bin\Debug\net10.0\KnowledgePlatform.Api.exe"

echo.
pause
