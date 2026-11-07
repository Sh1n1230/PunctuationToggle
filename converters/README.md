# 句読点の変換コマンド

すでに書いた文章の句読点を **「、。」⇔「，．」** にまとめて変換するコマンドです。同じ仕様を Rust・C#・Python の3言語で実装しています。言語は [hyperfine](https://github.com/sharkdp/hyperfine) での速度の比較をもとに絞りました（[実装する言語の選定](#実装する言語の選定)）。

## 使い方

標準入力から文章を読み、変換結果を標準出力に書き出します。どの言語の実装も同じように使えます。

```sh
python3 punctuation_converter.py < 原稿.txt > 提出用.txt            # 「、」→「，」、「。」→「．」
python3 punctuation_converter.py --reverse < 提出用.txt > 原稿.txt  # 「，」→「、」、「．」→「。」
```

- 入力と出力に**同じファイルを指定しないでください**（`< 原稿.txt > 原稿.txt`）。シェルが読む前に出力先を空にするため、原稿が消えます。別のファイルに書き出してから置き換えてください。
- 入力ファイルを引数で渡す形や、その場で書き換えるオプションはありません。標準入力と標準出力だけにして、3つの実装の仕様を小さく保っています。
- 入力をリダイレクトせずに起動すると、端末からの入力を待ちます（案内は表示しません。`cat` などと同じです）。終えるには Ctrl+D（Windows は Ctrl+Z の後に Enter）を押します。

### Windows での使い方

- コマンド プロンプト（cmd.exe）では、上と同じく `<` と `>` が使えます。Python の起動コマンドは環境によって `py` か `python` です。

  ```bat
  py punctuation_converter.py < 原稿.txt > 提出用.txt
  ```

- PowerShell では `<` で入力を渡せません。cmd.exe を通してください。

  ```powershell
  cmd /c "py punctuation_converter.py < 原稿.txt > 提出用.txt"
  ```

- PowerShell の `Get-Content 原稿.txt | ...` や `... > 提出用.txt` は、文字コードや改行コードを変えることがあるため使わないでください。

| オプション | 動作 |
|---|---|
| （なし） | 「、」を「，」に、「。」を「．」に変換する |
| `-r`, `--reverse` | 「，」を「、」に、「．」を「。」に変換する |
| `-h`, `--help` | 使い方を表示する |
| `--version` | 版を表示する（`punctuation_converter 1.3.0` のような1行。アプリと同じ版の番号） |

- 句読点以外は1バイトも変えません。改行コード（LF / CRLF）、最後の行の改行の有無、BOM、絵文字などはそのまま残ります。
- ASCII の `,` `.`、半角の `､` `｡`、縦書き用・小字形の句読点（`︐` `︑` `︒` `﹐` `﹑` `﹒`）は変換しません。
- 入力は UTF-8 にしてください。UTF-8 として正しくないバイト列があると、どの実装もエラーを表示して終了コード `1` で終了します。エラーには、最初の正しくないバイトの位置を「（3 行目、先頭から 120 バイト目）」の形で示します（バイトは 1 から数え、行は LF（CRLF の LF を含む）で数えます。CR だけの改行は行として数えません）。それまでに書き出した出力は途中までの不完全なもので、その量は実装の読み込みの単位によって違います（入力全体を確かめてから書き出すと、大きな入力でメモリを使い切るため）。終了コードが `1` のときの出力は使わないでください。テストでも、このときの標準出力は比べていません。
- 終了コードは、成功が `0`、読み書きの失敗が `1`、不正な引数が `2` です。
- シェルの `>&-`・`<&-` などで標準入出力が閉じている場合は、閉じた出力先には書けたものとして、閉じた入力は空として扱います（終了コード `0`。不正な引数は `2`）。
- `| head` のように出力の読み手が先に終了した場合は、`cat` や `grep` などと同じく、何も表示せずに終了します（終了コード `0`）。先頭だけを見るたびにエラーが出ないようにするためです（.NET の標準出力も、この場合は黙って書き込みをやめます）。

## 実装の一覧

| 言語 | ファイル | ビルド | 実行コマンド |
|---|---|---|---|
| C#（.NET Native AOT） | `PunctuationConverter.cs` | `make csharp-dotnet` | `bin/dotnet/PunctuationConverter` |
| Python | `punctuation_converter.py` | 不要 | `python3 punctuation_converter.py` |
| Rust | `punctuation_converter.rs` | `make rust` | `bin/punctuation_converter_rust` |

どの実装も、改行の無い巨大な入力でもメモリを使い切らないよう、入力を一定の量ずつ読みます。置換する文字はどれも1文字なので、どこで区切っても変換結果は同じです。

