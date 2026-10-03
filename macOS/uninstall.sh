#!/bin/sh
# PunctuationToggle を終了して ~/Applications から削除し、アクセシビリティの許可と設定を消す。
# 日本語入力の句読点の設定は、最後に切り替えた状態のまま残る。
set -eu

APP_NAME=PunctuationToggle
BUNDLE_ID=io.github.sh1n1230.PunctuationToggle
LEGACY_BUNDLE_ID=com.example.PunctuationToggle
DEST="$HOME/Applications/$APP_NAME.app"

pkill -x "$APP_NAME" 2>/dev/null || true

# ログイン項目は、アプリの「ログイン時に起動」をオフにしてからアンインストールすると確実に消える。
# 残った場合は、システム設定 →「一般」→「ログイン項目と機能拡張」から削除できる。
rm -rf "$DEST"

for id in "$BUNDLE_ID" "$LEGACY_BUNDLE_ID"; do
  tccutil reset Accessibility "$id" >/dev/null 2>&1 || true
  defaults delete "$id" >/dev/null 2>&1 || true
done

echo "アンインストールしました: $DEST"
