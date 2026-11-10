#!/usr/bin/env bash
# シェルスクリプトで、波かっこの無い変数（$name の形）のすぐ後ろに、日本語などの ASCII でない文字を書いていないことを確かめる。
# 見つかったら、その行を表示して終了コード 1 で終わる。CI の「コーディング規約」ジョブが実行する。
#
# macOS に付属の bash 3.2（/bin/sh と /bin/bash）は、UTF-8 のロケールで、変数名のすぐ後ろのバイトが ASCII でないと、
# そのバイトまでを変数名として読む。name ではない変数を読むことになり、set -u では「unbound variable」で止まり、
# set -u が無ければ値が消えて文字化けする。${name} の形で書けば起きない。
# CI の Linux の bash では起きないので、書き方で確かめる（コメントの中も同じ書き方にする）。
set -euo pipefail

cd "$(dirname "$0")/../.."

# ASCII でないバイト（UTF-8 の文字はすべて 0x80 以上のバイトで始まる）。バイトとして比べるので LC_ALL=C で探す
non_ascii_byte_range=$'\x80-\xff'
pattern='\$[A-Za-z_][A-Za-z0-9_]*['"$non_ascii_byte_range"']'

if matches=$(LC_ALL=C git grep -nE "$pattern" -- '*.sh'); then
  echo "変数のすぐ後ろに ASCII でない文字があります。macOS に付属の bash で変数を読めないので、\${name} の形で書いてください:" >&2
  echo "$matches" >&2
  exit 1
fi
echo "変数のすぐ後ろに ASCII でない文字を書いているシェルスクリプトはありませんでした"
