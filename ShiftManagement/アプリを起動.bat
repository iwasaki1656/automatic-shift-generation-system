@echo off
chcp 65001 > nul
cd /d "%~dp0"

if exist "publish\ShiftManagement.App.exe" (
    start "" "publish\ShiftManagement.App.exe"
) else (
    echo 初回起動のためビルド中...
    dotnet run --project src\ShiftManagement.App\ShiftManagement.App.csproj
)
