# install.ps1 と uninstall.ps1 で使う、インストール先の定義と、終了・削除の手順。
# このファイルは直接実行せず、. で読み込む。
#
# フォルダーごと削除するため、想定した場所の、このアプリのフォルダーであることを確かめてから削除する。

if (-not [IO.Path]::IsPathRooted($env:LOCALAPPDATA)) {
    throw "LOCALAPPDATA が絶対パスではありません（$env:LOCALAPPDATA）。インストール先を決められないため中止します。"
}
$programsFolder = Join-Path $env:LOCALAPPDATA 'Programs'
$destination = Join-Path $programsFolder 'PunctuationToggle'
$installedExe = Join-Path $destination 'PunctuationToggle.exe'
# install.ps1 がインストール先に置く目印。削除が途中で失敗して exe が消えた後でも、このアプリのフォルダーと分かる
$installMarkerName = '.punctuationtoggle-install'
$sessionId = (Get-Process -Id $PID).SessionId

function Test-ReparsePoint([string] $Path) {
    $item = Get-Item -LiteralPath $Path -Force
    return [bool]($item.Attributes -band [IO.FileAttributes]::ReparsePoint)
}

function Test-Administrator {
    $principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# 今のセッションで、インストール先の exe から起動した PunctuationToggle だけを終了させ、終了するまで待つ
# （ほかのユーザーや、別の場所でビルドした PunctuationToggle は終了させない）
function Stop-InstalledApp {
    [CmdletBinding(SupportsShouldProcess)]
    param()
    $running = Get-Process PunctuationToggle -ErrorAction SilentlyContinue |
        Where-Object { $_.SessionId -eq $sessionId -and $_.Path -eq $installedExe }
    if (-not $running) { return }
    if (-not $PSCmdlet.ShouldProcess($installedExe, '終了')) { return }
    $running | Stop-Process -Force
    foreach ($process in $running) {
        if (-not $process.WaitForExit(5000)) {
            throw 'PunctuationToggle を終了できませんでした。タスク マネージャーで終了してから、もう一度実行してください。'
        }
    }
}

# インストール先が、このアプリのフォルダー（exe か目印がある）であることを確かめてから削除する。無ければ何もしない
function Remove-InstalledFolder {
    [CmdletBinding(SupportsShouldProcess)]
    param()
    if (-not (Test-Path -LiteralPath $destination)) { return }
    # シンボリックリンクやジャンクションだと、リンク先の別の場所を消すおそれがある
    if ((Test-ReparsePoint $programsFolder) -or (Test-ReparsePoint $destination)) {
        throw "$destination またはその親のフォルダーがシンボリックリンク（ジャンクション）です。意図しない場所を削除しないよう中止します。"
    }
    $hasExe = Test-Path -LiteralPath $installedExe -PathType Leaf
    $hasMarker = Test-Path -LiteralPath (Join-Path $destination $installMarkerName) -PathType Leaf
    if (-not ($hasExe -or $hasMarker)) {
        throw "$destination に PunctuationToggle.exe がありません。PunctuationToggle のフォルダーではないおそれがあるため、削除せずに中止します。"
    }

    if (-not $PSCmdlet.ShouldProcess($destination, '削除')) { return }
    Write-Host "削除します: $destination"
    try {
        # 目印は最後に消す（途中で失敗しても、次の実行でこのアプリのフォルダーと分かるように）
        Get-ChildItem -LiteralPath $destination -Force |
            Where-Object { $_.Name -ne $installMarkerName } |
            Remove-Item -Recurse -Force
        Remove-Item -LiteralPath $destination -Recurse -Force
    }
    catch {
        throw "$destination を削除できませんでした。PunctuationToggle が終了していること、ほかのアプリ（PowerShell やエクスプローラーなど）でこのフォルダーを開いていないことを確かめてから、もう一度実行してください。`n$($_.Exception.Message)"
    }
}
