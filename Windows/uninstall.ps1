# PunctuationToggle（Windows 版）を終了して削除し、「ログイン時に起動」の登録と設定を消す。
# Microsoft IME の句読点の設定は、最後に切り替えた状態のまま残る。
# 使い方: powershell -ExecutionPolicy Bypass -File .\Windows\uninstall.ps1
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'InstallCommon.ps1')

Stop-InstalledApp

# 値やキーがあるときだけ削除し、消せなければ場所を示して中止する（消せなかったのに「アンインストールしました」と表示しない）
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$settingsKey = 'HKCU:\Software\PunctuationToggle'
try {
    if ($null -ne (Get-ItemProperty -Path $runKey -Name 'PunctuationToggle' -ErrorAction SilentlyContinue)) {
        Remove-ItemProperty -Path $runKey -Name 'PunctuationToggle'
    }
    if (Test-Path -Path $settingsKey) {
        Remove-Item -Path $settingsKey -Recurse
    }
}
catch {
    throw "「ログイン時に起動」の登録（$runKey の PunctuationToggle）か設定（$settingsKey）を削除できませんでした。もう一度実行するか、レジストリ エディターで削除してください。`n$($_.Exception.Message)"
}

$wasInstalled = Test-Path -LiteralPath $destination
Remove-InstalledFolder

if ($wasInstalled) {
    Write-Host "アンインストールしました: $destination"
}
else {
    Write-Host "$destination にアプリはありませんでした。「ログイン時に起動」の登録と設定だけを削除しました。"
}
