@echo off
chcp 65001 > nul
setlocal enabledelayedexpansion
title 勤務表自動作成システム - デスクトップショートカット作成

cd /d "%~dp0"

echo ========================================================
echo   勤務表自動作成システム - セットアップ
echo ========================================================
echo.

REM 1. .NET SDK の確認
echo [1/3] .NET SDK のインストールを確認しています...
for /f "delims=" %%i in ('dotnet --list-sdks 2^>nul') do (
    set "SDK_FOUND=%%i"
)

if not defined SDK_FOUND (
    echo.
    echo ========================================================
    echo 【確認】.NET SDK が見つかりませんでした。
    echo ========================================================
    echo システムをビルドして実行ファイルを作成するには、
    echo マイクロソフト公式の「.NET 9 SDK」が必要です。
    echo.
    echo 自動的にブラウザでダウンロードページを開きます。
    echo 「.NET SDK x64」をダウンロードしてインストールしてください。
    echo ========================================================
    echo.
    start https://dotnet.microsoft.com/download/dotnet/9.0
    echo インストールが完了したら、もう一度このファイルをダブルクリックしてください。
    echo.
    pause
    exit /b 1
)

echo   OK: .NET SDK が見つかりました: !SDK_FOUND!
echo.

REM 2. アプリケーションの発行 (Publish)
echo [2/3] アプリケーションをビルド・発行中 (Release)...
echo しばらくお待ちください...
dotnet publish "src\ShiftManagement.App\ShiftManagement.App.csproj" -c Release -o "%~dp0publish"

if %errorlevel% neq 0 (
    echo.
    echo 【エラー】ビルドまたは発行に失敗しました。
    echo.
    pause
    exit /b 1
)

echo   OK: ビルドが正常に完了しました。
echo.

REM 3. デスクトップにショートカットを作成
echo [3/3] デスクトップにショートカットを作成中...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $desktop = [Environment]::GetFolderPath('Desktop'); $shortcut = $ws.CreateShortcut((Join-Path $desktop '勤務表自動作成システム.lnk')); $shortcut.TargetPath = '%~dp0publish\ShiftManagement.App.exe'; $shortcut.WorkingDirectory = '%~dp0publish'; $shortcut.IconLocation = '%~dp0publish\ShiftManagement.App.exe,0'; $shortcut.Description = '介護施設・看護施設向け勤務表自動作成システム'; $shortcut.Save()"

if %errorlevel% neq 0 (
    echo 【警告】ショートカットの自動作成に失敗しました。
    echo 手動で publish フォルダ内の ShiftManagement.App.exe のショートカットを作成してください。
) else (
    echo.
    echo ========================================================
    echo  ★ 成功！ デスクトップに「勤務表自動作成システム」の
    echo     ショートカットアイコンを作成しました！
    echo ========================================================
)

echo.
set /p START_APP="今すぐアプリケーションを起動しますか？ (Y/N) [既定値: Y]: "
if "%START_APP%"=="" set START_APP=Y
if /i "%START_APP%"=="Y" (
    start "" "%~dp0publish\ShiftManagement.App.exe"
)

echo.
echo 処理が完了しました。ウィンドウを閉じるには何かキーを押してください...
pause > nul
