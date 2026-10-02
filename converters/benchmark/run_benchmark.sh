#!/usr/bin/env bash
# ビルド済みの各言語の実装の実行時間を hyperfine で計測する。
#
# 使い方: benchmark/run_benchmark.sh [入力ファイル] [hyperfine のオプション...]
#   入力ファイルの既定値は benchmark/repeated_text.txt（無ければ作る）。
#   例: benchmark/run_benchmark.sh benchmark/wikipedia_programming.txt --warmup 5 --runs 20
#
# 結果は Markdown の表として benchmark/results/ に保存する。
set -euo pipefail

cd "$(dirname "$0")/.."

input_file=${1:-benchmark/repeated_text.txt}
shift || true
hyperfine_options=("$@")
if [[ ${#hyperfine_options[@]} -eq 0 ]]; then
  hyperfine_options=(--warmup 2 --runs 10)
fi

if ! command -v hyperfine >/dev/null; then
  echo "hyperfine が見つかりません（https://github.com/sharkdp/hyperfine）。" >&2
  exit 1
fi
if [[ $input_file == benchmark/repeated_text.txt && ! -f $input_file ]]; then
  benchmark/generate_repeated_text.sh
fi

# 「名前|実行に必要なファイル|コマンド」の組。ビルドされていないものは除く。
candidates=(
  "C++|bin/punctuation_converter_cpp|bin/punctuation_converter_cpp"
  "C# (.NET Native AOT)|bin/dotnet/PunctuationConverter|bin/dotnet/PunctuationConverter"
  "C# (Mono)|bin/PunctuationConverter.exe|mono bin/PunctuationConverter.exe"
  "Go|bin/punctuation_converter_go|bin/punctuation_converter_go"
  "Java|bin/PunctuationConverter.class|java -cp bin PunctuationConverter"
  "Python|punctuation_converter.py|python3 punctuation_converter.py"
  "Rust|bin/punctuation_converter_rust|bin/punctuation_converter_rust"
  "TypeScript (Node.js)|bin/punctuation_converter.js|node bin/punctuation_converter.js"
)

arguments=()
for candidate in "${candidates[@]}"; do
  IFS='|' read -r name required_file command <<< "$candidate"
  if [[ -e $required_file ]] && command -v "${command%% *}" >/dev/null; then
    arguments+=(--command-name "$name" "$command < $input_file")
  else
    echo "スキップ: ${name}（ビルドされていないか、実行環境がありません）" >&2
  fi
done

mkdir -p benchmark/results
result_file="benchmark/results/$(basename "$input_file" .txt).md"
hyperfine "${hyperfine_options[@]}" --sort mean-time --export-markdown "$result_file" "${arguments[@]}"
echo "結果を保存しました: $result_file"
