# 開発ガイド

PunctuationToggle への貢献を歓迎します。送っていただいたコードは、本リポジトリと同じ [Mozilla Public License 2.0](LICENSE) のもとで公開されます。不具合の報告や要望は [Issues](https://github.com/Sh1n1230/PunctuationToggle/issues) へ、修正は Pull Request でお送りください。参加にあたっては [行動規範](CODE_OF_CONDUCT.md) を守ってください。

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
│   ├── PunctuationToggle.Tests/    Core の単体テスト（xUnit。macOS・Linux でも実行できる）
│   ├── PunctuationToggle.WindowsTests/  レジストリの読み書きと変更通知のテスト（Windows だけ。HKCU\Software\PunctuationToggle.Tests の下だけを使う）
│   ├── Directory.Packages.props    テストのプロジェクトが使う NuGet パッケージの版
│   ├── Scripts/                    開発用のスクリプト（macOS 版のアイコンの PNG から .ico を作る generate_app_icon.ps1）
│   ├── PunctuationToggle.sln
│   └── install.ps1 / uninstall.ps1   （共通の手順は InstallCommon.ps1）
├── converters/                     句読点の変換コマンド（3言語）とベンチマーク
│   ├── tests/run_tests.sh          全実装に tests/cases/ の入力を与えて出力を比べるテスト（make test）
│   ├── tests/run_random_tests.py   ランダムな入力でのテスト（make test-random）
│   ├── tests/test_sigint.py        入力待ちの Ctrl+C のテスト（run_tests.sh が全実装で実行する）
│   ├── tests/test_punctuation_converter.py  Python 実装の main の単体テスト
│   └── tests/cases/                入力と期待する出力（改行コードを含めてそのまま扱う）
├── testdata/                       両 OS の Core のテストが読む共通のテストベクタ
│                                   （trigger_key_vectors.json。両 OS に共通の判定を変えたら、まずここを直す）
├── docs/                           脅威モデルなどの文書
├── .github/
│   ├── workflows/                  CI・リリース・ベンチマークの自動化
│   ├── actions/                    ワークフローで共通の手順（.NET SDK・Rust の導入）の複合アクション
│   ├── lint/                       CI の「コーディング規約」ジョブのツールの版の固定と設定
│   └── scripts/                    ワークフローから使うスクリプト
├── CLAUDE.md / AGENTS.md           AI のコーディングエージェント向けの指示（CLAUDE.md が正。AGENTS.md は参照だけ）
├── NOTICE.md                       MIT License で受け入れた変更の著作権表示
├── ruff.toml                       Python の ruff の設定
├── global.json                     .NET SDK の版
├── rust-toolchain.toml             Rust の版
└── nuget.config                    NuGet のパッケージソース（nuget.org だけ）
```

キー入力の判定（切り替えキーの単独押しの検出や、設定画面でのキーの読み取り）は、どちらの OS でも `Core` に OS の API を使わない形で切り出しています。OS の入力イベントは `KeyboardInput` に変換してから `TriggerKeyDetector` / `TriggerKeyCapture` に渡します。判定の仕様を変えるときは、まず `Core` のテストを追加・修正してください。両 OS に共通の判定（`TriggerKeyDetector`・`TriggerKeyCapture`）のテストは、`testdata/trigger_key_vectors.json` に入力の列と期待する結果を書き、macOS 版（`TriggerKeyVectorTests.swift`）と Windows 版（`TriggerKeyVectorTests.cs`）の両方のテストが同じものを読みます。片方の OS だけのキーや、入力の列で表せない操作のテストだけを、それぞれのテストに書きます。

### アプリアイコン

macOS 版のアプリアイコンの PNG（`macOS/PunctuationToggle/Assets.xcassets/AppIcon.appiconset/`）は、`macOS/Scripts/generate_app_icon.swift` で描いて作っています。PNG を直接編集せず、スクリプトを直して、リポジトリのルートで `swift macOS/Scripts/generate_app_icon.swift` を実行し、できた PNG をスクリプトと一緒にコミットしてください。句読点はヒラギノ角ゴシック W7（macOS に標準で入っています）で描きます。見つからない場合、スクリプトはファイルを書き換えずにエラーで終わります。スクリプトも CI の `swift format lint` の対象（`Scripts`）です。

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

.NET 10 SDK が必要です。CI とリリースで使う SDK の版は、リポジトリのルートの `global.json` で固定しています（Dependabot が更新を提案します）。手元では、同じ 10.0 の、`global.json` の版以上の SDK が使われます（`rollForward` が `latestFeature`）。`global.json` は作業ディレクトリから上にたどって探されますが、`Windows/install.ps1` はリポジトリのルートに移ってから SDK を確かめて発行するので、ほかのフォルダーから実行しても効きます。`PunctuationToggle.Core` と単体テストは macOS や Linux でも実行でき、アプリ本体もビルド（コンパイルの確認）だけはできます。

```sh
dotnet test Windows/PunctuationToggle.sln
dotnet build Windows/PunctuationToggle.sln
```

CI では、両 OS の Core の単体テストのカバレッジを集計し、Actions の実行結果のサマリーに表示しています（下限は設けていません）。手元では `swift test --enable-code-coverage`（macOS）や `dotnet test Windows/PunctuationToggle.sln --collect:"XPlat Code Coverage"`（Windows）で同じものを作れます。

テストで使う NuGet パッケージの版は `Windows/Directory.Packages.props` にまとめ、テストのプロジェクトごとの `packages.lock.json`（`Windows/PunctuationToggle.Tests/`・`Windows/PunctuationToggle.WindowsTests/`）で固定しています。パッケージを追加・更新したときは `dotnet restore Windows/PunctuationToggle.sln` で lock ファイルを更新し、一緒にコミットしてください（CI では lock ファイルと食い違うと失敗します）。

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
- アプリを2回起動しても、2つ目はすぐ終わり、切り替えキーで2回切り替わらない（macOS は別の場所にあるアプリ同士でも同じ）。
- メニューの「最新版を確認…」で、ブラウザーに最新のリリースのページが開く。
- キーボードだけで、メニューから「切り替えキーを変更…」を開き、キーを読み取り、Esc で中止して閉じられる。
- スクリーンリーダー（VoiceOver・ナレーター）で、メニューバー・タスクトレイの表示が「読点と句点（、。）」などと読み上げられ、設定画面で文字キー（A など）を押すと「使えません」の案内が読み上げられる。

#### macOS 版だけ

- アクセシビリティの許可を外すと、メニューバーの表示が「⚠︎」になり、許可し直すと（最初の1分は1秒以内、その後は10秒以内か、メニューを開いたときに）句読点の表示に戻る。
- 「PunctuationToggle について」や、「ログイン時に起動」に失敗したときのアラートのボタンが日本語で表示される。

#### Windows 版だけ

- タスクトレイのアイコンを左クリックすると切り替わる。
- タスクトレイのメニューを開いて、アクセスキー（「終了(&X)」の X など）を押すと、その項目が実行される。
- タスクバーのライト・ダークを切り替えると、アイコンの文字色が描き直される。
- 表示倍率を変えると、アイコンがその倍率の大きさで描き直される（遅くとも10秒以内）。
- IME の設定画面で句読点を変えると、タスクトレイの表示がすぐ追従する。
- 「ログイン時に起動」をオンにしてから、Windows の設定の「スタートアップ アプリ」で PunctuationToggle をオフにすると、メニューに「（Windows の設定で無効）」と表示され、選ぶと Windows の設定を開くか尋ねられる。
- 切り替えキーで切り替えたとき、入力中のアプリ（メモ帳など）に句読点がすぐ反映される（`FocusBouncer`）。
- ハイコントラストを有効にして設定画面を開くと、案内がテーマの文字色で表示される。

## ブランチとコミット

今の運用を書き起こしたものです。

- main には直接 push せず、ブランチを作って Pull Request を送ります。main のルールセット「protect main」で、Pull Request を経由すること（承認の数は 0）と、CI の次のジョブが成功していることを必須にしています: 「macOS アプリ」「Windows アプリ」「変換コマンド（ubuntu-24.04・macos-26・windows-2025）」「変換コマンド（Python 3.10・Python 3.15）」「コーディング規約」。CI のジョブの名前を変えたり、マトリックスの版を変えたりしたときは、ルールセットの必須のチェックも合わせて変えてください（変えないと、Pull Request がいつまでもマージできません）。リリースのワークフローのジョブ（変更したファイルによって動かないことがある）と Security Check は、必須にしていません。main の削除と force push も禁止しています。
- ブランチ名は `<種類>/<内容を表す英語の短い名前>` にします（例: `fix/windows-trigger-key-save-failure`、`feat/windows-tray-icon-dpi`、`ci/lint`）。リリースは `release/vX.Y.Z` です。
- 種類は [Conventional Commits](https://www.conventionalcommits.org/ja/v1.0.0/) のものを使います: `feat`（機能の追加）・`fix`（不具合の修正）・`docs`（文書）・`test`（テスト）・`refactor`（動作を変えない整理）・`perf`（性能）・`build`（ビルドの設定・依存）・`ci`（CI）・`chore`（そのほか）。
- コミットメッセージと Pull Request のタイトルは、`<種類>: <日本語の説明>` にします（例: `fix: Windows 版で、切り替えキーを保存できなくても異常終了しないようにする`）。説明の句読点は「、。」で、文末の「。」は付けません。リリースの Pull Request は `vX.Y.Z をリリースする` です。
- Dependabot の Pull Request は、`.github/dependabot.yml` の `commit-message` で、どの種類の依存でも `build(deps): …` の形になります。
- 関係する Issue は、Pull Request の本文に `Fixes #45` のように書きます（マージすると Issue が閉じます）。閉じない関係は `Refs #45` と書きます。

### OS や IME を更新したときの確認

本ツールは、macOS の日本語入力の設定（`com.apple.inputmethod.Kotoeri` の `JIMPrefPunctuationTypeKey` と分散通知）と、Microsoft IME の設定（レジストリの `option1` のビット16〜17）という、公開されていない仕様に依存しています。OS の大型アップデートやベータ版の公開のときは、実機で次を確かめ、README の「動作を確かめた OS の版」の表を更新してください。

1. 切り替えキーで「、。」⇔「，．」が切り替わり、メモ（メモ帳）・ブラウザー・ターミナルで入力する句読点が変わる。変換中の未確定の文字が消えない。
2. OS の設定画面（システム設定の日本語入力、Microsoft IME の設定）の句読点の表示も切り替わる。逆に、設定画面で変えると、メニューバー・タスクトレイの表示が追従する。
3. ログアウトしてログインし直す（または再起動する）と、最後に切り替えた句読点が保たれている。
4. 上の「手動での動作確認」の項目。

動かなくなった場合は、設定の保存先や通知の名前が変わっていないか（macOS は `defaults read com.apple.inputmethod.Kotoeri`、Windows は `reg query HKCU\Software\Microsoft\IME\15.0\IMEJP\MSIME`）を確かめ、Issue に記録してください。

## コーディング規約

- 各言語の標準的な命名規則と書式に従います（Swift: [API Design Guidelines](https://www.swift.org/documentation/api-design-guidelines/)、C#: [.NET のコーディング規則](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)、Rust: `rustfmt`、Python: PEP 8）。インデントなどの基本設定は `.editorconfig` にあります。
- 名前は省略せずに、役割が分かるものにします（`rl`・`br`・`cp` のような略語や、1文字の変数名は使いません。ループの添字も `index` などにします）。
- 数値の意味が分からない定数（キーコードやレジストリのビットなど）は、名前を付けた定数にして、出典や意味をコメントに書きます。
- コメントとユーザーに表示する文字列は日本語で書きます。コメントには「何をしているか」よりも「なぜそうしているか」を書きます。
- ソースファイルには、ライセンスの告知を入れません（ルートの LICENSE で示します）。例外は、1つのファイルだけで持ち出して使える変換コマンドの3つの実装で、先頭に `SPDX-License-Identifier: MPL-2.0` と MPL-2.0 の告知（Exhibit A）を入れます。
- 文書（README・CONTRIBUTING など）は、「です・ます」の易しい文体で書きます。「〜である」「〜して差し支えない」のような硬い言い回しや、難しい漢語は避けます。
- 警告はエラーとして扱います。macOS 版はアプリ（`SWIFT_TREAT_WARNINGS_AS_ERRORS`・`GCC_TREAT_WARNINGS_AS_ERRORS`）と Core の `Package.swift`（`treatAllWarnings(as: .error)`）、Windows 版と変換コマンドの C# 実装は `TreatWarningsAsErrors` と .NET のコード分析（`AnalysisLevel=latest-recommended`）を有効にしています。警告が出ない状態を保ってください。
- 変換コマンドは、すべての言語で同じ仕様（オプション、終了コード、入出力をバイト単位で保つこと）を守ります。仕様を変えるときはすべての実装と `tests/cases/` を同時に更新します。
- 変換コマンドの Rust 実装は、外部のクレートを使わず、`#![forbid(unsafe_code)]` で `unsafe` を禁じています。`unsafe` が必要になった場合は、この属性を外す理由を Pull Request で説明してください。

### 書式の確認

書式と静的解析は CI（「コーディング規約」ジョブと、macOS アプリのジョブ）で確かめています。Pull Request を送る前に、手元でも次のコマンドで確かめてください。ruff・mypy・ShellCheck・check-jsonschema は、CI と同じ版を `.github/lint/requirements.txt` から導入できます（`pip install --require-hashes --no-deps --only-binary :all: -r .github/lint/requirements.txt`）。actionlint は、`ci.yml` の「actionlint の導入」に書いた版を [GitHub のリリース](https://github.com/rhysd/actionlint/releases)から入れてください。

```sh
# Rust・Python・シェルスクリプト・Markdown のリンク・通信する API・ワークフロー・設定ファイルのスキーマ・C# を、
# CI と同じ内容でまとめて確かめる（入っていないツールはスキップと表示する。中身は .github/lint/lint.sh）
.github/lint/lint.sh

# Swift（Xcode に同梱の swift format。設定は macOS/.swift-format）
(cd macOS && swift format lint --strict -r Package.swift PunctuationToggle Scripts Tests)

# PowerShell（CI と同じ版の PSScriptAnalyzer を入れて、PowerShell で実行する）
#   Install-Module PSScriptAnalyzer -RequiredVersion 1.25.0 -Scope CurrentUser
#   Invoke-ScriptAnalyzer -Path Windows -Recurse -Settings .github/lint/PSScriptAnalyzerSettings.psd1
```

個々のコマンドは `lint.sh` の中にあります。`--verify-no-changes` や `--check` を外す（Swift は `lint --strict` を `format -i` にする）と、指摘された書式をその場で直せます。

## リリースの手順

1. `CHANGELOG.md` を、[Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) の形で次のように更新します（v1.3.0 の #44 が例です）。
   - `## [Unreleased]` の下にたまった項目を、新しい見出し `## [X.Y.Z] - YYYY-MM-DD`（例: `## [1.3.0] - 2026-10-15`）の下に移し、`## [Unreleased]` は空のまま残します。リリースのワークフローは `## [X.Y.Z]` の見出しを探してリリースノートを作るため、見出しの形を間違えると失敗します。
   - 新しい見出しのすぐ下に、そのリリースの要約を1段落書きます。
   - 変更の種類の見出しは、Keep a Changelog と同じ「追加」「変更」「非推奨」「削除」「修正」「セキュリティ」の順に並べます（無い種類は書きません）。
   - ファイルの末尾の比較リンクで、`[Unreleased]` の比較元を新しいタグにし（`compare/vX.Y.Z...HEAD`）、新しいバージョンのリンク（`[X.Y.Z]: .../compare/v<前のバージョン>...vX.Y.Z`）を追加します。
2. README の「動作を確かめた OS の版」を、リリースする版で確かめた結果に更新します（手順は「OS や IME を更新したときの確認」）。
3. バージョン番号を更新します（macOS: `PunctuationToggle.xcodeproj` の `MARKETING_VERSION` と `CURRENT_PROJECT_VERSION`、Windows: `Windows/Directory.Build.props` の `Version`、変換コマンド: 3つの実装の `VERSION`（`--version` で表示する版））。
4. 手順 1〜3 の変更を Pull Request（ブランチは `release/vX.Y.Z`）で main に入れてから、main で `v1.2.3` のようなタグを付けて push します。リリースのタグ（`v*`）は、ルールセットで削除と付け直しを禁じ（「protect release tags」）、作れるのをリポジトリの管理者だけにしています（「restrict release tag creation」）。付け間違えた場合は、タグを付け直さずに次の版を出してください（来歴証明が、同じ版の名前の違う中身に付かないようにするため）。タグと手順 3 のバージョン番号が一致しない場合、リリースのワークフローはビルドの前に失敗します。一致していれば、GitHub Actions が macOS 版と Windows 版をビルドし、来歴証明（artifact attestation）を付けて、zip と `SHA256SUMS.txt` を添付した下書きのリリースを作ります。
5. リリースの内容を確認して公開します。添付された zip は、README の「ダウンロードしたファイルの確認」の手順で検証できます。

CI とリリースのランナーは、`windows-latest` のようなラベルではなく、イメージの版（`windows-2025`・`ubuntu-24.04`・`macos-26`）で指定しています。GitHub が `-latest` の指す先を変えても、リポジトリを変えないままビルド環境が変わらないようにするためです。新しいイメージに上げるときは、`.github/workflows/` のすべてのワークフロー（今は `ci.yml`・`release.yml`・`security.yml`・`benchmark.yml`。`git grep -n "runs-on:" .github/workflows` で確かめられます）の版をそろえて書き換え、Pull Request で CI とリリースのビルド（`release.yml` の Pull Request での実行）が通ることを確かめてからマージしてください。

各ジョブの `timeout-minutes` は、ハングしたときに既定の 360 分までランナーを使い続けないよう、今の実行時間の数倍にしています。値の横のコメントに、決めた時点の実行時間を書いています。実行時間が上限の半分を超えるようになったら、上限とコメントを見直してください。

テストやビルドのジョブは読み取り権限だけで動かし、リリースへの書き込み権限はビルド済みのファイルを添付する最後のジョブにだけ与えています。ワークフローを変更するときもこの分担を保ってください。

配布用ファイルのビルドに影響するファイル（`.github/workflows/release.yml`、`global.json`、`nuget.config`、`LICENSE`、`Windows/Directory.Build.props`、`Windows/PunctuationToggle/`・`Windows/PunctuationToggle.Core/`・`macOS/PunctuationToggle/`・`macOS/PunctuationToggle.xcodeproj/` 以下、リリースのジョブで動くテスト（`Windows/PunctuationToggle.Tests/`・`Windows/PunctuationToggle.WindowsTests/`・`Windows/Directory.Packages.props`・`Windows/PunctuationToggle.sln`・`macOS/Tests/`・`macOS/Package.swift`・`testdata/`）、版を確かめる変換コマンドの3つのソース）を変更した Pull Request では、リリースは作らずに配布用ファイルのビルドまでが実行され、できた zip を Actions の成果物からダウンロードして確かめられます。条件は `release.yml` の `pull_request.paths` にあります。
