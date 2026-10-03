#!/usr/bin/env bash
# 各言語の実装に同じ入力を与え、出力がバイト単位で期待どおりになるかを確認する。
#
# 使い方: tests/run_tests.sh [実装名...]
#   実装名を省略するとすべての実装をテストする。ビルドされていない実装は SKIP になる。
#   REQUIRE_ALL=1 を指定すると、SKIP があった場合も失敗として扱う（CI 用）。
#
# 事前に `make all csharp-dotnet` でビルドしておくこと。
set -euo pipefail

cd "$(dirname "$0")/.."

ALL_IMPLEMENTATIONS=(csharp-dotnet python rust)
CASES_DIR=tests/cases
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT

# 実装名から実行コマンドを求める。実行できない場合は失敗を返す。
command_for() {
  case "$1" in
    csharp-dotnet) [[ -x bin/dotnet/PunctuationConverter ]] && echo "bin/dotnet/PunctuationConverter" ;;
    python) [[ -f punctuation_converter.py ]] && command -v python3 >/dev/null && echo "python3 punctuation_converter.py" ;;
    rust) [[ -x bin/punctuation_converter_rust ]] && echo "bin/punctuation_converter_rust" ;;
    *) return 1 ;;
  esac
}

# 長い1行（読み込みの区切りをまたぐ）のテストデータを作る
create_long_line_case() {
  local case_dir="$WORK_DIR/cases/long_line"
  mkdir -p "$case_dir"
  local repeat_count=100000
  awk -v count="$repeat_count" 'BEGIN { for (i = 0; i < count; i++) printf "%s", "😀、𠮷。a，b．" }' > "$case_dir/input.txt"
  awk -v count="$repeat_count" 'BEGIN { for (i = 0; i < count; i++) printf "%s", "😀，𠮷．a，b．" }' > "$case_dir/to_comma_period.txt"
  awk -v count="$repeat_count" 'BEGIN { for (i = 0; i < count; i++) printf "%s", "😀、𠮷。a、b。" }' > "$case_dir/to_touten_kuten.txt"
}

failures=0
skips=0

report_failure() {
  echo "  FAIL: $1"
  failures=$((failures + 1))
}

# 1つの実装をすべてのケースでテストする
test_implementation() {
  local name=$1
  local command=$2
  local failures_before=$failures
  local output="$WORK_DIR/output"
  local errors="$WORK_DIR/errors"

  for case_dir in "$CASES_DIR"/*/ "$WORK_DIR"/cases/*/; do
    local case_name
    case_name=$(basename "$case_dir")
    for option in "" "--reverse" "-r"; do
      local expected="$case_dir/to_comma_period.txt"
      [[ -n $option ]] && expected="$case_dir/to_touten_kuten.txt"

      # shellcheck disable=SC2086 # command と option は単語分割させる
      if ! $command $option < "$case_dir/input.txt" > "$output" 2> "$errors"; then
        report_failure "$case_name ${option:-（オプションなし）}: 異常終了しました: $(head -c 200 "$errors")"
      elif ! cmp -s "$output" "$expected"; then
        report_failure "$case_name ${option:-（オプションなし）}: 出力が期待と異なります"
      fi
    done
  done

  for option in "--help" "-h"; do
    # shellcheck disable=SC2086
    if ! $command $option < /dev/null > "$output" 2> "$errors" || ! grep -q "使い方" "$output"; then
      report_failure "$option: 使い方が表示されません"
    fi
  done

  for arguments in "--unknown" "-r -r" "extra"; do
    local status=0
    # shellcheck disable=SC2086
    $command $arguments < /dev/null > "$output" 2> "$errors" || status=$?
    if [[ $status -ne 2 || -s $output || ! -s $errors ]]; then
      report_failure "不正な引数 '$arguments': 終了コード 2 とエラー出力を期待しましたが、終了コードは $status でした"
    fi
  done

  if [[ $failures -eq $failures_before ]]; then
    echo "PASS  $name"
  else
    echo "FAIL  $name"
  fi
}

create_long_line_case

implementations=("$@")
if [[ ${#implementations[@]} -eq 0 ]]; then
  implementations=("${ALL_IMPLEMENTATIONS[@]}")
fi

for name in "${implementations[@]}"; do
  if [[ " ${ALL_IMPLEMENTATIONS[*]} " != *" $name "* ]]; then
    echo "不明な実装です: ${name}（${ALL_IMPLEMENTATIONS[*]} のいずれかを指定してください）" >&2
    exit 2
  fi
done

for name in "${implementations[@]}"; do
  if command=$(command_for "$name"); then
    test_implementation "$name" "$command"
  else
    echo "SKIP  ${name}（ビルドされていないか、実行環境がありません）"
    skips=$((skips + 1))
  fi
done

echo
echo "失敗: $failures 件、スキップ: $skips 件"
if [[ $failures -gt 0 || ( ${REQUIRE_ALL:-0} == 1 && $skips -gt 0 ) ]]; then
  exit 1
fi
