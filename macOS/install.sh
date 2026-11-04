#!/bin/sh
# PunctuationToggle をビルドして ~/Applications にインストールし、起動する。
#
# アドホック署名のアプリはビルドし直すたびに署名が変わるため、
# アクセシビリティの許可が一覧上はオンのまま無効になる。
# そこで古い許可をリセットしてから起動し、許可を求め直す。
set -eu

cd "$(dirname "$0")"

# shellcheck source-path=SCRIPTDIR source=app_bundle.sh
. ./app_bundle.sh
BUILD_DIR=build

if ! command -v xcodebuild >/dev/null 2>&1; then
  echo "xcodebuild が見つかりません。Xcode をインストールしてください。" >&2
  exit 1
fi

pkill -x "$APP_NAME" 2>/dev/null || true

xcodebuild -project "$APP_NAME.xcodeproj" -scheme "$APP_NAME" \
  -configuration Release -destination "platform=macOS" \
  -derivedDataPath "$BUILD_DIR" -quiet

check_dest_parent
mkdir -p "$DEST_PARENT"
remove_installed_app
cp -R "$BUILD_DIR/Build/Products/Release/$APP_NAME.app" "$DEST"

tccutil reset Accessibility "$BUNDLE_ID" >/dev/null 2>&1 || true
tccutil reset Accessibility "$LEGACY_BUNDLE_ID" >/dev/null 2>&1 || true

open "$DEST"

echo "インストールしました: $DEST"
echo "表示されるダイアログから「システム設定を開く」を選び、$APP_NAME をオンにしてください。"
echo "許可されるとメニューバーの「⚠︎」が「、。」に変わります（再起動は不要です）。"
echo
echo "一覧に $APP_NAME が複数表示される場合は、古いビルドの項目が残っています。"
echo "すべて選んで「−」で削除してから、もう一度このスクリプトを実行してください。"
