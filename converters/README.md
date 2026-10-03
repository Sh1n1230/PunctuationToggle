# 句読点の変換コマンド

すでに書いた文章の句読点を **「、。」⇔「，．」** にまとめて変換するコマンドです。同じ仕様を Rust・C#・Python の3言語で実装しています。言語は [hyperfine](https://github.com/sharkdp/hyperfine) での速度の比較をもとに絞りました（[実装する言語の選定](#実装する言語の選定)）。

## 使い方

標準入力から文章を読み、変換結果を標準出力に書き出します。どの言語の実装も同じように使えます。

```sh
python3 punctuation_converter.py < 原稿.txt > 提出用.txt            # 「、」→「，」、「。」→「．」
python3 punctuation_converter.py --reverse < 提出用.txt > 原稿.txt  # 「，」→「、」、「．」→「。」
```

| オプション | 動作 |
|---|---|
| （なし） | 「、」を「，」に、「。」を「．」に変換する |
| `-r`, `--reverse` | 「，」を「、」に、「．」を「。」に変換する |
| `-h`, `--help` | 使い方を表示する |

- 句読点以外は1バイトも変えません。改行コード（LF / CRLF）、最後の行の改行の有無、BOM、絵文字などはそのまま残ります。
- ASCII の `,` `.` や半角の `､` `｡` は変換しません。
- 入力は UTF-8 にしてください（UTF-8 として正しくないバイト列の扱いは実装によって異なります）。
- 終了コードは、成功が `0`、読み書きの失敗が `1`、不正な引数が `2` です。

## 実装の一覧

| 言語 | ファイル | ビルド | 実行コマンド |
|---|---|---|---|
| C#（.NET Native AOT） | `PunctuationConverter.cs` | `make csharp-dotnet` | `bin/dotnet/PunctuationConverter` |
| Python | `punctuation_converter.py` | 不要 | `python3 punctuation_converter.py` |
| Rust | `punctuation_converter.rs` | `make rust` | `bin/punctuation_converter_rust` |

読み込みの方法は言語ごとに、その言語で自然な書き方を選んでいます。

- 1行ずつ（改行を含めて）読む: Rust
- 一定の文字数ずつ読む: C#、Python（置換する文字はどれも1文字なので、どこで区切っても変換結果は同じ）

### 必要な実装の選定と不要ファイルの整理

提供している3種類の実装はそれぞれ独立して動作し、相互の依存関係を持ちません。利用目的に応じていずれか1つの実装を選択すれば足りるため、ソースコードを取得した後は利用しない言語の実装ファイルを削除して差し支えありません。例えば Rust 実装を用いる場合は `punctuation_converter.rs` のみを残し、`punctuation_converter.py`、`PunctuationConverter.cs`、および `dotnet/` を削除できます。Python 実装を用いる場合は `punctuation_converter.py` 単体で動作します。なお、`make test` によるテストスクリプトは、削除された実装を自動的にスキップ（SKIP）して残余の実装のみを検証します。

## 必要なツール

