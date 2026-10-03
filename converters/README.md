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

## 必要なツール

| 対象 | ツール |
|---|---|
| C#（.NET Native AOT） | [.NET 10 SDK](https://dotnet.microsoft.com/download) 以降と、Native AOT 用のツール（macOS は Xcode Command Line Tools、Linux は clang と zlib） |
| Python | Python 3.9 以降 |
| Rust | Rust（`rustc`） |
| ベンチマーク | [hyperfine](https://github.com/sharkdp/hyperfine)（`brew install hyperfine`） |

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

### 計測結果（選定時の参考値）

- 日時: 2026-10-03
- マシン: Apple M2 / macOS 26.6.1
- コンパイラー・ランタイム: Apple clang 21.0.0、.NET SDK 10.0.401、Mono 6.12.0、Go 1.27.0、Java 25、Python 3.13.3、Rust 1.98.1、Node.js 24.13.1 + TypeScript 7.0.2、hyperfine 1.20.0

#### 合成データ: 「、。」を500万回繰り返した1行（約30MB）

`benchmark/run_benchmark.sh benchmark/repeated_text.txt --warmup 2 --runs 10`

| 言語 | 平均 [ms] | 最小 [ms] | 最大 [ms] | Rust との比 | 判断 |
|:---|---:|---:|---:|---:|:---|
| Rust | 51.6 ± 0.4 | 51.3 | 52.5 | 1.00 | 残す |
| C#（.NET Native AOT） | 52.0 ± 0.3 | 51.7 | 52.4 | 1.01 | 残す |
| Python | 52.9 ± 0.5 | 52.2 | 53.9 | 1.02 | 残す |
| Java | 115.2 ± 0.6 | 114.2 | 116.4 | 2.23 | 削除 |
| Go | 184.3 ± 0.8 | 183.3 | 186.2 | 3.57 | 削除 |
| C++ | 185.7 ± 0.5 | 184.7 | 186.5 | 3.59 | 削除 |
| C#（Mono） | 217.4 ± 6.9 | 207.4 | 230.3 | 4.21 | 削除 |
| TypeScript（Node.js） | 493.8 ± 1.1 | 492.3 | 495.4 | 9.56 | 削除 |

#### 実際の文章: Wikipedia「プログラミング」の記事（約40KB、194行）

`benchmark/run_benchmark.sh benchmark/wikipedia_programming.txt --warmup 5 --runs 30`

| 言語 | 平均 [ms] | 最小 [ms] | 最大 [ms] | Rust との比 | 判断 |
|:---|---:|---:|---:|---:|:---|
| Rust | 4.7 ± 0.3 | 4.0 | 5.2 | 1.00 | 残す |
| Go | 4.7 ± 0.3 | 3.8 | 5.2 | 1.01 | 削除 |
| C++ | 5.1 ± 0.3 | 4.2 | 5.6 | 1.10 | 削除 |
| C#（.NET Native AOT） | 6.6 ± 0.3 | 5.5 | 7.1 | 1.42 | 残す |
| Python | 17.8 ± 0.4 | 17.0 | 18.9 | 3.81 | 残す |
| TypeScript（Node.js） | 28.4 ± 0.5 | 27.1 | 29.6 | 6.08 | 削除 |
| Java | 42.2 ± 0.7 | 41.2 | 44.3 | 9.04 | 削除 |
| C#（Mono） | 85.4 ± 4.6 | 72.3 | 97.2 | 18.29 | 削除 |

置換する文字が1,000万個ある合成データでは置換そのものの速さが、約40KB の実際の文章ではプロセスの起動（ランタイムの読み込みや JIT）にかかる時間がほとんどを占めます。

### 残す実装とその理由

| 言語 | 選んだ理由 |
|---|---|
| Rust | 2種類のデータのどちらでも最速（実際の文章では Go と同率）で、速度の基準になる。標準ライブラリーだけで書け、`rustc` 1つでビルドできる。 |
| C#（.NET Native AOT） | 合成データでは Rust と同等（1.01 倍）、実際の文章でも 1.42 倍と、ネイティブ実行と呼べる速さ。Windows 版のアプリも C#（.NET）で書いているので、同じ言語・同じツールで保守できる。 |
| Python | 合成データでは Rust と同等（1.02 倍）で、C で書かれた `str.replace` が効いている。実際の文章でも 18 ms 弱で、体感の差はない。ビルドが不要で、macOS・Linux では最初から入っていることが多く、ソースを直接実行できるので、利用者がいちばん手軽に使える。 |

### 削除した実装とその理由

| 言語 | 削除した理由 |
|---|---|
| C#（Mono） | 同じソースを .NET Native AOT でもビルドしており、どちらのデータでも大幅に遅い（合成データで 4.2 倍、実際の文章で 18 倍）。「C# が遅い」ように見えたのは言語ではなく Mono ランタイムによるもの。基準3により削除。 |
| Go | 実際の文章では Rust と同じだが、合成データでは 3.6 倍遅い。「速いネイティブバイナリー」という役割は Rust が果たしており、Go にしかない利点がない（基準1・2）。 |
| C++ | Go とほぼ同じ結果（どちらのデータでも Rust 以下）。コンパイラーの有無が環境で変わり、Rust に対して優位な点がない（基準1）。 |
| Java | 合成データで 2.2 倍、実際の文章で 9 倍遅く、JVM の起動が支配的。Python や C# に対して優位な点がない（基準1）。 |
| TypeScript | どちらのデータでも最も遅い部類（合成データで最下位の 9.6 倍）。Node.js と、TypeScript のコンパイル（`npm install`）が必要で、手軽さでも Python に劣る（基準1）。 |

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
