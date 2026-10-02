#!/bin/sh
# PunctuationToggle をビルドして ~/Applications にインストールする。
#
# アドホック署名のアプリはビルドし直すたびに署名が変わるため、
# アクセシビリティの許可が一覧上はオンのまま無効になる。
# そこで古い許可をリセットしてから起動し、許可を求め直す。
set -eu

cd "$(dirname "$0")"

BUNDLE_ID=com.example.PunctuationToggle
DEST="$HOME/Applications/PunctuationToggle.app"

pkill -x PunctuationToggle 2>/dev/null || true

xcodebuild -project PunctuationToggle.xcodeproj -scheme PunctuationToggle \
  -configuration Release -destination "platform=macOS" \
  -derivedDataPath build -quiet

mkdir -p "$HOME/Applications"
rm -rf "$DEST"
cp -R build/Build/Products/Release/PunctuationToggle.app "$DEST"

tccutil reset Accessibility "$BUNDLE_ID" >/dev/null 2>&1 || true

open "$DEST"

echo "インストールしました: $DEST"
echo "表示されるダイアログから「システム設定を開く」を選び、PunctuationToggle をオンにしてください。"
echo "許可されるとメニューバーの「⚠︎」が「、。」に変わります（再起動は不要です）。"
