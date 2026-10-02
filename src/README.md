# 句読点変換ベンチマーク

同じ処理（標準入力を1行ずつ読み、「、」→「，」・「。」→「．」に置換して標準出力へ書き出す）を複数の言語で実装し、[hyperfine](https://github.com/sharkdp/hyperfine) で速度を比較するためのサンプル集です。

## 実装一覧

| 言語 | ファイル | ビルド方法 | 実行コマンド |
|---|---|---|---|
| C++ | `punc_converter.cpp` | `make cpp` | `./bin/cpp_bin` |
| C#（Mono） | `punc_converter.cs` | `make cs` | `mono bin/punc_converter.exe` |
| C#（.NET Native AOT） | `dotnet/Program.cs` | `make cs-dotnet` | `./bin/dotnet-aot/PuncConverter` |
| Go | `punc_converter.go` | `make go` | `./bin/go_bin` |
| Java | `punc_converter.java` | `make java` | `java -cp bin punc_converter` |
| Python | `punc_converter.py` | 不要 | `python3 punc_converter.py` |
| Rust | `punc_converter.rs` | `make rs` | `./bin/rust_bin` |
| TypeScript | `punc_converter.ts` | `make ts` | `node bin/punc_converter.js` |

> `punc_converter.cs`（Mono）と `dotnet/Program.cs`（.NET SDK / Native AOT）はロジックは同一で、ビルド・実行系統が異なるだけです。Windows実装の言語検討のため、両方の実測比較を後述しています。

すべて `<入力ファイル` のように標準入力からテキストを受け取り、変換結果を標準出力に書き出します。

## 必要なツール

- g++ / clang++（C++）
- Mono（`mcs` / `mono`）（C#、`make cs`）
- .NET SDK 8 以降（C#、`make cs-dotnet`。Native AOT 用ビルドツールチェーンが必要。macOSでは Xcode Command Line Tools が入っていればOK）
- Go
- JDK（`javac` / `java`）
- Python 3
- Rust（`rustc`）
- Node.js と TypeScript（`npm install` で `@types/node` を導入済み）
- [hyperfine](https://github.com/sharkdp/hyperfine)（`brew install hyperfine`）

## ビルド

```sh
npm install   # TypeScript の型定義（@types/node）を取得
make          # cpp / cs / go / java / rs / ts をまとめてビルド（bin/ 以下に生成）
```

個別にビルドする場合は `make cpp` のように言語ごとのターゲットを指定します。生成物は `bin/` にまとまり、`make clean` で削除できます。

`make cs-dotnet` は `all` には含まれていません（.NET SDKが無い環境でも `make` 一発が通るようにするため）。C#（.NET AOT）を試す場合は個別に実行してください。`DOTNET_RID` 変数でランタイム識別子を指定できます（デフォルトは `osx-arm64`。Linuxなら `linux-x64` など）。

```sh
make cs-dotnet DOTNET_RID=osx-arm64
```

## ベンチマーク用テストデータの生成

```sh
cd test
./generate_test.sh 5000000 test.txt   # 「、。」を500万回繰り返した1行のテキストを生成（約30MB）
```

第1引数は繰り返し回数（省略時 50000000）、第2引数は出力ファイル名（省略時 `test.txt`）です。

また、実在する自然な日本語文でのベンチマーク用に `test/wikipedia_programming.txt`（[Wikipedia「プログラミング」](https://ja.wikipedia.org/wiki/%E3%83%97%E3%83%AD%E3%82%B0%E3%83%A9%E3%83%9F%E3%83%B3%E3%82%B0)記事本文、約40KB・複数行）も用意しています。

## ベンチマークの実行

```sh
hyperfine --warmup 2 -m 5 \
  --command-name cpp               "./bin/cpp_bin < test/test.txt" \
  --command-name go                "./bin/go_bin < test/test.txt" \
  --command-name rust              "./bin/rust_bin < test/test.txt" \
  --command-name java              "java -cp bin punc_converter < test/test.txt" \
  --command-name "csharp(mono)"    "mono bin/punc_converter.exe < test/test.txt" \
  --command-name "csharp(dotnet-aot)" "./bin/dotnet-aot/PuncConverter < test/test.txt" \
  --command-name python            "python3 punc_converter.py < test/test.txt" \
  --command-name "typescript(node)" "node bin/punc_converter.js < test/test.txt"
```

## 実行結果（参考値）

- 日時: 2026-09-23
- マシン: Apple Silicon (arm64) / macOS 26.6.1
- テストデータ: `、。` を500万回繰り返した1行・約30MB（`test/generate_test.sh 5000000`）
- コンパイラ/ランタイム: Apple clang 21.0.0、Go 1.27.0、Rust 1.98.1、OpenJDK 25、Mono 6.12.0、Python 3.13.3、Node.js 24.13.1 + TypeScript 7.0.2、hyperfine 1.20.0

| 言語 | 平均実行時間 | 最小 | 最大 | Pythonとの相対倍率 |
|---|---:|---:|---:|---:|
| Python | 0.079 s ± 0.007 | 0.075 s | 0.116 s | 1.00（基準） |
| Rust | 0.124 s ± 0.009 | 0.118 s | 0.163 s | 1.56 × |
| Java | 0.162 s ± 0.004 | 0.155 s | 0.169 s | 2.05 × |
| Go | 0.327 s ± 0.021 | 0.312 s | 0.377 s | 4.13 × |
| C#（Mono） | 0.820 s ± 0.069 | 0.768 s | 0.938 s | 10.33 × |
| TypeScript（Node） | 1.297 s ± 0.024 | 1.270 s | 1.327 s | 16.34 × |
| C++ | 2.024 s ± 0.043 | 1.982 s | 2.074 s | 25.51 × |

> **注記:** このテストデータは改行がほぼ無い巨大な1行（約30MB）です。C++実装は `std::string::replace` を置換対象の出現数（約500万回）だけ繰り返し呼び出しており、1行が非常に長い場合はこの呼び出し回数のオーバーヘッドが支配的になり、Python の1回の `str.replace()`（内部でCレベルの単純走査）より遅くなります。複数行に分かれた通常のテキストではこの傾向は変わる可能性があるため、自分のユースケースに近いデータで測り直すことを推奨します。
>
> 全実装の出力は `md5`（`md5sum`）で比較し、バイト単位で一致することを確認済みです。

### 参考: C#（.NET Native AOT）を含めた再計測

上記の表の C#（Mono）はMonoランタイム上での実行です。Windows実装の言語として.NET（Mono ではなく `dotnet publish` でビルドした実行ファイル）を検討しているため、同じ `test/test.txt` で .NET 版も計測しました（`hyperfine --warmup 2 -m 5`、日時・環境は上記と同一、.NET SDK 8.0.302）。

| 言語 | 平均実行時間 | 最小 | 最大 | 備考 |
|---|---:|---:|---:|---|
| C#（.NET Native AOT） | 0.069 s ± 0.009 | 0.065 s | 0.111 s | `dotnet publish -p:PublishAot=true` でビルドしたネイティブ実行ファイル |
| C#（.NET 通常ビルド） | 0.090 s ± 0.001 | 0.088 s | 0.093 s | `dotnet publish`（AOTなし、.NETランタイム同梱） |
| Rust（参考・再掲） | 0.117 s ± 0.001 | 0.115 s | 0.120 s | 上表と同条件で再計測 |
| C#（Mono・参考・再掲） | 0.807 s ± 0.026 | 0.778 s | 0.836 s | 上表と同条件で再計測 |

Native AOTビルドのC#はこのデータではRustより速く、Monoと比べて**約12倍**高速でした。「C#が遅い」という結果は言語ではなくMonoランタイムに起因していたことになります。Windows向けの実装を検討する際は、Monoではなくこの.NET AOTの数値を基準にするのが妥当です。

## ベンチマーク結果（実文テストケース: Wikipedia「プログラミング」記事）

上記の合成データ（`、。` の反復）は巨大な1行という極端なケースだったため、実在する自然文でも比較してみました。

- テストデータ: `test/wikipedia_programming.txt`（Wikipedia「プログラミング」記事本文、約40KB・複数行、「、」352個・「。」212個）
- コマンド: 上記と同じ7実装を `test/wikipedia_programming.txt` に対して実行（`hyperfine --warmup 5 -m 20`）
- 日時・環境: 上記の実行結果と同一

| 言語 | 平均実行時間 | 最小 | 最大 | Rustとの相対倍率 |
|---|---:|---:|---:|---:|
| Rust | 2.0 ms ± 0.1 | 1.8 ms | 2.6 ms | 1.00（基準） |
| Go | 2.6 ms ± 0.1 | 2.2 ms | 3.7 ms | 1.33 × |
| C++ | 4.2 ms ± 0.2 | 4.0 ms | 6.0 ms | 2.12 × |
| Python | 14.6 ms ± 0.5 | 14.1 ms | 18.6 ms | 7.38 × |
| TypeScript（Node） | 26.2 ms ± 0.4 | 25.3 ms | 28.2 ms | 13.26 × |
| Java | 33.7 ms ± 0.5 | 33.0 ms | 36.7 ms | 17.06 × |
| C#（Mono） | 83.5 ms ± 4.8 | 72.0 ms | 92.3 ms | 42.23 × |

> **注記:** ファイルサイズが約40KBと小さいため、実際の変換処理そのものより言語ランタイムやプロセスの起動オーバーヘッド（JITウォームアップ、GC初期化など）が支配的です。そのため上の合成データ（約30MB・500万回反復）のベンチマークとは傾向が大きく異なり、単純に順位を比較することはできません。「巨大な1行を高速に処理する」ケースと「小さな自然文をすぐさま処理する」ケースでは適した実装が異なる、という参考値として見てください。
>
> 全実装の出力は `md5` で比較し、バイト単位で一致することを確認済みです。

### 参考: C#（.NET Native AOT）を含めた再計測

こちらも同様に、Mono以外の.NET実行系での計測結果を追記します（`hyperfine --warmup 5 -m 20`、条件は上記と同一）。

| 言語 | 平均実行時間 | 最小 | 最大 | 備考 |
|---|---:|---:|---:|---|
| C#（.NET Native AOT） | 4.5 ms ± 0.5 | 3.8 ms | 7.5 ms | Rustと同等（1.8倍以内） |
| Rust（参考・再掲） | 2.5 ms ± 3.0 | 1.8 ms | 55.2 ms | 上表と同条件で再計測（外れ値あり） |
| C#（.NET 通常ビルド） | 26.9 ms ± 4.7 | 25.0 ms | 72.4 ms | AOTなし。プロセス起動時のJIT・アセンブリ読み込みが乗る |
| C#（Mono・参考・再掲） | 90.1 ms ± 5.0 | 79.6 ms | 104.0 ms | 上表と同条件で再計測 |

小さい入力でも、Native AOTビルドならMonoの**約20倍**の速度が出ています。起動オーバーヘッドが支配的なこのケースでもRustに肉薄しており、「C#は遅い」という印象は主にMonoランタイムによるものだったことが確認できました。

## ディレクトリ構成

```
src/
├── Makefile                 # 各言語のビルド定義
├── punc_converter.cpp
├── punc_converter.cs         # Mono用
├── punc_converter.go
├── punc_converter.java
├── punc_converter.py
├── punc_converter.rs
├── punc_converter.ts
├── package.json             # TypeScript の型定義取得用
├── dotnet/                  # .NET SDK / Native AOT用（Mono版と同じロジック）
│   ├── PuncConverter.csproj
│   └── Program.cs
└── test/
    ├── generate_test.sh          # ベンチマーク用テストデータ生成スクリプト（合成データ）
    └── wikipedia_programming.txt # ベンチマーク用テストデータ（実文、Wikipedia「プログラミング」記事）
```
