# Windows 版のアプリのアイコン（Windows/PunctuationToggle/PunctuationToggle.ico）を、macOS 版のアプリアイコンの PNG
# （macOS/PunctuationToggle/Assets.xcassets/AppIcon.appiconset。macOS/Scripts/generate_app_icon.swift で作る）から作る。
# 図柄を両 OS で同じにするため、macOS 版の PNG を作り直したら、このスクリプトも実行し直す。
# 使い方（Windows の PowerShell 7）: pwsh Windows/Scripts/generate_app_icon.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repositoryRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$sourceDirectory = Join-Path $repositoryRoot 'macOS/PunctuationToggle/Assets.xcassets/AppIcon.appiconset'
$destination = Join-Path $repositoryRoot 'Windows/PunctuationToggle/PunctuationToggle.ico'

# Windows がアイコンに使う大きさ（エクスプローラー・タスクバー・タイトルバー・表示倍率ごと）
$sizes = @(16, 20, 24, 32, 48, 256)

# macOS 版の PNG（ピクセル数 → ファイル）
$sources = @{}
foreach ($file in Get-ChildItem -LiteralPath $sourceDirectory -Filter 'icon_*.png') {
    $image = [System.Drawing.Image]::FromFile($file.FullName)
    try {
        $sources[$image.Width] = $file.FullName
    }
    finally {
        $image.Dispose()
    }
}

# 大きさごとに、それ以上の大きさで最も小さい PNG を縮小して作る（縮小は1回だけにして、にじみを抑える）
$entries = foreach ($size in $sizes) {
    $sourceSize = $sources.Keys | Where-Object { $_ -ge $size } | Sort-Object | Select-Object -First 1
    if ($null -eq $sourceSize) {
        throw "$size ピクセル以上の PNG が $sourceDirectory にありません。"
    }
    $source = [System.Drawing.Image]::FromFile($sources[$sourceSize])
    $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $stream = [System.IO.MemoryStream]::new()
    try {
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.DrawImage($source, 0, 0, $size, $size)
        # .ico の中には PNG のまま入れる（Windows Vista 以降が読める形式）
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        [pscustomobject]@{ Size = $size; Data = $stream.ToArray() }
    }
    finally {
        $stream.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
        $source.Dispose()
    }
}

# ICO の形式: ICONDIR（6バイト）、ICONDIRENTRY（16バイト × 枚数）、各画像のデータ
# https://learn.microsoft.com/previous-versions/ms997538(v=msdn.10)
$output = [System.IO.MemoryStream]::new()
$writer = [System.IO.BinaryWriter]::new($output)
try {
    $writer.Write([uint16]0)               # 予約
    $writer.Write([uint16]1)               # 種類（1 = アイコン）
    $writer.Write([uint16]$entries.Count)  # 枚数
    $offset = 6 + (16 * $entries.Count)
    foreach ($entry in $entries) {
        # 幅と高さは1バイトで、256 は 0 と書く
        $dimension = if ($entry.Size -ge 256) { 0 } else { $entry.Size }
        $writer.Write([byte]$dimension)            # 幅
        $writer.Write([byte]$dimension)            # 高さ
        $writer.Write([byte]0)                     # 色数（パレットなし）
        $writer.Write([byte]0)                     # 予約
        $writer.Write([uint16]1)                   # カラープレーン
        $writer.Write([uint16]32)                  # 1ピクセルのビット数
        $writer.Write([uint32]$entry.Data.Length)  # データの大きさ
        $writer.Write([uint32]$offset)             # データの位置
        $offset += $entry.Data.Length
    }
    foreach ($entry in $entries) {
        $writer.Write($entry.Data)
    }
    $writer.Flush()
    [System.IO.File]::WriteAllBytes($destination, $output.ToArray())
}
finally {
    $writer.Dispose()
    $output.Dispose()
}
Write-Host "作成しました: $destination（$($sizes -join '・') ピクセル）"
