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

# README のとおり Xcode 26 以降が要る。Command Line Tools だけの環境では xcodebuild はあっても -version が失敗する
MINIMUM_XCODE_MAJOR_VERSION=26
if ! xcode_version=$(xcodebuild -version 2>/dev/null | sed -n 's/^Xcode \([0-9][0-9]*\).*/\1/p'); then
  xcode_version=
fi
if [ -z "$xcode_version" ]; then
  echo "Xcode が見つかりません。Xcode をインストールし、sudo xcode-select -s /Applications/Xcode.app を実行してください。" >&2
  exit 1
fi
if [ "$xcode_version" -lt "$MINIMUM_XCODE_MAJOR_VERSION" ]; then
  echo "Xcode $MINIMUM_XCODE_MAJOR_VERSION 以降が必要です（今の Xcode は ${xcode_version}）。" >&2
  exit 1
fi

# CI・リリースと同じくアドホック署名でビルドする。ビルドに失敗しても、動いているアプリはそのまま使える
xcodebuild -project "$APP_NAME.xcodeproj" -scheme "$APP_NAME" \
  -configuration Release -destination "platform=macOS" \
  -derivedDataPath "$BUILD_DIR" CODE_SIGN_IDENTITY=- -quiet

check_dest_parent
mkdir -p "$DEST_PARENT"

# 同じフォルダーに一時的な名前でコピーしてから、mv（同じボリュームの中では名前の変更）で置き換える。
# コピーが途中で止まっても、壊れたバンドルがインストール先に残らない
STAGING="$DEST_PARENT/.$APP_NAME.app.installing"
rm -rf "$STAGING"
trap 'rm -rf "$STAGING"' EXIT
ditto "$BUILD_DIR/Build/Products/Release/$APP_NAME.app" "$STAGING"

quit_running_app
remove_installed_app
mv "$STAGING" "$DEST"
trap - EXIT

tccutil reset Accessibility "$BUNDLE_ID" >/dev/null 2>&1 || true
tccutil reset Accessibility "$LEGACY_BUNDLE_ID" >/dev/null 2>&1 || true

open "$DEST"

echo "インストールしました: $DEST"
echo "表示されるダイアログから「システム設定を開く」を選び、$APP_NAME をオンにしてください。"
echo "許可されるとメニューバーの「⚠︎」が「、。」に変わります（再起動は不要です）。"
echo
echo "一覧に $APP_NAME が複数表示される場合は、古いビルドの項目が残っています。"
echo "すべて選んで「−」で削除してから、もう一度このスクリプトを実行してください。"
