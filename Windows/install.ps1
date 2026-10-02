# PunctuationToggle（Windows 版）をビルドして %LOCALAPPDATA%\Programs\PunctuationToggle にインストールし、起動する。
# 使い方: powershell -ExecutionPolicy Bypass -File .\Windows\install.ps1
$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'PunctuationToggle\PunctuationToggle.csproj'
$destination = Join-Path $env:LOCALAPPDATA 'Programs\PunctuationToggle'
$requiredSdkMajorVersion = 10

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK が見つかりません。https://dotnet.microsoft.com/download から .NET $requiredSdkMajorVersion SDK をインストールしてください。"
}
$sdkMajorVersions = dotnet --list-sdks | ForEach-Object { [int]($_ -split '\.')[0] }
if (-not ($sdkMajorVersions | Where-Object { $_ -ge $requiredSdkMajorVersion })) {
    throw ".NET $requiredSdkMajorVersion SDK 以降が必要です。https://dotnet.microsoft.com/download からインストールしてください。"
}

# 実行中だと上書きできないので終了させる
$running = Get-Process PunctuationToggle -ErrorAction SilentlyContinue
if ($running) {
    $running | Stop-Process -Force
    $running | Wait-Process -Timeout 5 -ErrorAction SilentlyContinue
}

dotnet publish $project -c Release -o $destination --nologo
if ($LASTEXITCODE -ne 0) { throw 'ビルドに失敗しました。' }

Start-Process (Join-Path $destination 'PunctuationToggle.exe')

Write-Host "インストールしました: $destination"
Write-Host 'タスクトレイの「、。」を右クリックすると、切り替えキーの変更や「ログイン時に起動」の設定ができます。'
