@echo off
setlocal enabledelayedexpansion
title Shift Management System - Setup

cd /d "%~dp0"

echo ========================================================
echo   Shift Management System - Setup
echo ========================================================
echo.

REM 1. Check .NET SDK
echo [1/3] Checking .NET SDK installation...
for /f "delims=" %%i in ('dotnet --list-sdks 2^>nul') do (
    set "SDK_FOUND=%%i"
)

if not defined SDK_FOUND (
    echo.
    echo ========================================================
    echo [NOTICE] .NET SDK not found.
    echo .NET 9 SDK is required to build the application.
    echo Opening download page in browser...
    echo ========================================================
    echo.
    start https://dotnet.microsoft.com/download/dotnet/9.0
    echo Please install .NET SDK x64 and run this file again.
    pause
    exit /b 1
)

echo   OK: Found .NET SDK: !SDK_FOUND!
echo.

REM 2. Publish application
echo [2/3] Building and publishing application (Release)...
echo Please wait a moment...
dotnet publish "src\ShiftManagement.App\ShiftManagement.App.csproj" -c Release -o "%~dp0publish"

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Build or publish failed.
    echo.
    pause
    exit /b 1
)

echo   OK: Build completed successfully.
echo.

REM 3. Create desktop shortcut
echo [3/3] Creating desktop shortcut...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $desktop = [Environment]::GetFolderPath('Desktop'); $shortcut = $ws.CreateShortcut((Join-Path $desktop '勤務表自動作成システム.lnk')); $shortcut.TargetPath = '%~dp0publish\ShiftManagement.App.exe'; $shortcut.WorkingDirectory = '%~dp0publish'; $shortcut.IconLocation = '%~dp0publish\ShiftManagement.App.exe,0'; $shortcut.Description = '介護施設・看護施設向け勤務表自動作成システム'; $shortcut.Save()"

if %errorlevel% neq 0 (
    echo [WARNING] Could not create shortcut automatically.
) else (
    echo.
    echo ========================================================
    echo  SUCCESS: Created desktop shortcut!
    echo ========================================================
)

echo.
set /p START_APP="Launch application now? (Y/N) [Default: Y]: "
if "%START_APP%"=="" set START_APP=Y
if /i "%START_APP%"=="Y" (
    start "" "%~dp0publish\ShiftManagement.App.exe"
)

echo.
echo Setup completed. Press any key to close this window...
pause > nul
