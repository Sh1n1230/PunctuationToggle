# PunctuationToggle

[![CI](https://github.com/Sh1n1230/PunctuationToggle/actions/workflows/ci.yml/badge.svg)](https://github.com/Sh1n1230/PunctuationToggle/actions/workflows/ci.yml)
[![Security Check](https://github.com/Sh1n1230/PunctuationToggle/actions/workflows/security.yml/badge.svg)](https://github.com/Sh1n1230/PunctuationToggle/actions/workflows/security.yml)

[最新のリリース](https://github.com/Sh1n1230/PunctuationToggle/releases/latest) ・ ライセンス: [MPL-2.0](LICENSE)

PunctuationToggle は、指定したキーを単独で押すことで、日本語入力の句読点設定を「、。」と「，．」の間で即座に切り替える常駐アプリケーションです。macOS と Windows の双方に対応しています。

公的文書や論文で指定される「，．」と、日常の連絡や一般的な執筆で用いる「、。」を頻繁に切り替える環境において、OS の設定画面を開く手間を省きます。

本ツールは、入力された文字イベントを監視して別の文字へ差し替える方式を採っていません。OS の日本語入力システム（IME）自身が保持する句読点設定を直接更新します。そのため、日本語の変換処理中にある未確定文字列を確定させたり破棄したりすることなく、ターミナルやコードエディタを含むあらゆるアプリケーションで一貫して動作します。

切り替えに用いるキーは任意に変更できます（初期設定は macOS が右 Command、Windows が右 Ctrl）。また、既存のテキストファイルに含まれる句読点を一括して変換するコマンドラインツール（[既存文書の一括変換コマンド](#既存文書の一括変換コマンド)）も用意しています。

| 項目 | macOS | Windows |
|---|---|---|
| 初期設定の切り替えキー | 右Command | 右Ctrl |
| 対象の日本語入力 | macOS 標準の日本語入力 | Microsoft IME |
| 動作環境 | macOS 26 以降 | Windows 11（Windows 10 でも動く見込みですが、動作確認は行っていません） |
| 状態の表示場所 | メニューバー | タスクトレイ |
| 実装言語・場所 | Swift（[`macOS/`](macOS/)） | C# / .NET（[`Windows/`](Windows/)） |

## インストール

アプリは [Releases](https://github.com/Sh1n1230/PunctuationToggle/releases) からダウンロードして使います。リポジトリを取得（clone）したり、ビルドしたりする必要はありません（ソースからビルドするのは開発するときだけです。手順は [CONTRIBUTING.md](CONTRIBUTING.md#ビルドとテスト) にあります）。

### macOS への導入

1. [Releases](https://github.com/Sh1n1230/PunctuationToggle/releases) から `PunctuationToggle-<バージョン>-macOS.zip` をダウンロードします（アーカイブ内には実行ファイル本体と `LICENSE` が含まれます）。
2. ダウンロードしたファイルが本リポジトリでビルドされたものであることを確かめるため、後述の[ダウンロードしたファイルの検証](#ダウンロードしたファイルの検証)を実施してください。
3. アーカイブを展開し、生成された `PunctuationToggle.app` を「アプリケーション」フォルダー（または `~/Applications`）へ移動して起動します。

> [!CAUTION]
> 本ツールはすべてのキー入力を監視する権限を必要とします。必ず本リポジトリの [Releases](https://github.com/Sh1n1230/PunctuationToggle/releases) から入手し、後述の検証を済ませたファイルのみを実行してください。

> [!NOTE]
> 配布しているバイナリは Apple による公証を受けていないため、初回起動時に「開発元を検証できません」という警告が表示されます。ファイルの検証を完了した状態でシステム設定の「プライバシーとセキュリティ」を開き、下部に表示される「このまま開く」を選択してください。

「アプリケーション」フォルダー（または `~/Applications`）以外の場所から起動すると、同フォルダーへの移動を促す案内が表示されます。外部フォルダーのまま運用した場合、OS の仕様によりアクセシビリティの許可が起動ごとに初期化されたり、「ログイン時に起動」が正常に機能しなかったりするためです。

初回起動時にはアクセシビリティの許可を求めるダイアログが表示されます。「システム設定を開く」を選択し、「プライバシーとセキュリティ」→「アクセシビリティ」で PunctuationToggle を有効にしてください。許可が付与されると、メニューバーの表示が警告アイコン（⚠︎）から現在の句読点（「、。」など）へ切り替わります（システムの再起動は不要です）。

### Windows への導入

1. [Releases](https://github.com/Sh1n1230/PunctuationToggle/releases) から、利用環境に合致するアーカイブ（64bit 環境は `win-x64`、Arm 環境は `win-arm64`）をダウンロードします。
2. 後述の[ダウンロードしたファイルの検証](#ダウンロードしたファイルの検証)を実施し、ファイルが改変されていないことを確かめます。
3. 本ツール専用のフォルダー（例: `%LOCALAPPDATA%\Programs\PunctuationToggle`）を作成してアーカイブを展開し、`PunctuationToggle.exe` を起動します。本バイナリは自己完結型（Self-Contained）として出力しているため、.NET ランタイムの事前導入や管理者権限は不要です。

> [!IMPORTANT]
> 「ダウンロード」フォルダーやデスクトップなど、ほかのファイルと混在する場所で直接起動しないでください。Windows は実行ファイルと同じフォルダーに存在する DLL を優先して読み込む仕様を持つため、悪意のある DLL（`version.dll` など）が同居していた場合、キー入力を監視する本プロセスの権限で読み込まれる危険があります。「ログイン時に起動」機能も、起動した場所のパスを登録します。

> [!NOTE]
> コード署名証明書による電子署名を付与していないため、初回起動時に Microsoft Defender SmartScreen による警告が表示される場合があります。ファイルの検証を済ませた後、「詳細情報」をクリックして「実行」を選択してください。

### ダウンロードしたファイルの検証

本ツールはシステム全体のキー入力を監視する権限を要求するため、実行前に配布バイナリの同一性と来歴を検証することを推奨します。v1.1.0 より後のリリースでは GitHub Actions のビルド生成物に対して[来歴証明（artifact attestation）](https://docs.github.com/ja/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations)を付与しており、SHA-256 チェックサム一覧（`SHA256SUMS.txt`）も添付しています。

#### GitHub CLI による来歴証明の検証

[GitHub CLI](https://cli.github.com/)（`gh`）が利用できる環境では、以下のコマンドでバイナリの来歴を直接検証できます。実行結果に `Verification succeeded!` と出力されれば、本リポジトリの公式ワークフローで改変なくビルドされたファイルです。

```sh
gh attestation verify PunctuationToggle-<バージョン>-macOS.zip --repo Sh1n1230/PunctuationToggle
gh attestation verify PunctuationToggle-<バージョン>-win-x64.zip --repo Sh1n1230/PunctuationToggle    # Arm 版は -win-arm64.zip
gh attestation verify PunctuationToggle-<バージョン>-win-x64.cdx.json --repo Sh1n1230/PunctuationToggle  # SBOM（macOS 版は -macOS.cdx.json）
gh attestation verify SHA256SUMS.txt --repo Sh1n1230/PunctuationToggle

# ダウンロードしたフォルダー内の対象ファイルを一括検証する場合
for file in PunctuationToggle-* SHA256SUMS.txt; do gh attestation verify "$file" --repo Sh1n1230/PunctuationToggle; done
```

#### チェックサムによる整合性の検証

GitHub CLI を利用しない場合は、計算した SHA-256 ハッシュ値がリリースページ添付の `SHA256SUMS.txt` と一致するか確認します。ただし、ハッシュ値の一致は手元のファイルが手元の `SHA256SUMS.txt` と同一であることしか証明しません。v1.3.0 より後のリリースでは `SHA256SUMS.txt` 自体にも来歴証明を付与しているため、別の検証可能な端末があれば、照合元ファイルの真正性も確認できます。

```sh
# macOS（ダウンロードしたフォルダーで実行）
shasum -a 256 -c SHA256SUMS.txt --ignore-missing
```

```powershell
# Windows（PowerShell）
$zip = 'PunctuationToggle-<バージョン>-win-x64.zip'  # Arm 版は 'PunctuationToggle-<バージョン>-win-arm64.zip'
$entries = @(Get-Content .\SHA256SUMS.txt | Where-Object {
    $parts = $_ -split '\s+', 2
    $parts.Count -eq 2 -and $parts[1] -eq $zip
})

if ($entries.Count -ne 1) {
    Write-Error "SHA256SUMS.txt に $zip のチェックサムが1件だけ見つかりません。このファイルは実行しないでください。"
} else {
    $expected = ($entries[0] -split '\s+', 2)[0]
    $actual = (Get-FileHash -LiteralPath ".\$zip" -Algorithm SHA256).Hash

    if ($actual -ieq $expected) {
        '一致しました'
    } else {
        Write-Error '一致しません。このファイルは実行しないでください。'
    }
}
```

Windows 版のリリースには、実行ファイルに含まれる .NET ランタイムおよび依存ライブラリの一覧（SBOM。CycloneDX 形式の `PunctuationToggle-<バージョン>-win-x64.cdx.json` など）を添付しています。将来 .NET に脆弱性が報告された際、利用中のバージョンが影響を受けるかを調査できます。SBOM ファイル自体にも来歴証明を付与しています。macOS 版にも同一形式の SBOM（`PunctuationToggle-<バージョン>-macOS.cdx.json`）を添付しています（macOS 版は OS 標準のフレームワークのみを使用しており、外部ライブラリは含みません）。

### 新しいバージョンの確認

本ツールは、更新確認を目的としたネットワーク通信を一切行いません。キー入力を常時監視するアプリケーションがバックグラウンドで外部通信を行わない設計とすることで、安全性の根拠を明確に保つためです。

更新の有無を確認する際は、メニューの「最新版を確認…」を選択してください。既定のブラウザーで[最新リリースページ](https://github.com/Sh1n1230/PunctuationToggle/releases/latest)が開きます。「PunctuationToggle について」（Windows では「バージョン情報」）に表示されるバージョン番号と比較し、新しいリリースが公開されている場合は、手動でダウンロードして置き換えてください。

本ツールは OS の日本語入力が持つ非公開の内部設定に依存しているため、OS の更新によって動作しなくなった場合も、最新バージョンで対応版が提供されていることがあります。

### アンインストール

設定メニューの「ログイン時に起動」を無効化してからアプリケーションを終了し、以下の手順で関連ファイルを削除します。いずれの OS においても、日本語入力システムの句読点設定は最後に切り替えた状態が維持されます。

- **macOS:** アプリケーションを終了し、`PunctuationToggle.app` をゴミ箱へ移動します。アクセシビリティの許可設定と環境設定も消去したい場合は、ターミナルで以下のコマンドを実行してください。

  ```sh
  tccutil reset Accessibility io.github.sh1n1230.PunctuationToggle
  defaults delete io.github.sh1n1230.PunctuationToggle
  ```

- **Windows:** アプリケーションを終了し、展開先のフォルダーを削除します。「ログイン時に起動」の登録とレジストリ設定も消去したい場合は、PowerShell で以下のコマンドを実行してください。

  ```powershell
  Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'PunctuationToggle' -ErrorAction SilentlyContinue
  Remove-Item -Path 'HKCU:\Software\PunctuationToggle' -Recurse -ErrorAction SilentlyContinue
  ```

## 使い方

### 基本的な操作

常駐中の操作と挙動は以下のとおりです。

| 操作 | 動作 |
|---|---|
| 切り替えキーを単独で押して離す（修飾キーの場合） | 「、。」と「，．」を切り替えます。 |
| 切り替えキーを押す（非修飾キーの場合） | 「、。」と「，．」を切り替えます。 |
| 切り替えキーと他のキーの同時押し（例: Command＋C） | 通常の修飾キー付きショートカットとして動作し、切り替えは発生しません。 |
| メニューバーまたはタスクトレイのメニュー | 句読点の切り替え、切り替えキーの変更、ログイン時起動の切り替え、最新版の確認を行います。 |

現在の句読点設定は、メニューバー（macOS）またはタスクトレイ（Windows）に「、。」または「，．」のアイコンとして表示されます。Windows ではタスクトレイアイコンを左クリックすることでも切り替えられます。

なお、OS の設定画面であらかじめ「，。」や「、．」が選択されていた状態で本ツールの切り替えキーを押すと、まず「，．」が適用され、以降は「、。」と「，．」が交互に切り替わります。

### 切り替えキーの変更

メニューの「切り替えキーを変更…」を選択すると、キー入力待機ウィンドウが表示されます。割り当てたいキーを押すことで設定が更新されます。

キーの読み取りは設定ウィンドウが最前面にある間に限り行われます。他のアプリケーションにフォーカスが遷移した場合は変更処理が中止され、設定ウィンドウは自動的に閉じます。Esc キーを押すと変更を中止でき、「初期設定に戻す」ボタンを押すと初期値（macOS は右 Command、Windows は右 Ctrl）へ復帰します。Tab キーでボタン間のフォーカスを移動し、スペースキー（Windows では Enter キーも可）で決定することも可能です（macOS ではシステム設定の「キーボード」→「キーボードナビゲーション」を有効にして操作してください）。

| キー分類 | macOS で選択可能なキー | Windows で選択可能なキー | 動作特性 |
|---|---|---|---|
| 修飾キー | 左右の Command、Shift、Option、Control、fn | 左右の Shift、Ctrl、Alt、Windows | 単独で押して離した瞬間に句読点を切り替えます。他のキーと組み合わせた既存のショートカット入力を阻害しません。 |
| 非修飾キー | F1〜F20、英数、かな | F1〜F24、変換、無変換、アプリケーション、Pause、Scroll Lock | キーを押した瞬間に切り替えます。キー本来の入力イベントは本ツールが消費するため、背後のアプリケーションへは渡されません（修飾キーと併用された場合を除く）。 |

文字入力に用いる一般キー（英数字、スペース、Enter など）は、登録によって日常のテキスト入力が不可能になる事態を防ぐため、選択できない設計にしています。修飾キーを押しながら別のキーを押した場合（Command＋F5 など）も、既存のショートカット操作を優先するため読み取り対象から除外されます。

macOS のファンクションキー（F1〜F12）は、システム設定（「キーボード」→「F1、F2 などのキーを標準のファンクションキーとして使用」）の設定状態により、画面輝度や音量調整などの特殊キーとして認識され設定できない場合があります。この場合は fn キーを押しながら対象キーを押すことで設定できることがあります。

> [!NOTE]
> 「英数」「かな」「変換」「無変換」を切り替えキーに割り当てた場合、キー本来の入力モード切り替え機能は失われます。
> Windows キーや Alt キーを割り当てた場合、単独押しで切り替えた際にスタートメニューやメニューバーが誤って開かないよう、未割り当ての仮想キーコード（0xE8）を送信して他キーとの同時押し状態を擬似的に成立させています。Windows＋E や Alt＋Tab などの組み合わせ操作は従来どおり利用できます。
> macOS で fn（地球儀）キーを割り当てる場合は、システム設定の「キーボード」→「fnキーを押して」（または「地球儀キーを押して」）で設定されている動作（入力ソースの変更や絵文字パレットの表示など）が並行して発火するため、同設定をあらかじめ「何もしない」に変更してください。

### 既存文書の一括変換コマンド

テキストファイル内に含まれる句読点を一括して変換するコマンドラインツールを、C#・Python・Rust の3言語で提供しています（[`converters/`](converters/)）。正規の UTF-8 テキストに対しては全言語の実装で同一の変換結果（バイト列単位で一致）を出力します。詳細な使用方法やベンチマークに基づく言語選定の指針は [converters/README.md](converters/README.md) を参照してください。

変換コマンドは Releases の配布物には含めていません。Python 実装はファイル1つで動作するため、[`converters/punctuation_converter.py`](converters/punctuation_converter.py) のページの「Download raw file」からそのファイルだけをダウンロードすれば、リポジトリを取得しなくても利用できます（Python 3.10 以降が必要です）。

macOS および Linux 環境での実行例は以下のとおりです。Windows 環境での構文については [converters/README.md の「Windows での使い方」](converters/README.md#windows-での使い方) を参照してください。

```sh
# 「、。」を「，．」へ一括変換
python3 punctuation_converter.py < 原稿.txt > 提出用.txt

# 「，．」を「、。」へ逆変換
python3 punctuation_converter.py --reverse < 提出用.txt > 原稿.txt
```

## 動作の仕組み

### macOS

macOS 標準の日本語入力（旧 Kotoeri）は、内部に「句読点の種類」という環境設定項目を保持しています（システム設定の「キーボード」→「入力ソース」→「日本語 - ローマ字入力」で表示される項目と同等です）。PunctuationToggle は GUI 画面を経由せず、OS 内部の設定値とプロセス間通知を直接操作して設定を書き換えます。

1. `CGEventTap` API によりキーボードの入力イベントを監視し、指定された切り替えキーの押下を検出します。修飾キー自体のイベントは破棄せず、そのまま後続の処理系へ流します。マウスのクリックやスクロール操作は、修飾キーを押したままマウス操作が行われたかを判別する目的でのみ、非介入の監視モード（`listenOnly`）で受信します。他アプリケーションがプログラムから生成したキーイベント（`CGEvent.post` など）や、Karabiner-Elements 等で再マッピングされた入力も、物理キー入力と同様に切り替え判定の対象とします（macOS ではイベント送信元プロセスにもアクセシビリティ権限が必須であるため、特権の低いプロセスからの入力を特別に除外する処理は行いません）。
2. 環境設定ドメイン `com.apple.inputmethod.Kotoeri` 内のキー `JIMPrefPunctuationTypeKey` の値を、`0`（「、。」）と `3`（「，．」）の間で書き換えます。
3. システム設定画面での操作時と同一の分散通知 `com.apple.inputmethod.JIM.PreferencesDidChangeNotification` をブロードキャストし、常駐中の日本語入力エンジンに対して設定変更を即座に反映させます。

| `JIMPrefPunctuationTypeKey` の値 | 句読点の組み合わせ |
|---|---|
| 0 | 、。 |
| 1 | ，。 |
| 2 | 、． |
| 3 | ，． |

システム設定で「，。」（1）や「、．」（2）が設定されていた場合、切り替えキーを押すと「，．」（3）へ遷移し、以降は「、。」と「，．」を相互に切り替えます。

本ツール独自のキー割り当て設定は、環境設定ドメイン `io.github.sh1n1230.PunctuationToggle` のキー `TriggerKeyCode` に仮想キーコードとして保存されます。「アプリケーション」フォルダーへの移動案内の非表示設定は、同ドメインの `SuppressMoveToApplicationsAlert` に保持されます。「ログイン時に起動」は `SMAppService` API を介して登録されます。システム設定の「ログイン項目と機能拡張」で無効化されている場合は、メニュー項目に「（システム設定で承認が必要）」と表示し、クリック時に該当の設定画面を開きます。

なお、本ツールが設定を書き換える対象は macOS 標準の日本語入力に限られます。Google 日本語入力や ATOK などのサードパーティ製 IME を使用中にメニューを開いた場合は、句読点の切り替えが機能しない旨をメニュー内に明示します。

### Windows

Microsoft IME の句読点設定は、レジストリキー `HKCU\Software\Microsoft\IME\15.0\IMEJP\MSIME` 配下の DWORD 値 `option1` において、ビット 16〜17 に格納されています。

1. 低レベルキーボードフック（`WH_KEYBOARD_LL`）によりキーストロークを監視し、設定された切り替えキーの押下を検出します。修飾キーのイベントは遮断せず後続へ通過させます。他プロセスからプログラム経由で注入された入力（`SendInput` など）のうち、整合性レベルの低いプロセス（Web ブラウザーのサンドボックスなど）から送信されたイベント（`LLKHF_LOWER_IL_INJECTED`）は、利用者の直接操作ではないため無視して通過させます。PowerToys の Keyboard Manager や AutoHotkey などによる入力は通常権限で送出されるため、切り替えキーとして利用できます。
2. `option1` のビット 16〜17 のみを抽出し、`1`（「、。」）と `0`（「，．」）の間でビット反転書き込みを行います。その他の設定ビットは変更せず維持します。
3. Microsoft IME はウィンドウのフォーカス取得時にレジストリ設定を再読み込みする構造を持つため、画面外に配置した透明ウィンドウへ一瞬だけフォーカスを遷移させて直前のウィンドウへ復帰させ、動作中のアプリケーションに入力設定を即座に再認識させます。

| ビット 16〜17 の値 | 句読点の組み合わせ |
|---|---|
| 0 | ，． |
| 1 | 、。 |
| 2 | 、． |
| 3 | ，。 |

IME の設定画面で「、．」（2）や「，。」（3）が選ばれていた場合、切り替えキーを押すと「，．」（0）へ切り替わり、以降は「、。」と「，．」を行き来します。

本ツール独自の設定値は、レジストリの `HKCU\Software\PunctuationToggle` に保存されます。「ログイン時に起動」は `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` に登録されます。Windows の設定画面やタスクマネージャーの「スタートアップ アプリ」で無効化された状態は、非公開のレジストリ値（`HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run`）に反映されます。本ツールはこの値を読み取るのみで強制的な書き換えは行いません。無効化を検知した場合はメニューに「（Windows の設定で無効）」と表示し、クリック時に Windows の設定画面で有効化するよう案内します。

### 動作を確認した OS 環境

本ツールは日本語入力システムの非公開仕様に依存しているため、OS や IME の大型アップデートに伴い挙動が変化する可能性があります。メンテナーが実機上で主要動作（切り替え、設定保存、再起動後の設定保持）を確認した環境は以下のとおりです。表に記載のない以降のバージョンでも動作する例が多いですが、不具合を確認した場合は Issue にて報告してください。

| OS | バージョン | 日本語入力システム | 確認日 | 確認したアプリのバージョン |
|---|---|---|---|---|
| macOS | 26.7.2 | macOS 標準の日本語入力 | 2026-10-28 | 1.3.0 |
| Windows | Windows 11 Home 26H2（ビルド 26300.9711） | Microsoft IME（新しい Microsoft IME） | 2026-11-07 | 1.3.0 より後の main（コミット ac3aa74） |

※ Windows 環境における確認項目は、切り替え動作、設定保存、二重起動抑止、およびインストールとアンインストールの正常動作です。サインアウトおよび再サインイン後に句読点設定が維持されることについては、現時点で未検証です。

## トラブルシューティングと注意事項

### 既知の制限事項

- **非公開仕様への依存:** 両 OS ともに日本語入力エンジンの非公開設定および内部通知機構を利用しています。将来の OS や IME のアップデートによって動作仕様が変更される可能性があります。
- **終了時の設定維持:** 本アプリケーションを終了した場合でも、日本語入力システムの句読点設定は最後に切り替えた状態のまま残ります。元に戻したい場合は、終了前にキー操作で「、。」に戻すか、OS の設定画面から再設定してください。

### キー入力が反応しない場合の確認事項

#### macOS 環境

- **セキュア入力モード（Secure Event Input）の作動:** パスワード入力欄にフォーカスがある場合や、ターミナル等でセキュア入力モードが有効化されている環境では、セキュリティ保護のため OS がキーイベントのフックを遮断します。この状態では切り替えキーを押しても反応しません。セキュア入力を有効化したアプリケーションがバックグラウンドに回っても同モードを解除しない場合も同様です。メニューを開くと「セキュアキーボード入力が有効です」等の警告が表示されます（メニュー項目からの直接切り替えは引き続き動作します）。
- **アクセシビリティ権限の外れ:** macOS のセキュリティ機能により、アプリケーションの更新時などにアクセシビリティの権限が無効化されることがあります。メニューバーのアイコンが警告表示（⚠︎）になっている場合は、システム設定の「プライバシーとセキュリティ」→「アクセシビリティ」で権限を一度削除し、再登録してください。

#### Windows 環境

- **管理者権限で動作するウィンドウ（UIPI の制約）:** タスクマネージャーなど、管理者権限（昇格特権）で実行されているウィンドウが最前面にある間は、UIPI（User Interface Privilege Isolation）の制約により、一般ユーザー権限で動作する本ツールへキーイベントが届きません。この場合は、一般権限のアプリケーションにフォーカスを戻して切り替え操作を行ってください。
- **未確定文字列の強制確定:** Windows 版では IME に設定変更を認識させるため、画面外の不可視ウィンドウを介した極小時間のフォーカス切り替えを行います。このため、一部のアプリケーションでは入力途中の未確定文字列がその場で確定される場合があります。
- **低レベルフックの解除:** システムの負荷やタイムアウトにより、OS 側から低レベルキーボードフックが予告なく解除されることがあります。切り替えキーが突然反応しなくなった場合は、タスクトレイのアイコンを右クリックしてメニューを開いてください。メニュー表示時にフックの再登録処理が自動で実行され、監視が復帰します（画面ロックの解除時やスリープ復帰時にも自動で再登録されます）。

### 不具合の報告とサポート

予期せぬ不具合を発見した場合や、新しい OS バージョンで動作しなくなった場合は、[GitHub Issues](https://github.com/Sh1n1230/PunctuationToggle/issues) にて報告を受け付けています。

問題を迅速に切り分けるため、報告の際は以下の情報を含めてください。

- お使いの OS とその詳細なバージョン（例: macOS 26.7.2、Windows 11 26H2 ビルド 26300）
- 日本語入力システムの種類とバージョン（例: macOS 標準日本語入力、新しい Microsoft IME）
- PunctuationToggle のバージョン（「PunctuationToggle について」または「バージョン情報」で確認できます）
- 発生した現象、再現手順、および不具合発生時のメニュー表示状態

## 開発と貢献

PunctuationToggle への機能提案、不具合の修正、およびドキュメントの改善といった貢献を歓迎します。

リポジトリを取得してビルドするのは、開発に参加するときだけです。ソースからのビルド手順、Core ロジックの単体テスト実行方法、およびコーディング規約の詳細は [CONTRIBUTING.md](CONTRIBUTING.md) にまとめています。作業に着手する前に必ず一読してください。また、プロジェクトへの参加にあたっては [行動規範（CODE_OF_CONDUCT.md）](CODE_OF_CONDUCT.md) の遵守をお願いしています。

各バージョンの詳細な変更履歴は [CHANGELOG.md](CHANGELOG.md) を参照してください。

## ライセンス

本ソフトウェアは [Mozilla Public License 2.0](LICENSE)（MPL-2.0）のもとで公開されています。

> This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.

MPL-2.0 の規定に基づき、本ソフトウェアのソースコードを改変して頒布する場合は、改変部分のソースコードを同一の MPL-2.0 ライセンスのもとで公開する必要があります（改変を含まない独立した別ファイルや、組み合わされた別コード全体へライセンス義務が波及することはありません）。

なお、v1.3.0 以前のバージョンは MIT License で公開されており、該当バージョンは引き続き MIT License のもとで利用可能です。ライセンス改定前に MIT License で取り込んだ第三者による変更については、[NOTICE.md](NOTICE.md) に原著作者の著作権表示および許諾表示を保持しています。

MPL-2.0 の告知文（Exhibit A）はリポジトリ直下の [LICENSE](LICENSE) に集約しています。ただし、単一ファイルとして独立利用が可能な句読点変換コマンドの各ソースコード（`converters/` 内の `punctuation_converter.py`、`punctuation_converter.rs`、`PunctuationConverter.cs`）については、ファイル先頭に告知文を個別に記載しています。

また、ベンチマーク用の検証データ `converters/benchmark/wikipedia_programming.txt` はウィキペディア日本語版の記事から引用したものであり、[CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/deed.ja) に準拠します（詳細は [出典表記](converters/benchmark/ATTRIBUTION.md) を参照してください）。