| 対象 | ツール |
|---|---|
| C#（.NET Native AOT） | [.NET 10 SDK](https://dotnet.microsoft.com/download) 以降と、Native AOT 用のツール（macOS は Xcode Command Line Tools、Linux は clang と zlib、Windows は Visual Studio の「C++ によるデスクトップ開発」） |
| Python | Python 3.9 以降 |
| Rust | Rust（`rustc`） |
| ベンチマーク | [hyperfine](https://github.com/sharkdp/hyperfine)（macOS は `brew install hyperfine`、Windows は `winget install sharkdp.hyperfine`） |

## ビルドとテスト

このディレクトリで実行します。

```sh
make                      # Rust をビルド（bin/ に出力）
make csharp-dotnet        # C#（.NET Native AOT）をビルド（.NET SDK が無い環境でも make が通るよう、別にしている）
make test                 # すべての実装をテストする
```

`make test`（`tests/run_tests.sh`）は、`tests/cases/` の入力をそれぞれの実装に与え、出力が期待どおりかをバイト単位で比べます。CRLF の改行、最後の行に改行が無い文章、空の入力、BOM や絵文字、読み込みの区切りをまたぐ長い1行などを確かめています。ビルドされていない実装は SKIP になります。特定の実装だけを確かめるときは `tests/run_tests.sh rust python` のように指定します。

`make clean` で生成物を削除できます。

## ベンチマーク

```sh
make benchmark                                                                # 合成データ（約30MB）で計測
benchmark/run_benchmark.sh benchmark/wikipedia_programming.txt --warmup 5 --runs 30  # 実際の文章で計測
```

ビルド済みの実装をすべて hyperfine で計測し、結果を `benchmark/results/` に Markdown の表で保存します。合成データ（「、。」を500万回繰り返した1行）は、初回に `benchmark/generate_repeated_text.sh` で作ります。

## 実装する言語の選定

もとは C++・C#（Mono / .NET Native AOT）・Go・Java・Python・Rust・TypeScript の8実装（7言語）を作り、同じテスト（`make test`）で全実装の出力がバイト単位で一致することを確かめたうえで、速度を比べました。変換は1文字ずつの置換なので、どの言語でも同じ結果になります。違いが出るのは速度と、使うまでの手間だけです。そのため次の基準で、役割が重なる実装を減らしました。

1. 計測結果が、残す実装より同じか劣る実装は、残す理由がない。
2. 同じ速さの実装が複数あるときは、ほかの言語にない利点があるものを残す。
3. 同じソースのビルド方法の違いは、速い方だけ残す。

計測は性能特性の異なる2つの環境（macOS の Apple M2、および Windows の Ryzen 7 9700X）で実施し、双方の環境で一貫して成り立つ結果のみを採択・削除の根拠としました。測定環境によって順位が前後する僅差の項目（実文章における Rust・Go・C++ の差など）は、1ミリ秒未満の差異であり実用上の体感差を生じないため、選定判断からは除外しています。

### 計測結果（選定時の参考値）

置換対象文字が1,000万個存在する約30MBの合成データでは文字列走査と置換自体の処理性能が、約40KBの実文章ではランタイム読み込みやJITコンパイルに伴うプロセスの起動時間が大半を占めます。

#### 合成データ: 「、。」を500万回繰り返した1行（約30MB）

| 言語 | macOS [ms] | Rust との比 | Windows [ms] | Rust との比 | 判断 |
|:---|---:|---:|---:|---:|:---|
| Rust | 51.6 ± 0.4 | 1.00 | 55.2 ± 0.6 | 1.00 | 残す |
| C#（.NET Native AOT） | 52.0 ± 0.3 | 1.01 | 54.3 ± 3.9 | 0.98 | 残す |
| Python | 52.9 ± 0.5 | 1.02 | 68.2 ± 1.8 | 1.24 | 残す |
| Java | 115.2 ± 0.6 | 2.23 | 154.6 ± 1.1 | 2.80 | 削除 |
| Go | 184.3 ± 0.8 | 3.57 | 136.4 ± 2.7 | 2.47 | 削除 |
| C++ | 185.7 ± 0.5 | 3.59 | 124.7 ± 1.5 | 2.26 | 削除 |
| C#（Mono） | 217.4 ± 6.9 | 4.21 | 135.2 ± 3.6 | 2.45 | 削除 |
| TypeScript（Node.js） | 493.8 ± 1.1 | 9.56 | 409.8 ± 1.2 | 7.42 | 削除 |

#### 実際の文章: Wikipedia「プログラミング」の記事（約40KB、194行）

| 言語 | macOS [ms] | Rust との比 | Windows [ms] | Rust との比 | 判断 |
|:---|---:|---:|---:|---:|:---|
| Rust | 4.7 ± 0.3 | 1.00 | 4.1 ± 0.1 | 1.00 | 残す |
| Go | 4.7 ± 0.3 | 1.01 | 5.3 ± 0.4 | 1.29 | 削除 |
| C++ | 5.1 ± 0.3 | 1.10 | 3.7 ± 0.1 | 0.90 | 削除 |
| C#（.NET Native AOT） | 6.6 ± 0.3 | 1.42 | 7.3 ± 0.4 | 1.78 | 残す |
| Python | 17.8 ± 0.4 | 3.81 | 15.4 ± 0.7 | 3.76 | 残す |
| TypeScript（Node.js） | 28.4 ± 0.5 | 6.08 | 37.1 ± 1.0 | 9.05 | 削除 |
| Java | 42.2 ± 0.7 | 9.04 | 81.2 ± 10.8 | 19.80 | 削除 |
| C#（Mono） | 85.4 ± 4.6 | 18.29 | 58.8 ± 1.1 | 14.34 | 削除 |

#### 計測した環境

| 項目 | macOS | Windows |
|---|---|---|
| 日時 | 2026-10-03 | 2026-10-04 |
| マシン | Apple M2 / macOS 26.6.1 | AMD Ryzen 7 9700X / Windows 11 Home（26200） |
| C++ | Apple clang 21.0.0（`-O3`） | MSVC 14.51（`/O2`） |
| C# | .NET SDK 10.0.401、Mono 6.12.0 | .NET SDK 10.0.401、Mono 6.12.0（32ビット版） |
| Go / Java | Go 1.27.0 / Java 25 | Go 1.27.0 / Java 26 |
| Python | Python 3.13.3 | Python 3.13.3（python.org 版） |
| Rust | Rust 1.98.1 | Rust 1.99.0 |
| TypeScript | Node.js 24.13.1 + TypeScript 7.0.2 | Node.js 24.18.0 + TypeScript 7.0.2 |
| 計測ツール | hyperfine 1.20.0（`benchmark/run_benchmark.sh`） | hyperfine 1.20.0（下記参照） |

macOS 環境では `benchmark/run_benchmark.sh` を用い、合成データは `--warmup 2 --runs 10`、実文章は `--warmup 5 --runs 30` で測定しました。Windows 環境の hyperfine は既定でコマンドを `cmd.exe` 経由で実行するため、シェルを介さず標準入力を直接渡す `hyperfine -N --input <入力ファイル> <実行ファイルの絶対パス> ...` を用いて同一試行回数で測定を行いました。なお、実文章における Rust・C++・Go・Native AOT の測定値は差異が僅小であるため、`--warmup 10 --runs 100` で再測定した精度高い値を採用しています。

> [!NOTE]
> **Windows 環境における Python の測定値について:**
> Microsoft Store 版の Python はインタプリタ起動処理のオーバーヘッドが大きく、起動だけで約 75 ms を要します（`python -c pass` の実行時間が 74.9 ms、python.org 配布版は 14.6 ms）。そのため上記の表には python.org 配布版の測定値を記載しています。Store 版における測定値は合成データが 118.6 ms（Rust 比 2.15 倍）、実文章が 75.8 ms でした。この所要時間の差はすべてプロセスの起動オーバーヘッドに起因し、文字列置換処理自体のスループットは同等です。

### 残す実装とその理由

| 言語 | 選んだ理由 |
|---|---|
| Rust | 合成データにおいて双方の環境で最速群（Native AOT と同等）であり、約 40KB の実文章でも最速群（macOS では Go と同等、Windows では C++ との差が 0.4 ms）に入る。双方のデータかつ双方の測定環境において一貫して最速群に入るのは Rust のみであり、速度測定の基準となる。外部クレートに依存せず標準ライブラリのみで完結し、`rustc` 単体でビルドできる。 |
| C#（.NET Native AOT） | 合成データでは双方の環境で Rust と同等（0.98〜1.01 倍）の性能を示し、実文章でも所要時間は 8 ms 未満（Rust の 1.4〜1.8 倍）にとどまる。Windows 版の常駐アプリケーション本体も C#（.NET）で実装しているため、同一の言語基盤および開発ツールで保守できる。 |
| Python | 実行速度では Rust に及ばないものの（合成データで 1.02〜1.24 倍、実文章で約 3.8 倍）、合成データにおいては削除対象とした他のすべての言語実装を上回る。これは C 言語で実装された `str.replace` により文字列走査が高速に行われるためである。実文章における所要時間も 18 ms 未満であり実用上の性能差は極めて小さい。事前ビルドが不要で macOS や Linux 環境に標準導入されている場合が多く、スクリプトを直接実行できるため利用者の導入負担が最も小さい。 |

### 削除した実装とその理由

| 言語 | 削除した理由 |
|---|---|
| C#（Mono） | 同一のソースコードを .NET Native AOT でもビルドしており、双方の測定環境および双方の検証データにおいて大幅に遅い結果となった（合成データで 2.5〜4.2 倍、実文章で 14〜18 倍）。実行速度の低下は言語仕様ではなく Mono ランタイムの実行オーバーヘッドに起因する。基準3により削除。 |
| Go | 合成データにおいて双方の環境で Rust の 2.5〜3.6 倍の所要時間を要した。実文章では macOS 環境で Rust と同等、Windows 環境で 1.3 倍であった。「高速なネイティブバイナリ」としての役割は Rust で満たされており、Go 固有の優位性を持たない（基準1・2）。 |
| C++ | 合成データにおいて双方の環境で Rust の 2.3〜3.6 倍の所要時間を要した。実文章の Windows 環境では Rust より 0.4 ms 高速であったが実用上の差異には至らない。また Windows（MSVC）環境において標準出力がテキストモードで動作し、改行コード LF が CRLF へ変換されてテスト検証に合致しなかった。環境ごとのコンパイラ差異も大きく、Rust に対して保持すべき固有の利点がない（基準1）。 |
| Java | 双方の測定環境において、合成データで 2.2〜2.8 倍、実文章で 9〜20 倍遅く、JVM（Java 仮想マシン）の起動処理が支配的である。Python や C# に対する優位性がない（基準1）。 |
| TypeScript | 双方の測定環境において合成データで最下位（7.4〜9.6 倍）となった。Node.js ランタイムおよびビルド環境の準備が必要であり、手軽さの観点でも Python に及ばない（基準1）。 |

削除した実装のコードは Git の履歴（v1.1.0 より前のコミット）に残っています。

## ディレクトリ構成

```
converters/
├── Makefile                      # ビルド・テスト・ベンチマーク
├── PunctuationConverter.cs
├── punctuation_converter.py
├── punctuation_converter.rs
├── dotnet/                       # .NET SDK（Native AOT）用のプロジェクト
├── tests/
│   ├── run_tests.sh              # 全実装のテスト
│   └── cases/                    # 入力と期待する出力
└── benchmark/
    ├── run_benchmark.sh          # hyperfine での計測
    ├── generate_repeated_text.sh # 合成データの作成
    ├── wikipedia_programming.txt # 実際の文章（CC BY-SA 4.0、ATTRIBUTION.md を参照）
    └── ATTRIBUTION.md
```
