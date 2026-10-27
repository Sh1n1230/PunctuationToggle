# PunctuationToggle（Windows 版）を終了して削除し、「ログイン時に起動」の登録と設定を消す。
# Microsoft IME の句読点の設定は、最後に切り替えた状態のまま残る。
# 使い方: powershell -ExecutionPolicy Bypass -File .\Windows\uninstall.ps1
$ErrorActionPreference = 'Stop'

$programsFolder = Join-Path $env:LOCALAPPDATA 'Programs'
$destination = Join-Path $programsFolder 'PunctuationToggle'

# フォルダーごと削除するため、想定した場所の、このアプリのフォルダーであることを確かめてから削除する
function Test-ReparsePoint([string] $Path) {
    $item = Get-Item -LiteralPath $Path -Force
    return [bool]($item.Attributes -band [IO.FileAttributes]::ReparsePoint)
}

if (-not [IO.Path]::IsPathRooted($env:LOCALAPPDATA)) {
    throw "LOCALAPPDATA が絶対パスではありません（$env:LOCALAPPDATA）。削除する場所を決められないため中止します。"
}

$running = Get-Process PunctuationToggle -ErrorAction SilentlyContinue
if ($running) {
    $running | Stop-Process -Force
    $running | Wait-Process -Timeout 5 -ErrorAction SilentlyContinue
}

Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'PunctuationToggle' -ErrorAction SilentlyContinue
Remove-Item -Path 'HKCU:\Software\PunctuationToggle' -Recurse -ErrorAction SilentlyContinue

if (Test-Path -LiteralPath $destination) {
    # シンボリックリンクやジャンクションだと、リンク先の別の場所を消すおそれがある
    if ((Test-ReparsePoint $programsFolder) -or (Test-ReparsePoint $destination)) {
        throw "$destination またはその親のフォルダーがシンボリックリンク（ジャンクション）です。意図しない場所を削除しないよう中止します。"
    }
    if (-not (Test-Path -LiteralPath (Join-Path $destination 'PunctuationToggle.exe') -PathType Leaf)) {
        throw "$destination に PunctuationToggle.exe がありません。PunctuationToggle のフォルダーではないおそれがあるため、削除せずに中止します。"
    }

    Write-Host "削除します: $destination"
    try {
        Remove-Item -LiteralPath $destination -Recurse -Force
    }
    catch {
        throw "$destination を削除できませんでした。PunctuationToggle が終了していること、ほかのアプリ（PowerShell やエクスプローラーなど）でこのフォルダーを開いていないことを確かめてから、もう一度実行してください。`n$($_.Exception.Message)"
    }
}

Write-Host "アンインストールしました: $destination"
