# shellcheck shell=sh
# install.sh と uninstall.sh で使う、インストール先の定義と削除の手順。
# このファイルは直接実行せず、. で読み込む。
#
# rm -rf の対象を間違えると利用者のファイルを消してしまうため、
# 削除する前に、想定した場所の、このアプリのバンドルであることを確かめる。

APP_NAME=PunctuationToggle
BUNDLE_ID=io.github.sh1n1230.PunctuationToggle
# v1.0.0 までのバンドルID（古い許可や設定を削除するため）
LEGACY_BUNDLE_ID=com.example.PunctuationToggle

# root で実行すると、root の HOME（/var/root）にインストールしたり、root が持つファイルを作ったりするため中止する
if [ "$(id -u)" -eq 0 ]; then
  echo "root では実行できません。sudo を付けずに実行してください。" >&2
  exit 1
fi

case "${HOME:-}" in
  /*) ;;
  *)
    echo "HOME が絶対パスではありません（HOME=${HOME:-}）。インストール先を決められないため中止します。" >&2
    exit 1
    ;;
esac
DEST_PARENT="$HOME/Applications"
DEST="$DEST_PARENT/$APP_NAME.app"

# インストール先の親フォルダーがシンボリックリンクだと、リンク先の別の場所を消すおそれがあるため中止する
check_dest_parent() {
  if [ -L "$DEST_PARENT" ]; then
    echo "$DEST_PARENT はシンボリックリンクです。意図しない場所を変更しないよう中止します。" >&2
    exit 1
  fi
}

# インストール先にあるのが、このアプリのバンドルであることを確かめてから削除する。
# 無ければ何もしない。
remove_installed_app() {
  check_dest_parent
  if [ -L "$DEST" ]; then
    echo "$DEST はシンボリックリンクです。意図しない場所を削除しないよう中止します。" >&2
    exit 1
  fi
  if [ ! -e "$DEST" ]; then
    return 0
  fi

  # 中身が空のフォルダーは消してよい（rmdir は中身があると失敗するので、ほかのファイルは消さない）
  if rmdir "$DEST" 2>/dev/null; then
    return 0
  fi

  installed_bundle_id=$(plutil -extract CFBundleIdentifier raw "$DEST/Contents/Info.plist" 2>/dev/null || true)
  if [ -z "$installed_bundle_id" ]; then
    echo "$DEST の Info.plist を読めません（ファイルが無いか、壊れています）。$APP_NAME のアプリか分からないため、削除せずに中止します。" >&2
    echo "中身を確かめてから、Finder でゴミ箱に入れるか、rm -rf \"$DEST\" を実行して、もう一度実行してください。" >&2
    exit 1
  fi
  if [ "$installed_bundle_id" != "$BUNDLE_ID" ] && [ "$installed_bundle_id" != "$LEGACY_BUNDLE_ID" ]; then
    echo "$DEST は $APP_NAME のアプリではないようです（バンドルID: ${installed_bundle_id}）。削除せずに中止します。" >&2
    echo "別のアプリであれば、別の場所に移してから、もう一度実行してください。" >&2
    exit 1
  fi

  echo "削除します: $DEST"
  rm -rf "$DEST"
}

# 動いているアプリを終了し、終了するまで待つ（最大で約10秒）。
# 二重起動の防止は先に起動していた方を残すため、古いプロセスが残ったまま新しいアプリを開くと、新しい方がすぐ終わる
quit_running_app() {
  pkill -x "$APP_NAME" 2>/dev/null || return 0
  attempt=0
  while pgrep -x "$APP_NAME" >/dev/null 2>&1; do
    attempt=$((attempt + 1))
    if [ "$attempt" -gt 20 ]; then
      echo "$APP_NAME が終了しません。メニューバーの「終了」から終了してから、もう一度実行してください。" >&2
      exit 1
    fi
    sleep 0.5
  done
}
