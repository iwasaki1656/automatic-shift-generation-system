# 勤務表自動作成システム - デスクトップショートカット作成スクリプト (PowerShell)
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  勤務表自動作成システム - デスクトップショートカット作成" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

# 1. .NET SDK チェック
Write-Host "[1/3] .NET SDK のインストールを確認中..." -ForegroundColor Yellow

$sdkList = @()
try {
    $sdkList = (& dotnet --list-sdks 2>$null)
} catch {
    # dotnetコマンド自体がない場合
}

if (-not $sdkList -or $sdkList.Count -eq 0) {
    Write-Host ""
    Write-Host "【重要】 .NET SDK (開発キット) が見つかりませんでした。" -ForegroundColor Red
    Write-Host ""
    Write-Host "現在、お使いのWindowsには「.NET ランタイム」のみが入っており、" -ForegroundColor Yellow
    Write-Host "システムをビルドして実行ファイルを作成するための「.NET SDK」が未導入です。" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "ブラウザで .NET 9 SDK の公式ダウンロードページを開きます..." -ForegroundColor Cyan
    Write-Host "表示されたページの「.NET SDK x64」をダウンロードしてインストールしてください。" -ForegroundColor Cyan
    Write-Host ""

    # ブラウザを開く
    Start-Process "https://dotnet.microsoft.com/download/dotnet/9.0"

    Write-Host "インストール完了後、もう一度このスクリプトを実行してください。" -ForegroundColor Green
    Write-Host ""
    Read-Host "Enterキーを押すと終了します..."
    exit 1
}

Write-Host "  OK: .NET SDK が見つかりました (" -NoNewline -ForegroundColor Green
Write-Host ($sdkList[0]) -NoNewline -ForegroundColor White
Write-Host ")" -ForegroundColor Green

# 2. アプリケーションの発行 (Publish)
Write-Host ""
Write-Host "[2/3] アプリケーションをビルド・発行中 (Release)..." -ForegroundColor Yellow

$publishDir = Join-Path $scriptDir "publish"
$projectPath = Join-Path $scriptDir "src\ShiftManagement.App\ShiftManagement.App.csproj"

& dotnet publish $projectPath -c Release -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "【エラー】ビルドまたは発行に失敗しました。" -ForegroundColor Red
    Read-Host "Enterキーを押すと終了します..."
    exit 1
}

# 3. デスクトップにショートカットを作成
Write-Host ""
Write-Host "[3/3] デスクトップにショートカットを作成中..." -ForegroundColor Yellow

$desktopPath = [Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktopPath "勤務表自動作成システム.lnk"
$targetExe = Join-Path $publishDir "ShiftManagement.App.exe"

$wshShell = New-Object -ComObject WScript.Shell
$shortcut = $wshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = $publishDir
$shortcut.IconLocation = "$targetExe,0"
$shortcut.Description = "介護施設・看護施設向け勤務表自動作成システム"
$shortcut.Save()

Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "  ★ 完了！ デスクトップに「勤務表自動作成システム」の" -ForegroundColor Green
Write-Host "     ショートカットを作成しました！" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host ""

$answer = Read-Host "今すぐアプリケーションを起動しますか？ (Y/N)"
if ($answer -eq "Y" -or $answer -eq "y") {
    Start-Process $targetExe
}
