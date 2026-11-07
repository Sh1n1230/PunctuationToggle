#!/usr/bin/env bash
# アプリ本体と変換コマンドのソースに、ネットワークを使う API が無いことを確かめる（docs/THREAT_MODEL.md）。
# 見つかったら、その行を表示して終了コード 1 で終わる。CI の「コーディング規約」ジョブが実行する。
#
# このパターンで見つからないことは「通信しない」ことの証明にはならない（P/Invoke や動的な読み込みなどは
# 見つけられない）。配布物と動いているアプリでの確認（THREAT_MODEL.md）と組み合わせて使う。
set -euo pipefail

cd "$(dirname "$0")/../.."

# macOS: URLSession・NSURLConnection・URLRequest・Network.framework（NWConnection・nw_connection_*）・CFNetwork・
#        URL を渡すと通信しうる Data(contentsOf:)・String(contentsOf:)
# Windows: System.Net・HttpClient・WebClient・WebRequest・WinHTTP・WinINet（InternetOpen など）
# 共通: ソケット。変換コマンド: Python の urllib・http.client・socket、Rust の std::net
pattern='URLSession|NSURLConnection|URLRequest|NWConnection|nw_connection|CFReadStreamCreateForHTTPRequest|CFSocket|contentsOf:|Socket|socket|HttpClient|WebClient|WebRequest|System\.Net|WinHttp|InternetOpen|urllib|http\.client|std::net'

targets=(macOS/PunctuationToggle Windows/PunctuationToggle Windows/PunctuationToggle.Core converters)

if matches=$(git grep -nE "$pattern" -- "${targets[@]}" ':!*.md' ':!converters/tests/' ':!converters/benchmark/'); then
  echo "ネットワークを使う API が見つかりました。アプリと変換コマンドは通信しない約束です（SECURITY.md）:" >&2
  echo "$matches" >&2
  exit 1
fi
echo "ネットワークを使う API は見つかりませんでした"
