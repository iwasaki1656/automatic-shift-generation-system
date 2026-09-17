@echo off
chcp 65001 > nul
cd /d "%~dp0"

if exist "%~dp0publish\ShiftManagement.App.exe" (
    start "" "%~dp0publish\ShiftManagement.App.exe"
) else (
    echo [情報] まだアプリが発行されていません。「デスクトップにショートカットを作成.bat」を実行します...
    call "%~dp0デスクトップにショートカットを作成.bat"
)
