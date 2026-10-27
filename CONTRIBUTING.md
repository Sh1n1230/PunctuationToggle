# 開発ガイド

PunctuationToggle への貢献を歓迎します。不具合の報告や要望は [Issues](https://github.com/Sh1n1230/PunctuationToggle/issues) へ、修正は Pull Request でお送りください。参加にあたっては [行動規範](CODE_OF_CONDUCT.md) を守ってください。

## ディレクトリ構成

```
PunctuationToggle/
├── macOS/                          macOS 版（Swift）
│   ├── PunctuationToggle/          アプリ本体
│   │   └── Core/                   OS に依存しない判定ロジック（単体テストの対象）
│   ├── Tests/                      Core の単体テスト（Swift Testing）
│   ├── Scripts/                    開発用のスクリプト（アプリアイコンの PNG を作る generate_app_icon.swift）
│   ├── Package.swift               Core をテストするための Swift パッケージ
│   ├── PunctuationToggle.xcodeproj
│   └── install.sh / uninstall.sh   （共通の手順は app_bundle.sh）
├── Windows/                        Windows 版（C# / .NET）
│   ├── PunctuationToggle/          アプリ本体（Windows Forms）
│   ├── PunctuationToggle.Core/     OS に依存しない判定ロジック（単体テストの対象）
│   ├── PunctuationToggle.Tests/    Core の単体テスト（xUnit）
│   ├── PunctuationToggle.sln
│   └── install.ps1 / uninstall.ps1
├── converters/                     句読点の変換コマンド（3言語）とベンチマーク
├── .github/
│   ├── workflows/                  CI・リリース・ベンチマークの自動化
│   ├── lint/                       CI の「コーディング規約」ジョブのツールの版の固定と設定
│   └── scripts/                    ワークフローから使うスクリプト
├── global.json                     .NET SDK の版
├── rust-toolchain.toml             Rust の版
└── nuget.config                    NuGet のパッケージソース（nuget.org だけ）
```

キー入力の判定（切り替えキーの単独押しの検出や、設定画面でのキーの読み取り）は、どちらの OS でも `Core` に OS の API を使わない形で切り出しています。OS の入力イベントは `KeyboardInput` に変換してから `TriggerKeyDetector` / `TriggerKeyCapture` に渡します。判定の仕様を変えるときは、まず `Core` のテストを追加・修正してください。

### アプリアイコン

macOS 版のアプリアイコンの PNG（`macOS/PunctuationToggle/Assets.xcassets/AppIcon.appiconset/`）は、`macOS/Scripts/generate_app_icon.swift` で描いて作っています。PNG を直接編集せず、スクリプトを直して、リポジトリのルートで `swift macOS/Scripts/generate_app_icon.swift` を実行し、できた PNG をスクリプトと一緒にコミットしてください。スクリプトも CI の `swift format lint` の対象（`Scripts`）です。

## ビルドとテスト

### macOS 版

Xcode 26 以降が必要です。

```sh
cd macOS
swift test                                   # Core の単体テスト
xcodebuild -project PunctuationToggle.xcodeproj -scheme PunctuationToggle build
./install.sh                                 # ビルドして ~/Applications にインストール
```

### Windows 版

.NET 10 SDK が必要です。CI とリリースで使う SDK の版は、リポジトリのルートの `global.json` で固定しています（Dependabot が更新を提案します）。手元では、同じ 10.0 の、`global.json` の版以上の SDK が使われます（`rollForward` が `latestFeature`）。`global.json` は作業ディレクトリから上にたどって探されるため、リポジトリの外から `Windows/install.ps1` を実行した場合は効きません。`PunctuationToggle.Core` と単体テストは macOS や Linux でも実行でき、アプリ本体もビルド（コンパイルの確認）だけはできます。

```sh
dotnet test Windows/PunctuationToggle.sln
dotnet build Windows/PunctuationToggle.sln
```

CI では、両 OS の Core の単体テストのカバレッジを集計し、Actions の実行結果のサマリーに表示しています（下限は設けていません）。手元では `swift test --enable-code-coverage`（macOS）や `dotnet test Windows/PunctuationToggle.sln --collect:"XPlat Code Coverage"`（Windows）で同じものを作れます。

テストで使う NuGet パッケージは `Windows/PunctuationToggle.Tests/packages.lock.json` で固定しています。パッケージを追加・更新したときは `dotnet restore Windows/PunctuationToggle.sln` で lock ファイルを更新し、一緒にコミットしてください（CI では lock ファイルと食い違うと失敗します）。

### 変換コマンド

```sh
cd converters
make all csharp-dotnet
make test
make test-random
```

詳しくは [converters/README.md](converters/README.md) を参照してください。

### 手動での動作確認

キー入力の監視や日本語入力の設定の書き換えは自動テストできないため、アプリの動作を変えたときは次を手で確認してください。

- 切り替えキーの単独押しで「、。」⇔「，．」が切り替わり、メモ帳・テキストエディット・ブラウザなどで入力する句読点が変わる。
- 切り替えキーを使ったショートカット（右Command＋C、右Ctrl＋C など）やクリックでは切り替わらない。
- 「切り替えキーを変更…」で修飾キーとそれ以外のキー（F13、無変換など）を設定でき、アプリを再起動しても設定が残る。文字キーは設定できない。
- 「切り替えキーを変更…」のウィンドウを開いたままほかのアプリに切り替えると、設定ウィンドウが閉じ、そのアプリで普通に文字を入力でき、切り替えキーも元どおり効く。
- ほかのアプリ（メモやメモ帳など）が前面のときにメニューから「切り替えキーを変更…」を選んでも、設定ウィンドウが表示される。
- 「ログイン時に起動」をオン・オフできる。

## コーディング規約

- 各言語の標準的な命名規則と書式に従います（Swift: [API Design Guidelines](https://www.swift.org/documentation/api-design-guidelines/)、C#: [.NET のコーディング規則](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)、Rust: `rustfmt`、Python: PEP 8）。インデントなどの基本設定は `.editorconfig` にあります。
- 名前は省略せずに、役割が分かるものにします（`rl`・`br`・`cp` のような略語や、1文字の変数名は使いません。ループの添字も `index` などにします）。
- 数値の意味が分からない定数（キーコードやレジストリのビットなど）は、名前を付けた定数にして、出典や意味をコメントに書きます。
- コメントとユーザーに表示する文字列は日本語で書きます。コメントには「何をしているか」よりも「なぜそうしているか」を書きます。
- Windows 版と変換コマンドの C# 実装は、`TreatWarningsAsErrors` と .NET のコード分析（`AnalysisLevel=latest-recommended`）を有効にしています。警告が出ない状態を保ってください。
- 変換コマンドは、すべての言語で同じ仕様（オプション、終了コード、入出力をバイト単位で保つこと）を守ります。仕様を変えるときはすべての実装と `tests/cases/` を同時に更新します。

### 書式の確認

書式と静的解析は CI（「コーディング規約」ジョブと、macOS アプリのジョブ）で確かめています。Pull Request を送る前に、手元でも次のコマンドで確かめてください。ruff・mypy・ShellCheck・actionlint は、CI と同じ版を `.github/lint/requirements.txt` から導入できます（`pip install --require-hashes --no-deps -r .github/lint/requirements.txt`）。

```sh
# Swift（Xcode に同梱の swift format。設定は macOS/.swift-format）
(cd macOS && swift format lint --strict -r Package.swift PunctuationToggle Scripts Tests)

# C#
dotnet format Windows/PunctuationToggle.sln --verify-no-changes
dotnet format converters/dotnet/PunctuationConverter.csproj --verify-no-changes

# Rust
rustfmt --check --edition 2021 converters/punctuation_converter.rs
clippy-driver --edition 2021 -D warnings -o /tmp/punctuation_converter converters/punctuation_converter.rs
clippy-driver --edition 2021 -D warnings --test -o /tmp/punctuation_converter_test converters/punctuation_converter.rs

# Python
ruff check converters
ruff format --check converters
mypy --strict --python-version 3.10 converters/punctuation_converter.py converters/tests/run_random_tests.py

# シェルスクリプト
git ls-files -z '*.sh' | xargs -0 shellcheck

# PowerShell（CI と同じ版の PSScriptAnalyzer を入れて、PowerShell で実行する）
#   Install-Module PSScriptAnalyzer -RequiredVersion 1.25.0 -Scope CurrentUser
#   Invoke-ScriptAnalyzer -Path Windows -Recurse -Settings .github/lint/PSScriptAnalyzerSettings.psd1

# GitHub Actions のワークフロー（shellcheck が PATH にあれば run: の中も検査する）
actionlint
```

`--verify-no-changes` や `--check` を外す（Swift は `lint --strict` を `format -i` にする）と、指摘された書式をその場で直せます。

## リリースの手順

1. `CHANGELOG.md` に新しいバージョンの項目を追加します。
2. バージョン番号を更新します（macOS: `PunctuationToggle.xcodeproj` の `MARKETING_VERSION` と `CURRENT_PROJECT_VERSION`、Windows: `Windows/Directory.Build.props` の `Version`）。
3. main ブランチで `v1.2.3` のようなタグを付けて push します。タグと手順 2 のバージョン番号が一致しない場合、リリースのワークフローはビルドの前に失敗します。一致していれば、GitHub Actions が macOS 版と Windows 版をビルドし、来歴証明（artifact attestation）を付けて、zip と `SHA256SUMS.txt` を添付した下書きのリリースを作ります。
4. リリースの内容を確認して公開します。添付された zip は、README の「ダウンロードしたファイルの確認」の手順で検証できます。

CI とリリースのランナーは、`windows-latest` のようなラベルではなく、イメージの版（`windows-2025`・`ubuntu-24.04`・`macos-26`）で指定しています。GitHub が `-latest` の指す先を変えても、リポジトリを変えないままビルド環境が変わらないようにするためです。新しいイメージに上げるときは、3つのワークフロー（`ci.yml`・`release.yml`・`security.yml`）の版をそろえて書き換え、Pull Request で CI とリリースのビルド（`release.yml` の Pull Request での実行）が通ることを確かめてからマージしてください。

テストやビルドのジョブは読み取り権限だけで動かし、リリースへの書き込み権限はビルド済みのファイルを添付する最後のジョブにだけ与えています。ワークフローを変更するときもこの分担を保ってください。

配布用ファイルのビルドに影響するファイル（`.github/workflows/release.yml`、`global.json`、`LICENSE`、`Windows/Directory.Build.props`、`Windows/PunctuationToggle/`・`Windows/PunctuationToggle.Core/`・`macOS/PunctuationToggle/`・`macOS/PunctuationToggle.xcodeproj/` 以下）を変更した Pull Request では、リリースは作らずに配布用ファイルのビルドまでが実行され、できた zip を Actions の成果物からダウンロードして確かめられます。条件は `release.yml` の `pull_request.paths` にあります。
