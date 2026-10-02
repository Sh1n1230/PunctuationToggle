# 句読点の変換コマンド

すでに書いた文章の句読点を **「、。」⇔「，．」** にまとめて変換するコマンドです。同じ仕様を C++・C#・Go・Java・Python・Rust・TypeScript で実装し、[hyperfine](https://github.com/sharkdp/hyperfine) で速度を比較しています。

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
| C++ | `punctuation_converter.cpp` | `make cpp` | `bin/punctuation_converter_cpp` |
| C#（.NET Native AOT） | `PunctuationConverter.cs` | `make csharp-dotnet` | `bin/dotnet/PunctuationConverter` |
| C#（Mono） | `PunctuationConverter.cs` | `make csharp-mono` | `mono bin/PunctuationConverter.exe` |
| Go | `punctuation_converter.go` | `make go` | `bin/punctuation_converter_go` |
| Java | `PunctuationConverter.java` | `make java` | `java -cp bin PunctuationConverter` |
| Python | `punctuation_converter.py` | 不要 | `python3 punctuation_converter.py` |
| Rust | `punctuation_converter.rs` | `make rust` | `bin/punctuation_converter_rust` |
| TypeScript | `punctuation_converter.ts` | `make typescript` | `node bin/punctuation_converter.js` |

C# は1つのソースを、Mono と .NET SDK（`dotnet/PunctuationConverter.csproj`）の両方でビルドします。

読み込みの方法は言語ごとに、その言語で自然な書き方を選んでいます。

- 1行ずつ（改行を含めて）読む: Go、Rust
- 改行の直後で区切って一定のバイト数ずつ読む: C++
- 一定の文字数ずつ読む: C#、Java、Python、TypeScript（置換する文字はどれも1文字なので、どこで区切っても変換結果は同じ）

## 必要なツール

| 対象 | ツール |
|---|---|
| C++ | C++17 に対応したコンパイラー（clang++ / g++） |
| C#（.NET Native AOT） | [.NET 10 SDK](https://dotnet.microsoft.com/download) 以降と、Native AOT 用のツール（macOS は Xcode Command Line Tools、Linux は clang と zlib） |
| C#（Mono） | [Mono](https://www.mono-project.com/)（`mcs` / `mono`） |
| Go | Go |
| Java | JDK 17 以降 |
| Python | Python 3.9 以降 |
| Rust | Rust（`rustc`） |
| TypeScript | Node.js（`npm install` で TypeScript と型定義を入れる） |
| ベンチマーク | [hyperfine](https://github.com/sharkdp/hyperfine)（`brew install hyperfine`） |

## ビルドとテスト

このディレクトリで実行します。

```sh
npm install               # TypeScript のコンパイラーと型定義を入れる
make                      # C++・C#（Mono）・Go・Java・Rust・TypeScript をビルド（bin/ に出力）
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

### 計測結果（参考値）

- 日時: 2026-10-03
- マシン: Apple M2 / macOS 26.6.1
- コンパイラー・ランタイム: Apple clang 21.0.0、.NET SDK 10.0.401、Mono 6.12.0、Go 1.27.0、Java 25、Python 3.13.3、Rust 1.98.1、Node.js 24.13.1 + TypeScript 7.0.2、hyperfine 1.20.0

#### 合成データ: 「、。」を500万回繰り返した1行（約30MB）

`benchmark/run_benchmark.sh benchmark/repeated_text.txt --warmup 2 --runs 10`

| 言語 | 平均 [ms] | 最小 [ms] | 最大 [ms] | Rust との比 |
|:---|---:|---:|---:|---:|
| Rust | 51.6 ± 0.4 | 51.3 | 52.5 | 1.00 |
| C#（.NET Native AOT） | 52.0 ± 0.3 | 51.7 | 52.4 | 1.01 |
| Python | 52.9 ± 0.5 | 52.2 | 53.9 | 1.02 |
| Java | 115.2 ± 0.6 | 114.2 | 116.4 | 2.23 |
| Go | 184.3 ± 0.8 | 183.3 | 186.2 | 3.57 |
| C++ | 185.7 ± 0.5 | 184.7 | 186.5 | 3.59 |
| C#（Mono） | 217.4 ± 6.9 | 207.4 | 230.3 | 4.21 |
| TypeScript（Node.js） | 493.8 ± 1.1 | 492.3 | 495.4 | 9.56 |

置換する文字が1,000万個あるため、置換そのものの速さが差になります。1文字ずつ見て置き換える実装（Rust、C#、Java）や、C で書かれた `str.replace` を使う Python が速く、部分文字列の検索を繰り返す実装（Go の `strings.Replacer`、C++ の `std::string::find`）は検索1回ごとのコストが積み重なります。

#### 実際の文章: Wikipedia「プログラミング」の記事（約40KB、194行）

`benchmark/run_benchmark.sh benchmark/wikipedia_programming.txt --warmup 5 --runs 30`

| 言語 | 平均 [ms] | 最小 [ms] | 最大 [ms] | Rust との比 |
|:---|---:|---:|---:|---:|
| Rust | 4.7 ± 0.3 | 4.0 | 5.2 | 1.00 |
| Go | 4.7 ± 0.3 | 3.8 | 5.2 | 1.01 |
| C++ | 5.1 ± 0.3 | 4.2 | 5.6 | 1.10 |
| C#（.NET Native AOT） | 6.6 ± 0.3 | 5.5 | 7.1 | 1.42 |
| Python | 17.8 ± 0.4 | 17.0 | 18.9 | 3.81 |
| TypeScript（Node.js） | 28.4 ± 0.5 | 27.1 | 29.6 | 6.08 |
| Java | 42.2 ± 0.7 | 41.2 | 44.3 | 9.04 |
| C#（Mono） | 85.4 ± 4.6 | 72.3 | 97.2 | 18.29 |

入力が小さいので、変換よりもプロセスの起動（ランタイムの読み込みや JIT）にかかる時間がほとんどです。ネイティブコードにコンパイルする言語（Rust、Go、C++、.NET Native AOT）が速く、Mono・Java・Node.js は起動の分だけ遅くなります。

同じ C# でも、.NET Native AOT は Mono よりどちらのデータでも大幅に速く、「C# が遅い」のは言語ではなく Mono ランタイムによるものです。Windows 版のアプリを C#（.NET）で書いたのは、この結果も踏まえています。

全実装の出力は、`make test` でバイト単位で一致することを確認しています。

## ディレクトリ構成

```
converters/
├── Makefile                      # ビルド・テスト・ベンチマーク
├── punctuation_converter.cpp
├── PunctuationConverter.cs       # Mono と .NET で共通
├── punctuation_converter.go
├── PunctuationConverter.java
├── punctuation_converter.py
├── punctuation_converter.rs
├── punctuation_converter.ts
├── dotnet/                       # .NET SDK（Native AOT）用のプロジェクト
├── package.json, tsconfig.json   # TypeScript 用
├── tests/
│   ├── run_tests.sh              # 全実装のテスト
│   └── cases/                    # 入力と期待する出力
└── benchmark/
    ├── run_benchmark.sh          # hyperfine での計測
    ├── generate_repeated_text.sh # 合成データの作成
    ├── wikipedia_programming.txt # 実際の文章（CC BY-SA 4.0、ATTRIBUTION.md を参照）
    └── ATTRIBUTION.md
```
