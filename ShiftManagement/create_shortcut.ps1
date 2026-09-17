# 勤務表自動作成システム - デスクトップショートカット作成スクリプト (PowerShell)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  勤務表自動作成システム - デスクトップショートカット作成" -ForegroundColor Cyan
Write-Host "========================================================"
Write-Host ""

# 1. .NET SDK チェック
Write-Host "[1/3] .NET SDK を確認中..."
try {
    $dotnetVer = & dotnet --version
    Write-Host "  .NET SDK 検出: $dotnetVer" -ForegroundColor Green
} catch {
    Write-Host "【エラー】.NET SDK が見つかりませんでした。" -ForegroundColor Red
    Write-Host "https://dotnet.microsoft.com/download からインストールしてください。"
    Pause
    exit 1
}

# 2. アプリケーションの発行
Write-Host "[2/3] アプリケーションをビルド・発行中..."
& dotnet publish src\ShiftManagement.App\ShiftManagement.App.csproj -c Release -o publish
if ($LASTEXITCODE -ne 0) {
    Write-Host "【エラー】ビルドに失敗しました。" -ForegroundColor Red
    Pause
    exit 1
}

# 3. ショートカット作成
Write-Host "[3/3] デスクトップにショートカットを作成中..."
$wshShell = New-Object -ComObject WScript.Shell
$desktopPath = [Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktopPath "勤務表自動作成システム.lnk"
$targetExe = Join-Path $scriptDir "publish\ShiftManagement.App.exe"

$shortcut = $wshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = Join-Path $scriptDir "publish"
$shortcut.IconLocation = "$targetExe,0"
$shortcut.Description = "介護施設・看護施設向け勤務表自動作成システム"
$shortcut.Save()

Write-Host ""
Write-Host "★ 完了！ デスクトップに「勤務表自動作成システム」を作成しました！" -ForegroundColor Green
Write-Host ""

$answer = Read-Host "今すぐ起動しますか？ (Y/N)"
if ($answer -eq "Y" -or $answer -eq "y") {
    Start-Process $targetExe
}
