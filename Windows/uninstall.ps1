# PunctuationToggle（Windows 版）を終了して削除し、「ログイン時に起動」の登録と設定を消す。
# Microsoft IME の句読点の設定は、最後に切り替えた状態のまま残る。
# 使い方: powershell -ExecutionPolicy Bypass -File .\Windows\uninstall.ps1
$ErrorActionPreference = 'Stop'

$destination = Join-Path $env:LOCALAPPDATA 'Programs\PunctuationToggle'

$running = Get-Process PunctuationToggle -ErrorAction SilentlyContinue
if ($running) {
    $running | Stop-Process -Force
    $running | Wait-Process -Timeout 5 -ErrorAction SilentlyContinue
}

Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'PunctuationToggle' -ErrorAction SilentlyContinue
Remove-Item -Path 'HKCU:\Software\PunctuationToggle' -Recurse -ErrorAction SilentlyContinue
Remove-Item -Path $destination -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "アンインストールしました: $destination"
