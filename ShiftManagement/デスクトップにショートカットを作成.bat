@echo off
chcp 65001 > nul
setlocal enabledelayedexpansion

echo ========================================================
echo   勤務表自動作成システム - デスクトップショートカット作成
echo ========================================================
echo.

cd /d "%~dp0"

:: 1. .NET SDK の確認
echo [1/3] .NET SDK のインストールを確認中...
where dotnet > nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo 【エラー】.NET SDK が見つかりませんでした。
    echo 本システムの動作には .NET 9 SDK (または .NET 8 SDK) が必要です。
    echo 以下のマイクロソフト公式ページからインストールしてください:
    echo https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

:: 2. アプリケーションの発行 (Publish)
echo [2/3] アプリケーションをビルド・発行中 (Release)...
dotnet publish src\ShiftManagement.App\ShiftManagement.App.csproj -c Release -o publish
if %errorlevel% neq 0 (
    echo.
    echo 【エラー】ビルドまたは発行に失敗しました。
    pause
    exit /b 1
)

:: 3. デスクトップにショートカットを作成 (PowerShell 経由)
echo [3/3] デスクトップにショートカットを作成中...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$ws = New-Object -ComObject WScript.Shell;" ^
    "$desktop = [Environment]::GetFolderPath('Desktop');" ^
    "$shortcutPath = Join-Path $desktop '勤務表自動作成システム.lnk';" ^
    "$targetPath = Join-Path (Get-Location) 'publish\ShiftManagement.App.exe';" ^
    "$iconPath = Join-Path (Get-Location) 'publish\ShiftManagement.App.exe';" ^
    "$shortcut = $ws.CreateShortcut($shortcutPath);" ^
    "$shortcut.TargetPath = $targetPath;" ^
    "$shortcut.WorkingDirectory = Join-Path (Get-Location) 'publish';" ^
    "$shortcut.IconLocation = $iconPath + ',0';" ^
    "$shortcut.Description = '介護施設・看護施設向け勤務表自動作成システム';" ^
    "$shortcut.Save();"

if %errorlevel% neq 0 (
    echo.
    echo 【警告】ショートカットの自動作成に失敗しました。
    echo 代わりに publish\ShiftManagement.App.exe を右クリックして「送る」→「デスクトップ（ショートカットを作成）」を行ってください。
) else (
    echo.
    echo ========================================================
    echo  ★ 完了！ デスクトップに「勤務表自動作成システム」の
    echo     ショートカットを作成しました！
    echo ========================================================
)

echo.
set /p START_NOW="今すぐアプリケーションを起動しますか？ (Y/N): "
if /i "%START_NOW%"=="Y" (
    start "" "publish\ShiftManagement.App.exe"
)

echo.
echo 終了します。何かキーを押してください...
pause > nul
