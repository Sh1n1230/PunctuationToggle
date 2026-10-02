#!/usr/bin/env bash
# ベンチマーク用に、「、。」を繰り返した1行のテキストを作る。
#
# 使い方: benchmark/generate_repeated_text.sh [繰り返し回数] [出力ファイル]
#   繰り返し回数の既定値は 5000000（約30MB）、出力ファイルの既定値は benchmark/repeated_text.txt
set -euo pipefail

repeat_count=${1:-5000000}
output_file=${2:-"$(dirname "$0")/repeated_text.txt"}

if ! [[ $repeat_count =~ ^[0-9]+$ ]]; then
  echo "繰り返し回数には整数を指定してください: $repeat_count" >&2
  exit 2
fi

python3 -c 'import sys; print("、。" * int(sys.argv[1]))' "$repeat_count" > "$output_file"
echo "作成しました: $output_file"
