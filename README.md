# PunctuationToggle

キーの単独押下で、日本語入力の句読点設定を「、。」と「，．」の間で即座に切り替える常駐アプリケーションです。macOS と Windows の双方に対応しています。

公的文書や論文で用いる「，．」と、日常の連絡や執筆で用いる「、。」を使い分ける際、OS の設定画面を開かずにその場で切り替えられます。

本ツールは、入力された文字をフックして差し替える方式ではなく、OS の日本語入力システムが保持する句読点の設定そのものを切り替えます。そのため、変換中の未確定文字列を崩さず、ターミナルやコードエディタなどを含めた任意のアプリケーションで一貫して動作します。

切り替えキーは任意に変更できます（初期設定は macOS が右 Command、Windows が右 Ctrl）。また、既存の文章の句読点を一括変換するコマンドラインツール（[`converters/`](converters/)）も同梱しています。

| 項目 | macOS | Windows |
|---|---|---|
| 初期設定の切り替えキー | 右Command | 右Ctrl |
| 対象の日本語入力 | macOS 標準の日本語入力 | Microsoft IME |
| 動作環境 | macOS 26 以降 | Windows 10 / 11 |
| 表示場所 | メニューバー | タスクトレイ |
| 実装 | Swift（[`macOS/`](macOS/)） | C# / .NET（[`Windows/`](Windows/)） |

## 使い方

| 操作 | 動作 |
|---|---|
| 切り替えキーを単独で押して離す（修飾キーの場合） | 「、。」と「，．」を切り替え |
| 切り替えキーを押す（非修飾キーの場合） | 「、。」と「，．」を切り替え |
| 切り替えキーと他のキーの組み合わせ（例: Command＋C） | 通常のショートカットとして動作（切り替えは発生しない） |
| メニューバーまたはタスクトレイのメニュー | 句読点の切り替え、切り替えキーの変更、ログイン時起動の設定 |

現在の設定は、メニューバー（macOS）またはタスクトレイ（Windows）に「、。」または「，．」のアイコンとして表示されます。Windows ではタスクトレイのアイコンを左クリックしても切り替えられます。

なお、OS の設定画面で「，。」や「、．」を選択している状態で切り替えキーを押すと「，．」に切り替わり、以後は「、。」と「，．」が交互に切り替わります。

### 切り替えキーの変更

メニューの「切り替えキーを変更…」を選択し、表示された設定ウィンドウで割り当てたいキーを押します（キーを読み取るのは設定ウィンドウが前面にある間だけです。ほかのアプリに切り替えると、変更は中止されて設定ウィンドウが閉じます）。Esc キーで変更を中止でき、「初期設定に戻す」ボタンで初期キー（macOS は右 Command、Windows は右 Ctrl）に復帰します。

| キー分類 | macOS | Windows | 動作の特性 |
|---|---|---|---|
| 修飾キー | 左右の Command、Shift、Option、Control、fn | 左右の Shift、Ctrl、Alt、Windows | 単独で押して離したときに切り替えます。他のキーと組み合わせたショートカット入力は阻害しません。 |
| 非修飾キー | F1〜F20、英数、かな | F1〜F24、変換、無変換、アプリケーション、Pause、Scroll Lock | キーを押した瞬間に切り替えます。キー本来の入力イベントは消費され、背後のアプリケーションには渡されません（修飾キーと併用した場合を除く）。 |

文字入力に用いるキー（英数字、スペース、Enter など）を登録すると通常のテキスト入力が妨げられるため、選択できない設計にしています。

> [!NOTE]
> 「英数」「かな」「変換」「無変換」を切り替えキーに割り当てた場合、キー本来の入力モード切り替え動作は無効になります。また、Windows キーや Alt キーを修飾キーとして割り当てた場合、キー単独押下に伴うスタートメニューやメニューバーの表示は OS の仕様上そのまま発生します。

## インストール

### ダウンロードしたファイルの確認

