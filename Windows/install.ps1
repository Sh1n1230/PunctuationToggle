# PunctuationToggle（Windows 版）をビルドして %LOCALAPPDATA%\Programs にインストールし、起動する。
$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'PunctuationToggle\PunctuationToggle.csproj'
$dest = Join-Path $env:LOCALAPPDATA 'Programs\PunctuationToggle'

# 実行中だと上書きできないので終了させる
Get-Process PunctuationToggle -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

dotnet publish $project -c Release -o $dest --nologo
if ($LASTEXITCODE -ne 0) { throw 'ビルドに失敗しました。' }

Start-Process (Join-Path $dest 'PunctuationToggle.exe')

Write-Host "インストールしました: $dest"
Write-Host 'タスクトレイの「、。」を右クリックすると「ログイン時に起動」を設定できます。'