- 一定の文字数ずつ読む: C#、Python
- 一定のバイト数ずつ読む: Rust（UTF-8 の文字の途中で区切らないよう、末尾で切れた文字は次の読み込みに回す）

### 1つの実装だけを使う

3つの実装は互いに依存していません。どれか1つのファイルをコピーすれば、そのまま使えます（Python なら `punctuation_converter.py` だけ、Rust なら `punctuation_converter.rs` をビルドしたもの、C# なら `PunctuationConverter.cs` と `dotnet/` をビルドしたもの）。使わない実装のファイルを消す必要はありません。

消した場合も、`make test` は消した実装を SKIP にして、残りの実装だけを確かめます。ただし `make`（`make rust`）は `punctuation_converter.rs` を、`make csharp-dotnet` は `PunctuationConverter.cs` を使うので、消した実装のビルドのターゲットは使えなくなります。

## 必要なツール

| 対象 | ツール |
|---|---|
| C#（.NET Native AOT） | [.NET 10 SDK](https://dotnet.microsoft.com/download) 以降と、Native AOT 用のツール（macOS は Xcode Command Line Tools、Linux は clang と zlib、Windows は Visual Studio の「C++ によるデスクトップ開発」） |
| Python | Python 3.10 以降 |
| Rust | Rust（`rustc`）。[rustup](https://rustup.rs/) を使うと、リポジトリのルートの `rust-toolchain.toml` で固定した版（CI と同じ版）が使われます |
| ベンチマーク | [hyperfine](https://github.com/sharkdp/hyperfine) 1.16.0 以降（macOS は `brew install hyperfine`、Windows は `winget install sharkdp.hyperfine`） |

## ビルドとテスト

このディレクトリで実行します。

```sh
make                      # Rust をビルド（bin/ に出力）
make csharp-dotnet        # C#（.NET Native AOT）をビルド（.NET SDK が無い環境でも make が通るよう、別にしている）
make test                 # すべての実装をテストする
make rust-test            # Rust 実装の単体テスト（UTF-8 の文字の区切りの処理など）
make test-random          # ランダムに作った入力で、すべての実装をテストする（Python 3.10 以降が必要）
```

Windows では Git Bash で同じコマンドを実行します。Git Bash には `make` が無いため、GNU make（`winget install ezwinports.make` や `choco install make` など）を入れてください。`python3` が無い場合は `python` が使われます（`make test-random` は `make PYTHON=python test-random` とします）。CI では、Linux・macOS・Windows の3つで同じテストを実行しています（Windows は Chocolatey の make 4.4.1）。

`make test`（`tests/run_tests.sh`）は、`tests/cases/` の入力をそれぞれの実装に与え、出力が期待どおりかをバイト単位で比べます。CRLF や CR だけの改行、最後の行に改行が無い文章、空の入力、BOM や絵文字、読み込みの区切りをまたぐ長い1行、UTF-8 として正しくない入力（メッセージも比べる）、書き込めない出力先と使い方を書き出せない場合（`/dev/full` がある Linux のみ）、`--help` の出力が `tests/usage.txt` と同じこと、`--version`、不正な引数、出力の読み手が先に終了した場合と閉じた標準入出力（Windows を除く）、入力待ちの Ctrl+C（`tests/test_sigint.py`。POSIX のみ）を確かめています。成功したときは標準エラー出力が空であることも確かめます。最初に、`tests/cases/` のケースが確かめたい性質（CRLF・BOM など）を含んでいるかも確かめます。ビルドされていない実装は SKIP になります。特定の実装だけを確かめるときは `tests/run_tests.sh rust python` のように指定します。

`make test-random`（`tests/run_random_tests.py`）は、句読点・似た記号・全角半角・3種類の改行・絵文字・BOM を混ぜた入力を毎回ランダムに作り、出力が句読点だけを置き換えたものになること、変換が冪等であること、変換してから逆変換しても逆変換だけの結果と同じになること、正しくない UTF-8 を混ぜると終了コード 1 になることを確かめます。成功したときに標準エラー出力が空であることや、正しくない UTF-8 を変換・逆変換のどちらで与えても決まったメッセージになることも確かめます。縦書き用・小字形の句読点（︐︑︒﹐﹑﹒）や、NUL・非文字（U+FFFE・U+FFFF）・最大の符号位置（U+10FFFF）なども混ぜます。失敗したときは乱数の種と何件目のケースかが表示され、入力と期待する出力が `tests/cases/` と同じ形で一時ディレクトリー（`RANDOM_FAILURES_DIR` で指定できる）に保存されます。`RANDOM_SEED=<種> make test-random` で同じ入力を再現できます。保存された入力は、最小の形にして `tests/cases/` に追加してください。1回の実行は30秒で打ち切ります。

`make rust-test` は、Rust 実装だけが持つ処理（入力を一定のバイト数ずつ読み、途中で切れた UTF-8 の文字を次の読み込みに回す処理）を、文字の何バイト目で切れるかや、1回の読み込みで返るバイト数を狙って確かめます。

`make clean` で、ビルドの生成物と、ベンチマークの合成データ（約30MB）・結果を削除できます。ベンチマークの分だけを消すときは `make clean-benchmark` とします。`make` はソースが変わったときだけビルドし直し、`make test` はビルド済みの実装がソースより古ければ作り直してからテストします。

`make csharp-dotnet` は、.NET のランタイム識別子を実行中の OS と CPU（x64・arm64）から決めます。それ以外の CPU では `make DOTNET_RID=linux-arm csharp-dotnet` のように指定してください。

## ベンチマーク

```sh
make benchmark                                                                # 合成データ（約30MB）で計測
benchmark/run_benchmark.sh benchmark/wikipedia_programming.txt --warmup 5 --runs 30  # 実際の文章で計測
```

GitHub Actions でも、週に1回（と手動で）ベンチマークのワークフロー（`.github/workflows/benchmark.yml`）を実行し、結果を前回の結果と並べて実行結果のサマリーに表示しています。共有のランナーは性能がばらつくため、結果は性能の大きな変化に気づくための目安で、失敗の判定には使っていません。

ビルド済みの実装をすべて hyperfine で計測し、結果を `benchmark/results/` に Markdown の表（`<入力の名前>.md`）と JSON（`<入力の名前>.json`、比較用）で保存します。同じ名前の結果が既にあるときは、日時を付けた名前で保存します。シェルを介さずに起動する `-N`（`--shell=none`）を常に付けるので、hyperfine の 1.x と 2.x、Windows でも同じ方法で計測します。入力ファイルは呼び出した場所からの相対パスでも渡せます。Python は `PYTHON` 環境変数、`python3`、`python` の順に探します。入力ファイルは `--input` で渡すため hyperfine 1.16.0 以降が必要です（パスに空白を含んでいても計測できます）。合成データ（「、。」を500万回繰り返した1行）は、初回に `benchmark/generate_repeated_text.sh` で作ります。

## 実装する言語の選定

もとは C++・C#（Mono / .NET Native AOT）・Go・Java・Python・Rust・TypeScript の8実装（7言語）を作り、同じテスト（`make test`）で全実装の出力がバイト単位で一致することを確かめたうえで、速度を比べました。変換は1文字ずつの置換なので、どの言語でも同じ結果になります。違いが出るのは速度と、使うまでの手間だけです。そのため次の基準で、役割が重なる実装を減らしました。

1. 計測結果が、残す実装より同じか劣る実装は、残す理由がない。
2. 同じ速さの実装が複数あるときは、ほかの言語にない利点があるものを残す。
3. 同じソースのビルド方法の違いは、速い方だけ残す。

計測は、性能の傾向が違う2つの環境（macOS の Apple M2 と、Windows の Ryzen 7 9700X）で行い、両方の環境で同じように成り立つ結果だけを、残すか削除するかの根拠にしました。環境によって順位が入れ替わるわずかな差（実文章での Rust・Go・C++ の差など）は、1ミリ秒未満で使っていて感じる差にはならないため、判断には使っていません。

### 計測結果（選定時の参考値）

置き換える文字が1,000万個ある約30MBの合成データでは、文字列を調べて置き換える処理そのものの速さが時間の大半を占めます。約40KBの実文章では、ランタイムの読み込みや JIT コンパイルなど、プロセスの起動にかかる時間が大半を占めます。

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

macOS では `benchmark/run_benchmark.sh` で、合成データは `--warmup 2 --runs 10`、実文章は `--warmup 5 --runs 30` で計測しました。Windows では、hyperfine が既定でコマンドを `cmd.exe` を通して実行するため、シェルを通さずに標準入力を渡す `hyperfine -N --input <入力ファイル> <実行ファイルの絶対パス> ...` で、同じ回数だけ計測しました（今の `run_benchmark.sh` は `-N` を常に付けます）。実文章での Rust・C++・Go・Native AOT は差がわずかなため、`--warmup 10 --runs 100` で計測し直した値を載せています。

> [!NOTE]
> **Windows 環境における Python の測定値について:**
> Microsoft Store 版の Python は、起動だけで約 75 ms かかります（`python -c pass` が 74.9 ms。python.org 版は 14.6 ms）。そのため上の表には python.org 版の値を載せています。Store 版では、合成データが 118.6 ms（Rust の 2.15 倍）、実文章が 75.8 ms でした。この差はすべて起動にかかる時間によるもので、置き換えの処理の速さは変わりません。

### 残す実装とその理由

| 言語 | 選んだ理由 |
|---|---|
| Rust | 合成データでは両方の環境で最も速い組（Native AOT と同じ）に入り、約 40KB の実文章でも最も速い組（macOS では Go と同じ、Windows では C++ との差が 0.4 ms）に入ります。両方のデータ・両方の環境で最も速い組に入るのは Rust だけなので、速さを比べるときの基準にしています。外部のクレートを使わず標準ライブラリだけで書いてあり、`rustc` だけでビルドできます。 |
| C#（.NET Native AOT） | 合成データでは両方の環境で Rust と同じ速さ（0.98〜1.01 倍）で、実文章でも 8 ms 未満（Rust の 1.4〜1.8 倍）です。Windows 版の常駐アプリも C#（.NET）で書いているので、同じ言語と開発ツールで保守できます。 |
| Python | Rust よりは遅いものの（合成データで 1.02〜1.24 倍、実文章で約 3.8 倍）、合成データでは削除したほかのどの言語よりも速くなりました。C で書かれた `str.replace` が文字列を速く調べるためです。実文章でも 18 ms 未満で、使っていて差を感じることはほとんどありません。ビルドが要らず、macOS や Linux には最初から入っていることが多いので、使い始めるまでの手間が最も少なくて済みます。 |

### 削除した実装とその理由

| 言語 | 削除した理由 |
|---|---|
| C#（Mono） | 同じソースを .NET Native AOT でもビルドしており、両方の環境・両方のデータで大幅に遅くなりました（合成データで 2.5〜4.2 倍、実文章で 14〜18 倍）。遅いのは言語ではなく Mono のランタイムのためです（基準3）。 |
| Go | 合成データでは両方の環境で Rust の 2.5〜3.6 倍の時間がかかりました。実文章では macOS で Rust と同じ、Windows で 1.3 倍でした。「速いネイティブの実行ファイル」という役割は Rust で足りており、Go にしかない利点がありません（基準1・2）。 |
| C++ | 合成データでは両方の環境で Rust の 2.3〜3.6 倍の時間がかかりました。実文章の Windows では Rust より 0.4 ms 速かったものの、使っていて感じる差ではありません。Windows（MSVC）では標準出力がテキストモードで動き、LF が CRLF に変わってテストに通りませんでした。環境ごとのコンパイラの違いも大きく、Rust の代わりに残す利点がありません（基準1）。 |
| Java | 両方の環境で、合成データで 2.2〜2.8 倍、実文章で 9〜20 倍遅く、時間の大半が JVM（Java 仮想マシン）の起動でした。Python や C# より優れた点がありません（基準1）。 |
| TypeScript | 両方の環境で、合成データで最も遅くなりました（7.4〜9.6 倍）。Node.js とビルドの環境を用意する必要があり、手軽さでも Python に及びません（基準1）。 |

削除した実装のコードは Git の履歴（v1.1.0 より前のコミット）に残っています。

## ディレクトリ構成

```
converters/
├── Makefile                        # ビルド・テスト・ベンチマーク
├── .gitignore                      # bin/・ベンチマークの合成データと結果を無視する
├── PunctuationConverter.cs         # C# 実装
├── punctuation_converter.py        # Python 実装
├── punctuation_converter.rs        # Rust 実装（単体テストも含む。make rust-test）
├── dotnet/                         # .NET SDK（Native AOT）用のプロジェクト
├── tests/
│   ├── run_tests.sh                # 全実装のテスト（make test）
│   ├── run_random_tests.py         # ランダムな入力でのテスト（make test-random）
│   ├── test_sigint.py              # 入力待ちの Ctrl+C のテスト（run_tests.sh が全実装で実行する）
│   ├── test_punctuation_converter.py # Python 実装の main の単体テスト（run_tests.sh が実行する）
│   ├── usage.txt                   # --help で表示する使い方（3つの実装で同じ）
│   └── cases/                      # 入力と期待する出力
└── benchmark/
    ├── run_benchmark.sh            # hyperfine での計測
    ├── generate_repeated_text.sh   # 合成データの作成
    ├── wikipedia_programming.txt   # 実際の文章（CC BY-SA 4.0、ATTRIBUTION.md を参照）
    └── ATTRIBUTION.md

（ruff の設定はリポジトリのルートの ruff.toml）
```
