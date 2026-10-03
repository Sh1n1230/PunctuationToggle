# PunctuationToggle

キーを1回押すだけで、日本語入力の句読点を **「、。」⇔「，．」** に切り替える常駐アプリです。macOS と Windows に対応しています。

論文やレポートは「，．」、普段の文章は「、。」というように、設定画面を開かずにその場で切り替えられます。

- キー入力の文字を差し替えるのではなく、日本語入力そのものの「句読点」設定を切り替えるので、どのアプリでも同じように動作します。
- 切り替えキーは自由に変えられます（初期設定は macOS が右Command、Windows が右Ctrl）。
- 書き終えた文章の句読点をまとめて変換するコマンド（[`converters/`](converters/)）も同梱しています。

| | macOS | Windows |
|---|---|---|
| 初期設定の切り替えキー | 右Command | 右Ctrl |
| 対象の日本語入力 | macOS 標準の日本語入力 | Microsoft IME |
| 動作環境 | macOS 26 以降 | Windows 10 / 11 |
| 表示場所 | メニューバー | タスクトレイ |
| 実装 | Swift（[`macOS/`](macOS/)） | C# / .NET（[`Windows/`](Windows/)） |

## 使い方

| 操作 | 動作 |
|---|---|
| 切り替えキーを**単独で**押して離す | 「、。」⇔「，．」を切り替え |
| 切り替えキー＋C などのショートカット | 通常どおり動作（切り替えない） |
| メニューバー / タスクトレイのメニュー | 切り替え、切り替えキーの変更、ログイン時に起動の設定 |

現在のモードはメニューバー（macOS）またはタスクトレイ（Windows）に「、。」「，．」と表示されます。Windows ではタスクトレイのアイコンを左クリックしても切り替えられます。

日本語入力の設定画面で「，。」や「、．」を選んでいる場合は、切り替えキーを押すと「，．」になります。

### 切り替えキーを変更する

メニューの「切り替えキーを変更…」を選び、表示されたウィンドウで使いたいキーを押します。Esc でキャンセル、「初期設定に戻す」で元のキーに戻せます。

| 選べるキー | macOS | Windows | 動作 |
|---|---|---|---|
| 修飾キー | 左右の Command・Shift・Option・Control、fn | 左右の Shift・Ctrl・Alt・Windows | 単独で押して離すと切り替え。ショートカットとしての動作はそのまま |
| そのほかのキー | F1〜F20、英数、かな | F1〜F24、変換、無変換、アプリケーション、Pause、Scroll Lock | 押すと切り替え。そのキー本来の入力はアプリに渡さない（修飾キーと一緒に押した場合を除く） |

文字の入力に使うキー（英字・数字・スペース・Enter など）を選ぶと文字が打てなくなるため、選べないようにしています。

> **補足:** 「英数」「かな」「変換」「無変換」を切り替えキーにすると、そのキー本来の入力切り替えなどの動作は無効になります。Windows キーや Alt キーを単独で押したときのスタートメニューやメニューバーの表示は、そのまま起こります。

## インストール

### macOS