本ツールはすべてのキー入力を監視する権限を必要とするため、入手したファイルが本リポジトリでビルドされたものであることを、起動する前に確認してください。v1.1.0 より後のリリースでは、GitHub Actions がビルドした各ファイルに[来歴証明（artifact attestation）](https://docs.github.com/ja/actions/security-for-github-actions/using-artifact-attestations/using-artifact-attestations-to-establish-provenance-for-builds)を付け、SHA-256 のチェックサムを `SHA256SUMS.txt` として添付しています。

[GitHub CLI](https://cli.github.com/) を利用できる場合は、次のコマンドで来歴証明を検証します。`Verification succeeded!` と表示されれば、本リポジトリのリリース用ワークフローでビルドされたファイルです。

```sh
gh attestation verify PunctuationToggle-<バージョン>-macOS.zip --repo Sh1n1230/PunctuationToggle
```

GitHub CLI を利用しない場合は、チェックサムを計算し、同じリリースの `SHA256SUMS.txt` に記載された値と一致することを確認します。

```sh
# macOS（ダウンロードしたフォルダーで実行）
shasum -a 256 -c SHA256SUMS.txt --ignore-missing
```

```powershell
# Windows（PowerShell）
Get-FileHash .\PunctuationToggle-<バージョン>-win-x64.zip -Algorithm SHA256
```

> [!CAUTION]
> 以下で案内している警告の回避操作は、上記の確認を済ませたファイルに対してのみ行ってください。本リポジトリの [Releases](https://github.com/Sh1n1230/PunctuationToggle/releases) 以外から入手したファイルは実行しないでください。

### macOS

[Releases](https://github.com/Sh1n1230/PunctuationToggle/releases) から `PunctuationToggle-<バージョン>-macOS.zip` をダウンロードして展開し、生成された `PunctuationToggle.app` を「アプリケーション」フォルダーへ移動して起動します。

> [!NOTE]
> 配布しているバイナリは Apple の公証を受けていないため、初回起動時に「開発元を検証できません」という警告が表示されます。[ダウンロードしたファイルの確認](#ダウンロードしたファイルの確認)を済ませたうえで、システム設定の「プライバシーとセキュリティ」画面を開き、下部に表示される「このまま開く」を選択してください。

起動後、アクセシビリティの許可を求めるダイアログが表示されます。「システム設定を開く」を選択し、「プライバシーとセキュリティ」→「アクセシビリティ」で PunctuationToggle を有効にしてください。許可が付与されると、メニューバーの表示が警告アイコン（⚠︎）から現在の句読点（「、。」など）へ切り替わります（システムの再起動は不要です）。

#### ソースからビルドする場合

Xcode 26 以降が必要です。リポジトリを取得後、付属のインストールスクリプトを実行します。

```sh
git clone https://github.com/Sh1n1230/PunctuationToggle.git
cd PunctuationToggle
rm -rf Windows  # Windows 版のため不要
./macOS/install.sh
```

このスクリプトは、Release ビルドの生成、`~/Applications` への配置、古いビルドに由来するアクセシビリティ許可のリセット、およびアプリの起動までを一括して処理します。起動後は、配布版と同様にアクセシビリティの許可を有効にします。

> [!WARNING]
> 開発チームを指定せずにローカルでビルドした場合、バイナリはアドホック署名となります。ビルドし直すたびに署名識別子が変化するため、システム設定の一覧上で許可がオンになっていても内部的には権限が無効化され、キー入力を検知できなくなります。ソースから再ビルドした際は、必ず `./macOS/install.sh` を再実行するか、設定一覧から古い PunctuationToggle を削除したうえで登録し直してください。設定一覧に同名項目が重複している場合も、一度すべて削除してから本スクリプトを実行することで解消します。

### Windows

[Releases](https://github.com/Sh1n1230/PunctuationToggle/releases) から、利用環境に合致するアーカイブ（64bit 環境は `win-x64`、Arm 版 Windows は `win-arm64`）をダウンロードし、本ツール専用のフォルダー（例: `%LOCALAPPDATA%\Programs\PunctuationToggle`）に展開して `PunctuationToggle.exe` を起動します。自己完結型（Self-Contained）バイナリとして出力しているため、.NET ランタイムの事前導入や管理者権限の付与は不要です。

> [!IMPORTANT]
> 「ダウンロード」フォルダーやデスクトップなど、ほかのファイルと混在するフォルダーで直接起動しないでください。Windows は exe と同じフォルダーにある DLL を優先して読み込むため、同じフォルダーに悪意のある DLL（`version.dll` など）が置かれていると、キー入力を監視する本ツールのプロセスに読み込まれるおそれがあります。「ログイン時に起動」も、起動した場所の exe を登録します。

> [!NOTE]
> コード署名証明書による署名を行っていないため、初回起動時に Microsoft Defender SmartScreen による警告が表示される場合があります。[ダウンロードしたファイルの確認](#ダウンロードしたファイルの確認)を済ませたうえで、「詳細情報」をクリックし、「実行」を選択してください。

#### ソースからビルドする場合

[.NET 10 SDK](https://dotnet.microsoft.com/download) 以降が必要です。PowerShell からインストールスクリプトを実行すると、ビルドから `%LOCALAPPDATA%\Programs\PunctuationToggle` への配置および起動までを処理します。

```powershell
git clone https://github.com/Sh1n1230/PunctuationToggle.git
cd PunctuationToggle
Remove-Item -Recurse -Force macOS  # macOS 版のため不要
powershell -ExecutionPolicy Bypass -File .\Windows\install.ps1
```

ソースからビルドして実行する場合、実行環境に [.NET 10 デスクトップ ランタイム](https://dotnet.microsoft.com/download/dotnet/10.0)（SDK に同梱）が導入されている必要があります。

### 不要なコードの削除

本リポジトリには、macOS 版（Swift、`macOS/`）、Windows 版（C#、`Windows/`）、および句読点変換コマンド（Rust・C#・Python、`converters/`）が含まれていますが、各コンポーネントは互いに独立しており依存関係を持ちません。ソースコードからビルドして利用する際は、不要なフォルダーを削除して管理できます。

- Windows 環境では `macOS/` は不要です（macOS 環境では `Windows/` が不要です）。前述のビルド手順に示すとおり、Windows 環境では PowerShell の `Remove-Item -Recurse -Force macOS`（PowerShell における `rm` は `Remove-Item` のエイリアスであり、`-rf` 引数を解釈しないため）を用いて削除できます。
- 句読点変換コマンドを利用しない場合は `converters/` ディレクトリ全体を削除できます。利用する場合でも、利用対象とする言語の実装1つ（たとえば Rust を使用する場合は `punctuation_converter.rs` のみ）を残し、他の実装ファイルを削除して差し支えありません（詳細は [使う実装だけを残す](converters/README.md#使う実装だけを残す) を参照してください）。

> [!NOTE]
> 本リポジトリに対して Pull Request を作成する場合は、ローカル環境における不要コードの削除をコミットに含めないでください。

## アンインストール

設定メニューの「ログイン時に起動」を無効にしてから、以下の手順でファイルを削除します。なお、いずれの OS でも、日本語入力システムの句読点設定は最後に切り替えた状態が維持されます。

- **macOS:** アプリケーションを終了し、`PunctuationToggle.app` をゴミ箱へ移動して削除します。ソースから導入した場合は、`./macOS/uninstall.sh` を実行することで、バイナリに加えてアクセシビリティの許可設定および環境設定も一括して削除できます。
- **Windows:** アプリケーションを終了し、展開したフォルダーを削除します。ソースから導入した場合は、`powershell -ExecutionPolicy Bypass -File .\Windows\uninstall.ps1` を実行することで、配置先フォルダーおよびレジストリの設定情報を一括して削除できます。

## 動作の仕組み

### macOS

macOS 標準の日本語入力（旧 Kotoeri）には、「句読点の種類」という設定項目が用意されています（システム設定 → キーボード → 入力ソース → 日本語 - ローマ字入力）。PunctuationToggle は、GUI の設定画面を介さずに、OS 内部の設定値と通知機構を用いてこの設定を直接更新します。

1. `CGEventTap` API によりキーボード入力を監視し、指定された切り替えキーの押下を検出します。修飾キー自体の入力イベントは消費せず、そのまま後続の処理系へ流します。
2. `com.apple.inputmethod.Kotoeri` ドメインの環境設定キー `JIMPrefPunctuationTypeKey` の値を、`0`（「、。」）と `3`（「，．」）の間で書き換えます。
3. システム設定画面の変更時と同一の分散通知 `com.apple.inputmethod.JIM.PreferencesDidChangeNotification` をブロードキャストし、動作中の日本語入力エンジンに設定変更を即時反映させます。

IME の内部設定を直接更新するため、テキスト入力中の未確定文字列が破棄されることはありません。また、Terminal などで Option キーをメタキーとして割り当てている環境でも、キー入力を阻害することなく動作します。

| `JIMPrefPunctuationTypeKey` の値 | 句読点の組み合わせ |
|---|---|
| 0 | 、。 |
| 1 | ，。 |
| 2 | 、． |
| 3 | ，． |

### Windows

Microsoft IME の句読点設定は、レジストリキー `HKCU\Software\Microsoft\IME\15.0\IMEJP\MSIME` 内の DWORD 値 `option1` において、ビット 16〜17 に格納されています。

1. 低レベルキーボードフック（`WH_KEYBOARD_LL`）によりキーストロークを監視し、設定された切り替えキーの押下を検出します。修飾キーの入力イベントは遮断せずに通過させます。
2. `option1` のビット 16〜17 のみを抽出し、`1`（「、。」）と `0`（「，．」）の間で反転書き込みを行います。その他の設定ビットは元の値を保持します。
3. Microsoft IME はウィンドウのフォーカス取得時にレジストリ設定を再読み込みする構造を持つため、画面外に配置した透明ウィンドウへ一瞬だけフォーカスを遷移させて直前のウィンドウへ復帰させ、動作中のアプリケーションに入力設定を即座に再認識させます。

| ビット 16〜17 の値 | 句読点の組み合わせ |
|---|---|
| 0 | ，． |
| 1 | 、。 |
| 2 | 、． |
| 3 | ，。 |

本アプリケーション独自の切り替えキー設定などは、レジストリの `HKCU\Software\PunctuationToggle` に保存されます。

## 注意事項

- 両 OS ともに日本語入力エンジンの非公開の内部設定および通知機構を利用しています。OS や IME の将来の更新によって動作仕様が変更される可能性があります。
- アプリケーションを終了した場合でも、句読点の設定は最後に切り替えた状態のまま保持されます。
- macOS では、パスワード入力欄など Secure Event Input（セキュア入力モード）が有効化されている状況下では、セキュリティ保護のためキーイベントの監視が行えず、切り替えキーに反応しません。
- Windows では、UIPI（User Interface Privilege Isolation）の制約により、タスクマネージャーなど管理者権限で実行されているウィンドウが前面にある間は、標準権限で動作する本ツールから切り替えキーを検出できません。また、設定反映のためにウィンドウフォーカスの一時的な移動を行うため、一部のアプリケーションでは入力途中の未確定文字列がその場で確定される場合があります。

## 句読点の一括変換コマンド

作成済みのテキストファイルに含まれる句読点を一括して変換するコマンドラインツールを、C#・Python・Rust の3言語で提供しています。全実装で仕様と出力結果のバイト列が一致しているため、利用環境に合わせていずれか1つを選択すれば動作します。詳しい使い方、および macOS と Windows の双方で実施したベンチマークに基づく言語選定の理由は [converters/README.md](converters/README.md) を参照してください。

```sh
# 「、。」を「，．」へ変換
python3 converters/punctuation_converter.py < 原稿.txt > 提出用.txt

# 「，．」を「、。」へ逆変換
python3 converters/punctuation_converter.py --reverse < 提出用.txt > 原稿.txt
```

## 開発

ビルド手順、テストの実行方法、およびコーディング規約は [CONTRIBUTING.md](CONTRIBUTING.md) を参照してください。バージョンの推移と更新内容は [CHANGELOG.md](CHANGELOG.md) に記録されています。

## ライセンス

本ソフトウェアは [MIT License](LICENSE) のもとで公開されています。

ただし、ベンチマーク用の検証データ `converters/benchmark/wikipedia_programming.txt` はウィキペディア日本語版の記事から引用したものであり、[CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/deed.ja) に準拠します（詳細は [出典表記](converters/benchmark/ATTRIBUTION.md) を参照してください）。
