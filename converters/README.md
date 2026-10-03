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

### 使う実装だけを残す

3つの実装はそれぞれ単独で動き、互いに依存していません。使うのは1つだけで十分なので、クローンしたあとは、使わない実装のファイルを削除してください。たとえば Rust を使うなら `punctuation_converter.rs` だけを残し、`punctuation_converter.py`・`PunctuationConverter.cs`・`dotnet/` は不要です。Python を使うなら `punctuation_converter.py` 1つだけで動きます。`make test` は、削除した実装を SKIP にして残りの実装をテストします。

## 必要なツール

| 対象 | ツール |
|---|---|
| C#（.NET Native AOT） | [.NET 10 SDK](https://dotnet.microsoft.com/download) 以降と、Native AOT 用のツール（macOS は Xcode Command Line Tools、Linux は clang と zlib、Windows は Visual Studio の「C++ によるデスクトップ開発」） |
| Python | Python 3.9 以降 |
| Rust | Rust（`rustc`） |
| ベンチマーク | [hyperfine](https://github.com/sharkdp/hyperfine)（`brew install hyperfine`、Windows は `winget install sharkdp.hyperfine`） |

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

計測は性能の異なる2つの環境（macOS の Apple M2 と、Windows の Ryzen 7 9700X）で行い、**どちらの環境でも成り立つ結果だけ**を判断の根拠にしました。順位が環境によって入れ替わる部分（実際の文章での Rust・Go・C++ の差など）は、1ミリ秒未満の差で体感できないため、判断には使っていません。

### 計測結果（選定時の参考値）

置換する文字が1,000万個ある合成データでは置換そのものの速さが、約40KB の実際の文章ではプロセスの起動（ランタイムの読み込みや JIT）にかかる時間がほとんどを占めます。

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

| | macOS | Windows |
|---|---|---|
| 日時 | 2026-10-03 | 2026-10-04 |
| マシン | Apple M2 / macOS 26.6.1 | AMD Ryzen 7 9700X / Windows 11 Home（26200） |
| C++ | Apple clang 21.0.0（`-O3`） | MSVC 14.51（`/O2`） |
| C# | .NET SDK 10.0.401、Mono 6.12.0 | .NET SDK 10.0.401、Mono 6.12.0（32ビット版） |
| Go / Java | Go 1.27.0 / Java 25 | Go 1.27.0 / Java 26 |
| Python | Python 3.13.3 | Python 3.13.3（python.org 版） |
| Rust | Rust 1.98.1 | Rust 1.99.0 |
| TypeScript | Node.js 24.13.1 + TypeScript 7.0.2 | Node.js 24.18.0 + TypeScript 7.0.2 |
| 計測 | hyperfine 1.20.0（`benchmark/run_benchmark.sh`） | hyperfine 1.20.0（下記） |

macOS では `benchmark/run_benchmark.sh` に、合成データは `--warmup 2 --runs 10`、実際の文章は `--warmup 5 --runs 30` を指定しました。Windows の hyperfine はコマンドを `cmd.exe` で実行するため、このスクリプトはそのままでは動きません。そこで、シェルを介さずに標準入力を与える `hyperfine -N --input <入力ファイル> <実行ファイルの絶対パス> ...` で、同じ回数を計測しました。実際の文章の Rust・C++・Go・Native AOT は差が小さいため、`--warmup 10 --runs 100` で計測し直した値を載せています。

> **Windows の Python について:** Microsoft Store から入れた Python は、起動するだけで約75 ms かかります（`python -c pass` だけで 74.9 ms、python.org 版は 14.6 ms）。そのため上の表は python.org 版の値です。Store 版では合成データが 118.6 ms（Rust の 2.15 倍）、実際の文章が 75.8 ms でした。差はすべて起動にかかる時間で、変換の速さは変わりません。

### 残す実装とその理由

| 言語 | 選んだ理由 |
|---|---|
| Rust | 合成データではどちらの環境でも最速の組（Native AOT と同等）で、実際の文章でも最速の組（macOS では Go と同率、Windows では C++ との差が 0.4 ms）に入る。両方のデータで、両方の環境で最速の組に入るのは Rust だけなので、速度の基準になる。標準ライブラリーだけで書け、`rustc` 1つでビルドできる。 |
| C#（.NET Native AOT） | 合成データではどちらの環境でも Rust と同等（0.98〜1.01 倍）。実際の文章では Rust の 1.4〜1.8 倍だが 8 ms 未満で、体感の差はない。Windows 版のアプリも C#（.NET）で書いているので、同じ言語・同じツールで保守できる。 |
| Python | 速さでは Rust に及ばない（合成データで 1.02〜1.24 倍、実際の文章で約3.8 倍）が、合成データでは削除した実装のどれよりも速い。C で書かれた `str.replace` が効いている。実際の文章でも 18 ms 未満で、体感の差はない。ビルドが不要で、macOS・Linux では最初から入っていることが多く、ソースを直接実行できるので、利用者がいちばん手軽に使える。 |

### 削除した実装とその理由

| 言語 | 削除した理由 |
|---|---|
| C#（Mono） | 同じソースを .NET Native AOT でもビルドしており、どちらの環境・どちらのデータでも大幅に遅い（合成データで 2.5〜4.2 倍、実際の文章で 14〜18 倍）。「C# が遅い」ように見えたのは言語ではなく Mono ランタイムによるもの。基準3により削除。 |
| Go | 合成データではどちらの環境でも Rust の 2.5〜3.6 倍。実際の文章では macOS で Rust と同率、Windows で 1.3 倍。「速いネイティブバイナリー」という役割は Rust が果たしており、Go にしかない利点がない（基準1・2）。 |
| C++ | 合成データではどちらの環境でも Rust の 2.3〜3.6 倍。実際の文章では Windows で Rust より 0.4 ms 速かったが、体感できる差ではない。また Windows（MSVC）では標準出力がテキストモードになり、LF が CRLF に変わってテストに通らなかった。コンパイラーの有無も環境で変わり、Rust に対して優位な点がない（基準1）。 |
| Java | どちらの環境でも、合成データで 2.2〜2.8 倍、実際の文章で 9〜20 倍遅く、JVM の起動が支配的。Python や C# に対して優位な点がない（基準1）。 |
| TypeScript | どちらの環境でも、合成データで最下位（7.4〜9.6 倍）。Node.js と、TypeScript のコンパイル（`npm install`）が必要で、手軽さでも Python に劣る（基準1）。 |

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
