@echo off
cd /d "%~dp0"

if exist "publish\ShiftManagement.App.exe" (
    start "" "publish\ShiftManagement.App.exe"
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "create_shortcut.ps1"
)
