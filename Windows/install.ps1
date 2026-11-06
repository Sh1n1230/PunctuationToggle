# PunctuationToggle（Windows 版）をビルドして %LOCALAPPDATA%\Programs\PunctuationToggle にインストールし、起動する。
# 使い方: powershell -ExecutionPolicy Bypass -File .\Windows\install.ps1
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'InstallCommon.ps1')

$repositoryRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $PSScriptRoot 'PunctuationToggle\PunctuationToggle.csproj'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK が見つかりません。https://dotnet.microsoft.com/download から .NET 10 SDK をインストールしてください。"
}

# global.json は作業フォルダーから探されるため、リポジトリのルートで実行する（ほかのフォルダーから呼んでも版の固定が効く）。
# dotnet --version は global.json の規則で SDK を選ぶので、満たす SDK が無ければ失敗する
Push-Location $repositoryRoot
try {
    $requiredSdk = (Get-Content (Join-Path $repositoryRoot 'global.json') -Raw | ConvertFrom-Json).sdk.version
    # Windows PowerShell 5.1 では、Stop のままだと外部コマンドの標準エラー出力が例外になり、下の案内を表示できない
    $selectedSdk = & { $ErrorActionPreference = 'Continue'; dotnet --version 2>$null }
    if ($LASTEXITCODE -ne 0) {
        throw ".NET SDK $requiredSdk 以降（10.0 の SDK。global.json の指定）が必要です。https://dotnet.microsoft.com/download からインストールしてください。"
    }
    Write-Host ".NET SDK $selectedSdk でビルドします。"

    # 一時フォルダーに発行し、成功してからインストール先を作り直す（発行に失敗しても前の版が残り、
    # 前の版にしか無いファイルも残らない）
    $staging = Join-Path ([IO.Path]::GetTempPath()) ('PunctuationToggle-install-' + [Guid]::NewGuid())
    try {
        dotnet publish $project -c Release -o $staging --nologo
        if ($LASTEXITCODE -ne 0) { throw 'ビルドに失敗しました。' }

        Stop-InstalledApp
        Remove-InstalledFolder
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        Get-ChildItem -LiteralPath $staging -Force | Move-Item -Destination $destination
        # uninstall.ps1 が、exe が消えた後でもこのアプリのフォルダーと分かるようにする目印
        Set-Content -LiteralPath (Join-Path $destination $installMarkerName) -Value 'PunctuationToggle' -Encoding ASCII
    }
    finally {
        if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
    }
}
finally {
    Pop-Location
}

Write-Host "インストールしました: $destination"

# 別の場所の PunctuationToggle が動いていると、二重起動の防止で新しい方は起動しない
$others = Get-Process PunctuationToggle -ErrorAction SilentlyContinue | Where-Object { $_.SessionId -eq $sessionId }
if ($others) {
    Write-Warning "別の場所の PunctuationToggle が動いています（$($others[0].Path)）。終了してから、インストールした PunctuationToggle を起動してください。"
}
elseif (Test-Administrator) {
    # 管理者の PowerShell から起動すると、アプリも管理者権限で動いてしまう（インストール先はユーザーのフォルダーなので、管理者権限は要らない）
    Write-Warning "管理者として実行されているため、PunctuationToggle は起動しません。エクスプローラーで $installedExe を開いて起動してください。"
}
else {
    Start-Process $installedExe
}
Write-Host 'タスクトレイの「、。」を右クリックすると、切り替えキーの変更や「ログイン時に起動」の設定ができます。'
