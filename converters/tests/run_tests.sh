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
# 3つの実装が --help で書き出す使い方（不正な引数のときは標準エラー出力にも書く）
USAGE_FILE=tests/usage.txt
# --version で表示する版。アプリと同じ番号にする
EXPECTED_VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' ../Windows/Directory.Build.props)
# UTF-8 として正しくない入力のときに、3つの実装が標準エラー出力に書くメッセージ
INVALID_UTF8_MESSAGE="入力を UTF-8 として読み込めませんでした。"
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT

# Python のコマンド名。Windows では python3 が無く python だけのことがある
python_command() {
  if command -v python3 >/dev/null; then
    echo python3
  elif command -v python >/dev/null; then
    echo python
  else
    return 1
  fi
}

# 実装名から実行コマンドを求める。実行できない場合は失敗を返す。
# Windows（Git Bash）でも、.exe を省いたパスで実行ファイルを確かめ、実行できる
command_for() {
  case "$1" in
    csharp-dotnet) [[ -x bin/dotnet/PunctuationConverter ]] && echo "bin/dotnet/PunctuationConverter" ;;
    python) [[ -f punctuation_converter.py ]] && python_name=$(python_command) && echo "$python_name punctuation_converter.py" ;;
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

# 固定のケースが、確かめたい性質を本当に含んでいるかを確かめる（エディタの保存などで崩れていないか）
check_case_preconditions() {
  # バイトとして扱う（macOS の tr は、UTF-8 の文字の途中のバイトを渡すと Illegal byte sequence で失敗する）
  local LC_ALL=C
  local cases=$CASES_DIR
  local failures_before=$failures
  # grep は Windows の Git Bash で行末の CR を取り除いて比べるため、バイトを数えて確かめる
  local crlf_count
  crlf_count=$(od -An -v -tx1 "$cases/crlf/input.txt" | tr -d ' \n' | { grep -o '0d0a' || true; } | wc -l)
  [[ $crlf_count -gt 0 ]] || report_failure "crlf/input.txt に CRLF がありません"
  if [[ $(tr -cd '\r' < "$cases/cr_only/input.txt" | wc -c) -eq 0 || $(tr -cd '\n' < "$cases/cr_only/input.txt" | wc -c) -ne 0 ]]; then
    report_failure "cr_only/input.txt に CR が無いか、LF が含まれています"
  fi
  [[ $(head -c 3 "$cases/bom/input.txt" | od -An -tx1 | tr -d ' \n') == efbbbf ]] || report_failure "bom/input.txt が BOM で始まっていません"
  [[ -n $(tail -c 1 "$cases/no_trailing_newline/input.txt" | tr -d '\n\r') ]] || report_failure "no_trailing_newline/input.txt の最後が改行です"
  [[ ! -s $cases/empty/input.txt ]] || report_failure "empty/input.txt が空ではありません"
  if [[ $failures -eq $failures_before ]]; then
    echo "PASS  テストケースの前提"
  fi
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
      elif [[ -s $errors ]]; then
        report_failure "$case_name ${option:-（オプションなし）}: 成功したのに標準エラー出力に書いています: $(head -c 200 "$errors")"
      fi
    done
  done

  # 使い方の文章は3つの実装にそれぞれ書いてあるため、tests/usage.txt とバイト単位で比べて食い違いを防ぐ
  for option in "--help" "-h"; do
    # shellcheck disable=SC2086
    if ! $command $option < /dev/null > "$output" 2> "$errors"; then
      report_failure "$option: 異常終了しました: $(head -c 200 "$errors")"
    elif ! cmp -s "$output" "$USAGE_FILE"; then
      report_failure "$option: 使い方が tests/usage.txt と異なります"
    elif [[ -s $errors ]]; then
      report_failure "$option: 成功したのに標準エラー出力に書いています: $(head -c 200 "$errors")"
    fi
  done

  # --version は、リポジトリの版（Windows/Directory.Build.props の Version）を1行で書く
  # shellcheck disable=SC2086
  if ! $command --version < /dev/null > "$output" 2> "$errors"; then
    report_failure "--version: 異常終了しました: $(head -c 200 "$errors")"
  elif [[ $(cat "$output") != "punctuation_converter $EXPECTED_VERSION" || -s $errors ]]; then
    report_failure "--version: 「punctuation_converter $EXPECTED_VERSION」を期待しましたが「$(head -c 200 "$output")」でした"
  fi

  # 不正な引数では、標準エラー出力に「不正な引数です: <引数>」・空行・使い方を書く
  local expected_errors="$WORK_DIR/expected_errors"
  { printf '不正な引数です: --unknown\n\n'; cat "$USAGE_FILE"; } > "$expected_errors"
  # shellcheck disable=SC2086
  $command --unknown < /dev/null > "$output" 2> "$errors" || true
  if ! cmp -s "$errors" "$expected_errors"; then
    report_failure "不正な引数のエラー出力が「不正な引数です: --unknown」と tests/usage.txt の使い方になっていません"
  fi

  # UTF-8 として正しくない入力（不正なバイト、末尾で途中までしかない文字、先頭バイトの無い継続バイト、
  # 冗長な符号化、サロゲートの符号化、U+10FFFF を超える符号位置）は、置き換えずに終了コード 1 にする
  local invalid_input="$WORK_DIR/invalid_input"
  for invalid_bytes in 'a\xff\xe3\x80\x81\n' 'a\xe3\x80' '\x80' '\xc0\xaf' '\xed\xa0\x80' '\xf4\x90\x80\x80'; do
    # shellcheck disable=SC2059 # 不正なバイト列を作るため、printf の書式として解釈させる
    printf "$invalid_bytes" > "$invalid_input"
    # 変換でも逆変換でも同じように扱う
    for option in "" "--reverse"; do
      local status=0
      # shellcheck disable=SC2086
      $command $option < "$invalid_input" > "$output" 2> "$errors" || status=$?
      if [[ $status -ne 1 ]] || [[ $(cat "$errors") != "$INVALID_UTF8_MESSAGE" ]]; then
        report_failure "不正な UTF-8 '$invalid_bytes' ${option:-（オプションなし）}: 終了コード 1 と「$INVALID_UTF8_MESSAGE」を期待しましたが、終了コードは $status、エラー出力は「$(head -c 200 "$errors")」でした"
      fi
    done
  done

  # 書き込めない出力先では、トレースバックなどではなくエラーメッセージを表示して終了コード 1 にする。
  # 書き込むと必ず失敗する /dev/full は Linux にしか無いため、ほかの OS では確かめない
  # （Windows の Git Bash の /dev/full は、Windows のプログラムからは書き込みに成功する）
  if [[ $(uname -s) == Linux && -w /dev/full ]]; then
    local status=0
    # shellcheck disable=SC2086
    $command < "$WORK_DIR/cases/long_line/input.txt" > /dev/full 2> "$errors" || status=$?
    if [[ $status -ne 1 ]] || ! grep -q "^変換に失敗しました: " "$errors"; then
      report_failure "書き込めない出力先: 終了コード 1 とエラーメッセージを期待しましたが、終了コードは $status でした: $(head -c 200 "$errors")"
    fi

    # 使い方を書き出せない場合も、トレースバックや panic のメッセージを出さずに、
    # --help は終了コード 1、不正な引数は（エラーを書き出せなくても）終了コード 2 にする
    status=0
    # shellcheck disable=SC2086
    $command --help < /dev/null > /dev/full 2> "$errors" || status=$?
    if [[ $status -ne 1 ]] || grep -qE "Traceback|Exception|panicked" "$errors"; then
      report_failure "使い方を書き出せない: 終了コード 1 を期待しましたが、終了コードは $status でした: $(head -c 200 "$errors")"
    fi
    status=0
    # shellcheck disable=SC2086
    $command --unknown < /dev/null > "$output" 2> /dev/full || status=$?
    if [[ $status -ne 2 ]]; then
      report_failure "不正な引数のエラーを書き出せない: 終了コード 2 を期待しましたが、終了コードは $status でした"
    fi
  fi

  # 出力の読み手が先に終了した（`| head` など）ときは、何も表示せずに終了コード 0 で終える
  # （converters/README.md の「使い方」）。Windows の Git Bash では、読み手の終了が書き手に伝わる時機が違うため確かめない
  if [[ $(uname -s) != MINGW* && $(uname -s) != MSYS* && $(uname -s) != CYGWIN* ]]; then
    local statuses
    # shellcheck disable=SC2086
    statuses=$($command < "$WORK_DIR/cases/long_line/input.txt" 2> "$errors" | head -c 1 > /dev/null; echo "${PIPESTATUS[0]}")
    if [[ $statuses -ne 0 || -s $errors ]]; then
      report_failure "出力の読み手が先に終了: 終了コード 0 と空の標準エラー出力を期待しましたが、終了コードは $statuses でした: $(head -c 200 "$errors")"
    fi
  fi

  # 標準入出力が閉じている（シェルの >&- など）ときは、閉じた出力先には書けたものとして、閉じた入力は空として
  # 扱い、トレースバックなどを出さない（Rust の標準ライブラリの動作に合わせる）。Windows では確かめない
  if [[ $(uname -s) != MINGW* && $(uname -s) != MSYS* && $(uname -s) != CYGWIN* ]]; then
    local closed_case closed_status
    for closed_case in "convert:0" "closed_input:0" "help:0" "bad_argument:2"; do
      closed_status=0
      # shellcheck disable=SC2086
      case ${closed_case%%:*} in
        convert) $command < "$CASES_DIR/basic/input.txt" >&- 2> "$errors" || closed_status=$? ;;
        closed_input) $command <&- > "$output" 2> "$errors" || closed_status=$? ;;
        help) $command --help < /dev/null >&- 2> "$errors" || closed_status=$? ;;
        bad_argument) : > "$errors"; $command --bad < /dev/null > "$output" 2>&- || closed_status=$? ;;
      esac
      if [[ $closed_status -ne ${closed_case##*:} || -s $errors ]]; then
        report_failure "閉じた標準入出力（${closed_case%%:*}）: 終了コード ${closed_case##*:} を期待しましたが、終了コードは $closed_status でした: $(head -c 200 "$errors")"
      fi
    done
  fi

  # オプションは1つだけ受け付け、大文字小文字の違いや "--"・"-" も不正な引数とする。
  # UTF-8 でない引数（macOS や Linux で渡せる）も、異常終了させずに不正な引数として扱う
  for arguments in "--unknown" "-r -r" "extra" "-h -r" "--reverse --help" "-R" "-V" "--version -r" "--" "-" $'\xff'; do
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
check_case_preconditions

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
    # Ctrl+C のテストは Python で動かすため、Python が無い環境では省く
    if python_name=$(python_command) && ! "$python_name" tests/test_sigint.py "$command"; then
      report_failure "$name Ctrl+C: メッセージなどを表示せずに終了しませんでした"
    fi
    # Python 実装は、main をプロセスを起動せずに確かめる単体テストも実行する
    if [[ $name == python ]] && ! "$(python_command)" tests/test_punctuation_converter.py; then
      report_failure "python の単体テスト（tests/test_punctuation_converter.py）が失敗しました"
    fi
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
