#!/usr/bin/env bash
# CI の「コーディング規約」ジョブと同じ検査を、手元でも1つのコマンドで実行する。
#
# 使い方: .github/lint/lint.sh
#   入っていないツールの検査は「スキップ」と表示して続ける。REQUIRE_ALL=1 では、スキップも失敗にする（CI 用）。
#   PowerShell の検査（PSScriptAnalyzer）と Swift の書式（swift format）は、準備が OS に依存するため含めない
#   （CI ではそれぞれのジョブで実行する。手元での実行方法は CONTRIBUTING の「書式の確認」）。
set -uo pipefail

cd "$(dirname "$0")/../.."

failures=()
skips=()

# 名前、必要なコマンド、実行する内容（bash -c に渡す）
run_check() {
  local name=$1 tool=$2 script=$3
  if ! command -v "$tool" >/dev/null; then
    echo "スキップ: $name（$tool が見つかりません）"
    skips+=("$name")
    return
  fi
  echo "== $name"
  if ! bash -euo pipefail -c "$script"; then
    failures+=("$name")
  fi
}

# Python のコマンド名。Windows では python3 が無く python だけのことがある
python=python3
command -v python3 >/dev/null || python=python

run_check "Rust（rustfmt・Clippy）" clippy-driver "make -C converters lint-rust"
run_check "CI 用のスクリプトのテスト" "$python" "$python -m unittest discover -s .github/scripts/tests"
run_check "Python（ruff）" ruff "ruff check converters .github/scripts && ruff format --check --diff converters .github/scripts"
run_check "Python（mypy）" mypy "mypy --strict --python-version 3.10 converters/punctuation_converter.py converters/tests/*.py .github/scripts/*.py .github/scripts/tests/*.py"
run_check "シェルスクリプト（ShellCheck）" shellcheck "git ls-files -z '*.sh' | xargs -0 shellcheck"
run_check "Markdown のリンク" "$python" "$python .github/scripts/check_markdown_links.py"
run_check "通信する API が無いことの確認" git ".github/scripts/check_no_network.sh"
run_check "ワークフロー（actionlint）" actionlint "actionlint"
run_check "設定ファイルのスキーマ（check-jsonschema）" check-jsonschema "
  check-jsonschema --builtin-schema vendor.dependabot .github/dependabot.yml
  check-jsonschema --builtin-schema vendor.github-issue-forms .github/ISSUE_TEMPLATE/bug_report.yml .github/ISSUE_TEMPLATE/feature_request.yml
  check-jsonschema --builtin-schema vendor.github-issue-config .github/ISSUE_TEMPLATE/config.yml"
run_check "C#（dotnet format）" dotnet "
  dotnet format Windows/PunctuationToggle.sln --verify-no-changes
  dotnet format converters/dotnet/PunctuationConverter.csproj --verify-no-changes"

echo
echo "失敗: ${#failures[@]} 件${failures[*]:+（${failures[*]}）}、スキップ: ${#skips[@]} 件${skips[*]:+（${skips[*]}）}"
if [[ ${#failures[@]} -gt 0 || ( ${REQUIRE_ALL:-0} == 1 && ${#skips[@]} -gt 0 ) ]]; then
  exit 1
fi
