#!/usr/bin/env bash
# ビルド済みの各言語の実装の実行時間を hyperfine で計測する。
#
# 使い方: benchmark/run_benchmark.sh [入力ファイル] [hyperfine のオプション...]
#   入力ファイルの既定値は benchmark/repeated_text.txt（無ければ作る）。
#   例: benchmark/run_benchmark.sh benchmark/wikipedia_programming.txt --warmup 5 --runs 20
#
# 結果は Markdown の表と JSON（比較用）として benchmark/results/ に保存する。
# Python のコマンドは PYTHON 環境変数、python3、python の順に探す（Windows では python だけのことがある）。
set -euo pipefail

# 入力ファイルは、converters/ に移る前に、呼び出したときの作業ディレクトリーからの絶対パスにする
input_file=${1:-}
shift || true
if [[ -n $input_file && -f $input_file ]]; then
  input_file="$(cd "$(dirname "$input_file")" && pwd)/$(basename "$input_file")"
fi

cd "$(dirname "$0")/.."
input_file=${input_file:-benchmark/repeated_text.txt}
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
if [[ ! -f $input_file ]]; then
  echo "入力ファイルが見つかりません: $input_file" >&2
  exit 1
fi

python_command=${PYTHON:-}
if [[ -z $python_command ]]; then
  if command -v python3 >/dev/null; then
    python_command=python3
  else
    python_command=python
  fi
fi

# 「名前|実行に必要なファイル|コマンド」の組。ビルドされていないものは除く。
candidates=(
  "C# (.NET Native AOT)|bin/dotnet/PunctuationConverter|bin/dotnet/PunctuationConverter"
  "Python|punctuation_converter.py|$python_command punctuation_converter.py"
  "Rust|bin/punctuation_converter_rust|bin/punctuation_converter_rust"
)

arguments=()
for candidate in "${candidates[@]}"; do
  IFS='|' read -r name required_file command <<< "$candidate"
  if [[ -e $required_file ]] && command -v "${command%% *}" >/dev/null; then
    arguments+=(--command-name "$name" "$command")
  else
    echo "スキップ: ${name}（ビルドされていないか、実行環境がありません）" >&2
  fi
done

if [[ ${#arguments[@]} -eq 0 ]]; then
  echo "計測できる実装がありません。先に make（Rust）や make csharp-dotnet でビルドしてください。" >&2
  exit 1
fi

mkdir -p benchmark/results
# 入力の拡張子は .txt に限らないので最後のドット以降を取り除く。
# 同名の結果が既にあるときは黙って上書きせず、日時を付けた名前に退避する。
result_name=$(basename "$input_file")
result_name=${result_name%.*}
if [[ -e benchmark/results/${result_name}.md ]]; then
  result_name="${result_name}-$(date +%Y%m%d-%H%M%S)"
fi
result_file="benchmark/results/${result_name}.md"
# -N（--shell=none）: シェルを介さずに起動し、シェルの起動時間を差し引く補正をしない。
# hyperfine 1.x と 2.x、Windows（シェルが cmd.exe になる）で計測の方法がそろう
hyperfine -N "${hyperfine_options[@]}" --input "$input_file" --sort mean-time \
  --export-markdown "$result_file" --export-json "benchmark/results/${result_name}.json" "${arguments[@]}"
echo "結果を保存しました: ${result_file}（JSON: benchmark/results/${result_name}.json）"
