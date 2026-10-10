# 開発ガイド

PunctuationToggle への貢献を歓迎します。不具合の報告や要望は [Issues](https://github.com/Sh1n1230/PunctuationToggle/issues) へ、修正は Pull Request でお送りください。参加にあたっては [行動規範](CODE_OF_CONDUCT.md) を守ってください。

## ディレクトリ構成

```
PunctuationToggle/
├── macOS/                          macOS 版（Swift）
│   ├── PunctuationToggle/          アプリ本体
│   │   └── Core/                   OS に依存しない判定ロジック（単体テストの対象）
│   ├── Tests/                      Core の単体テスト（Swift Testing）
│   ├── Package.swift               Core をテストするための Swift パッケージ
│   ├── PunctuationToggle.xcodeproj
│   └── install.sh / uninstall.sh
├── Windows/                        Windows 版（C# / .NET）
│   ├── PunctuationToggle/          アプリ本体（Windows Forms）
│   ├── PunctuationToggle.Core/     OS に依存しない判定ロジック（単体テストの対象）
│   ├── PunctuationToggle.Tests/    Core の単体テスト（xUnit）
│   ├── PunctuationToggle.sln
│   └── install.ps1 / uninstall.ps1
├── converters/                     句読点の変換コマンド（3言語）とベンチマーク
└── .github/workflows/              CI とリリースの自動化
```

キー入力の判定（切り替えキーの単独押しの検出や、設定画面でのキーの読み取り）は、どちらの OS でも `Core` に OS の API を使わない形で切り出しています。OS の入力イベントは `KeyboardInput` に変換してから `TriggerKeyDetector` / `TriggerKeyCapture` に渡します。判定の仕様を変えるときは、まず `Core` のテストを追加・修正してください。

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

.NET 10 SDK 以降が必要です。`PunctuationToggle.Core` と単体テストは macOS や Linux でも実行でき、アプリ本体もビルド（コンパイルの確認）だけはできます。

```sh
dotnet test Windows/PunctuationToggle.sln
dotnet build Windows/PunctuationToggle.sln
```

テストで使う NuGet パッケージは `Windows/PunctuationToggle.Tests/packages.lock.json` で固定しています。パッケージを追加・更新したときは `dotnet restore Windows/PunctuationToggle.sln` で lock ファイルを更新し、一緒にコミットしてください（CI では lock ファイルと食い違うと失敗します）。

### 変換コマンド

```sh
cd converters
make all csharp-dotnet
make test
```

詳しくは [converters/README.md](converters/README.md) を参照してください。

### 手動での動作確認

キー入力の監視や日本語入力の設定の書き換えは自動テストできないため、アプリの動作を変えたときは次を手で確認してください。

- 切り替えキーの単独押しで「、。」⇔「，．」が切り替わり、メモ帳・テキストエディット・ブラウザなどで入力する句読点が変わる。
- 切り替えキーを使ったショートカット（右Command＋C、右Ctrl＋C など）やクリックでは切り替わらない。
- 「切り替えキーを変更…」で修飾キーとそれ以外のキー（F13、無変換など）を設定でき、アプリを再起動しても設定が残る。文字キーは設定できない。
- 「切り替えキーを変更…」のウィンドウを開いたままほかのアプリに切り替えると、設定ウィンドウが閉じ、そのアプリで普通に文字を入力でき、切り替えキーも元どおり効く。
- 「ログイン時に起動」をオン・オフできる。

## コーディング規約

- 各言語の標準的な命名規則と書式に従います（Swift: [API Design Guidelines](https://www.swift.org/documentation/api-design-guidelines/)、C#: [.NET のコーディング規則](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)、Rust: `rustfmt`、Python: PEP 8）。インデントなどの基本設定は `.editorconfig` にあります。
- 名前は省略せずに、役割が分かるものにします（`rl`・`br`・`cp` のような略語や、1文字の変数名は使いません。ループの添字も `index` などにします）。
- 数値の意味が分からない定数（キーコードやレジストリのビットなど）は、名前を付けた定数にして、出典や意味をコメントに書きます。
- コメントとユーザーに表示する文字列は日本語で書きます。コメントには「何をしているか」よりも「なぜそうしているか」を書きます。
- Windows 版は `TreatWarningsAsErrors` と .NET のコード分析（`AnalysisLevel=latest-recommended`）を有効にしています。警告が出ない状態を保ってください。
- 変換コマンドは、すべての言語で同じ仕様（オプション、終了コード、入出力をバイト単位で保つこと）を守ります。仕様を変えるときはすべての実装と `tests/cases/` を同時に更新します。

## リリースの手順

1. `CHANGELOG.md` に新しいバージョンの項目を追加します。
2. バージョン番号を更新します（macOS: `PunctuationToggle.xcodeproj` の `MARKETING_VERSION` と `CURRENT_PROJECT_VERSION`、Windows: `Windows/Directory.Build.props` の `Version`）。
3. main ブランチで `v1.2.3` のようなタグを付けて push します。GitHub Actions が macOS 版と Windows 版をビルドし、来歴証明（artifact attestation）を付けて、zip と `SHA256SUMS.txt` を添付した下書きのリリースを作ります。
4. リリースの内容を確認して公開します。添付された zip は、README の「ダウンロードしたファイルの確認」の手順で検証できます。

テストやビルドのジョブは読み取り権限だけで動かし、リリースへの書き込み権限はビルド済みのファイルを添付する最後のジョブにだけ与えています。ワークフローを変更するときもこの分担を保ってください。

`.github/workflows/release.yml` を変更した Pull Request では、リリースは作らずに配布用ファイルのビルドまでが実行され、できた zip を Actions の成果物からダウンロードして確かめられます。
