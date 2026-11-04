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

  installed_bundle_id=$(plutil -extract CFBundleIdentifier raw "$DEST/Contents/Info.plist" 2>/dev/null || true)
  if [ "$installed_bundle_id" != "$BUNDLE_ID" ] && [ "$installed_bundle_id" != "$LEGACY_BUNDLE_ID" ]; then
    echo "$DEST は $APP_NAME のアプリではないようです（バンドルID: ${installed_bundle_id:-不明}）。削除せずに中止します。" >&2
    exit 1
  fi

  echo "削除します: $DEST"
  rm -rf "$DEST"
}
