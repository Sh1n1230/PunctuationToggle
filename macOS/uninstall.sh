#!/bin/sh
# PunctuationToggle を終了して ~/Applications から削除し、アクセシビリティの許可と設定を消す。
# 日本語入力の句読点の設定は、最後に切り替えた状態のまま残る。
set -eu

cd "$(dirname "$0")"

# shellcheck source-path=SCRIPTDIR source=app_bundle.sh
. ./app_bundle.sh

quit_running_app

# ログイン項目は、アプリの「ログイン時に起動」をオフにしてからアンインストールすると確実に消える。
# 残った場合は、システム設定 →「一般」→「ログイン項目と機能拡張」から削除できる。
app_was_installed=false
if [ -e "$DEST" ]; then
  app_was_installed=true
fi
remove_installed_app

for id in "$BUNDLE_ID" "$LEGACY_BUNDLE_ID"; do
  tccutil reset Accessibility "$id" >/dev/null 2>&1 || true
  defaults delete "$id" >/dev/null 2>&1 || true
done

if [ "$app_was_installed" = true ]; then
  echo "アンインストールしました: $DEST"
else
  echo "$DEST にアプリはありませんでした。アクセシビリティの許可と設定だけを削除しました。"
fi
# このスクリプトは ~/Applications のアプリだけを消す。/Applications に入れた場合は手で消す
if [ -e "/Applications/$APP_NAME.app" ]; then
  echo "/Applications/$APP_NAME.app は削除していません。不要であれば、Finder でゴミ箱に入れてください。"
fi