[Releases](https://github.com/Sh1n1230/PunctuationToggle/releases) から `PunctuationToggle-<バージョン>-macOS.zip` をダウンロードして展開し、`PunctuationToggle.app` を「アプリケーション」フォルダに移動して開きます。

> **注意:** 配布しているアプリは Apple の公証を受けていないため、初回は「開発元を検証できません」と表示されて開けません。システム設定 →「プライバシーとセキュリティ」の下部に表示される「このまま開く」を押してください。

起動すると、アクセシビリティの許可を求めるダイアログが表示されます。「システム設定を開く」を選び、「プライバシーとセキュリティ」→「アクセシビリティ」で PunctuationToggle をオンにしてください。許可されるとメニューバーの「⚠︎」が「、。」に変わります（再起動は不要です）。

#### ソースからビルドする場合

Xcode 26 以降が必要です。

```sh
git clone https://github.com/Sh1n1230/PunctuationToggle.git
cd PunctuationToggle
./macOS/install.sh
```

Release ビルド、`~/Applications` へのコピー、古いアクセシビリティ許可のリセット、起動までを行います。その後、上と同じようにアクセシビリティを許可します。

> **一覧に PunctuationToggle が複数ある場合:** 以前にビルドしたアプリの項目が残っています。名前が同じで見分けられないため、すべて選んで「−」で削除してから `./macOS/install.sh` を実行し直し、新しく表示された1つをオンにしてください。

> **注意:** 開発チームを設定せずにビルドするとアドホック署名になり、ビルドし直すたびに署名が変わります。そのため、アクセシビリティの許可が一覧上はオンのまま無効になり、切り替えキーに反応しなくなります。ビルドし直したときは必ず `./macOS/install.sh` を使うか、一覧から PunctuationToggle を削除してから許可し直してください。Xcode から Run したデバッグ版も同じ理由で、許可が引き継がれません。

### Windows

[Releases](https://github.com/Sh1n1230/PunctuationToggle/releases) から、お使いの PC に合った zip（通常は `win-x64`、Arm 版 Windows では `win-arm64`）をダウンロードし、好きな場所に展開して `PunctuationToggle.exe` を実行します。.NET のインストールは不要です。管理者権限やアクセシビリティのような許可も不要です。

> **補足:** 署名していないため、初回は Microsoft Defender SmartScreen の警告が表示されることがあります。「詳細情報」→「実行」を選んでください。

#### ソースからビルドする場合

[.NET 10 SDK](https://dotnet.microsoft.com/download) 以降が必要です。PowerShell で次を実行すると、`%LOCALAPPDATA%\Programs\PunctuationToggle` へのビルドと起動までを行います。

```powershell
git clone https://github.com/Sh1n1230/PunctuationToggle.git
cd PunctuationToggle
powershell -ExecutionPolicy Bypass -File .\Windows\install.ps1
```

この方法でインストールした場合は、実行に [.NET 10 デスクトップ ランタイム](https://dotnet.microsoft.com/download/dotnet/10.0)が必要です（SDK に含まれています）。

## アンインストール

先にメニューの「ログイン時に起動」をオフにしてから、次の手順で削除します。どちらも、日本語入力の句読点の設定は最後に切り替えた状態のまま残ります。

- **macOS:** アプリを終了して `PunctuationToggle.app` を削除します。ソースからインストールした場合は `./macOS/uninstall.sh` でも削除できます（アクセシビリティの許可と設定も削除します）。
- **Windows:** アプリを終了して展開したフォルダを削除します。ソースからインストールした場合は `powershell -ExecutionPolicy Bypass -File .\Windows\uninstall.ps1` で削除できます（設定も削除します）。

## 仕組み

### macOS

macOS 標準の日本語入力には、もともと「句読点の種類」という設定があります（システム設定 → キーボード → 入力ソース → 日本語 - ローマ字入力）。このアプリは、その設定をシステム設定の画面と同じ方法で書き換えています。

1. `CGEventTap` でキー入力を監視し、切り替えキーが押されたことを検出します。修飾キーの入力は書き換えずにそのまま流します。
2. `com.apple.inputmethod.Kotoeri` ドメインの `JIMPrefPunctuationTypeKey` を `0`（、。）と `3`（，．）の間で切り替えます。
3. システム設定と同じ分散通知 `com.apple.inputmethod.JIM.PreferencesDidChangeNotification` を送り、実行中の日本語入力に即座に反映させます。

日本語入力そのものの設定を切り替えるため、変換中の未確定文字を壊さずに切り替えられ、Option をメタキーとして使う設定の Terminal などでも正しく動作します。

| `JIMPrefPunctuationTypeKey` | 句読点 |
|---|---|
| 0 | 、。 |
| 1 | ，。 |
| 2 | 、． |
| 3 | ，． |

### Windows

Microsoft IME の設定画面の「句読点」は、レジストリ `HKCU\Software\Microsoft\IME\15.0\IMEJP\MSIME` の `option1`（DWORD）のビット16〜17に保存されています。

1. 低レベルキーボードフック（`WH_KEYBOARD_LL`）でキー入力を監視し、切り替えキーが押されたことを検出します。修飾キーの入力は書き換えずにそのまま流します。
2. `option1` のビット16〜17だけを `1`（、。）と `0`（，．）の間で書き換えます。ほかのビットの設定は保ちます。
3. Microsoft IME は入力欄にフォーカスが入ったときに設定を読み直すため、画面外の透明なウィンドウに一瞬だけフォーカスを移してすぐ元のウィンドウに戻し、入力中のアプリに反映させます。

| ビット16〜17 | 句読点 |
|---|---|
| 0 | ，． |
| 1 | 、。 |
| 2 | 、． |
| 3 | ，。 |

切り替えキーの設定は `HKCU\Software\PunctuationToggle` に保存します。

## 注意事項

- どちらの OS でも、日本語入力の非公開の設定を使っています。今後の OS や日本語入力のアップデートで動かなくなる可能性があります。
- アプリを終了しても、句読点の設定は最後に切り替えた状態のまま残ります。
- macOS では、パスワード入力欄などでキー入力が保護されている間は切り替えキーに反応しません。
- Windows では、管理者として実行しているアプリ（タスクマネージャーなど）が前面にあるときは、Windows の制限により切り替えキーを検出できません。また、反映のためにフォーカスを一瞬移すので、アプリによっては変換中の未確定文字が確定されることがあります。

## 句読点の変換コマンド

すでに書いた文章の句読点をまとめて変換するコマンドを、C++・C#・Go・Java・Python・Rust・TypeScript で実装しています。使い方と各言語の速度の比較は [converters/README.md](converters/README.md) を参照してください。

```sh
python3 converters/punctuation_converter.py < 原稿.txt > 提出用.txt        # 「、。」→「，．」
python3 converters/punctuation_converter.py --reverse < 提出用.txt > 原稿.txt  # 「，．」→「、。」
```

## 開発

ビルド方法、テストの実行方法、コーディング規約は [CONTRIBUTING.md](CONTRIBUTING.md) を参照してください。変更履歴は [CHANGELOG.md](CHANGELOG.md) にあります。

## ライセンス

[MIT License](LICENSE)

ただし、ベンチマーク用のデータ `converters/benchmark/wikipedia_programming.txt` はウィキペディアの記事から引用したもので、[CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/deed.ja) です（[出典](converters/benchmark/ATTRIBUTION.md)）。
