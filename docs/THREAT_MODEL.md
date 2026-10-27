# 脅威モデル

PunctuationToggle は、すべてのキー入力を監視する権限を持つ常駐アプリです。この文書では、守るもの、想定する攻撃と対策、そして「入力内容を送信しない」ことを第三者が確かめる方法をまとめます。報告の方法と対象は [SECURITY.md](../SECURITY.md) を参照してください。

## 守るもの

| 守るもの | 理由 |
|---|---|
| 利用者のキー入力の内容 | パスワードなどを含む。本アプリはすべての入力を受け取れる |
| キー入力を監視する権限（macOS のアクセシビリティ、Windows の低レベルフック） | 本アプリのプロセスにコードを読み込ませれば、その権限で入力を盗める |
| 日本語入力の設定 | 本アプリが書き換える。ほかの設定を壊すと日本語入力が使えなくなる |
| 配布するファイル | 偽物を配布されると、利用者は同じ手順で権限を与えてしまう |

## 想定する攻撃と対策

| 攻撃 | 対策 |
|---|---|
| 偽物の配布物（改ざんした zip、なりすましたサイトからの配布） | リリースの zip・SBOM・`SHA256SUMS.txt` に来歴証明を付け、README で確かめ方を案内する。ビルドとリリースの書き込みはジョブを分け、外部のコードが動くジョブには書き込み権限を渡さない（`release.yml`） |
| 依存の乗っ取り（GitHub Actions・NuGet・lint のツール） | アクションはコミットの SHA で、lint のツールは版と sha256 で、NuGet は `packages.lock.json` で固定し、NuGet のソースは nuget.org だけにする（`nuget.config`）。Dependabot の更新は公開から7日待つ |
| DYLD の環境変数などによる、macOS 版へのコードの注入 | Hardened Runtime を有効にし（CI とリリースで確かめる）、ライブラリの検証を外す entitlement は付けない |
| DLL の差し替え（DLL プランティング）による、Windows 版へのコードの注入 | P/Invoke の DLL は System32 からだけ読み込む（`DefaultDllImportSearchPaths`）。exe と同じフォルダーの DLL を読み込む apphost の性質があるため、README で専用のフォルダーに展開するよう案内する |
| 権限の低いプロセスが、キー入力を注入して勝手に切り替える | Windows 版は、整合性レベルが低いプロセスが注入した入力（`LLKHF_LOWER_IL_INJECTED`）を判定に使わない |
| アクセシビリティの許可（TCC）の悪用 | アドホック署名ではビルドし直すと許可が無効になるため、`install.sh` は古い許可をリセットしてから求め直す。許可は本アプリのバンドル ID とコード署名に結び付く |
| インストール・アンインストールのスクリプトによる誤った削除 | 削除の前に、想定した場所の、このアプリのものであること、シンボリックリンクでないことを確かめる |

## macOS 版で App Sandbox を有効にしない理由

macOS 版は App Sandbox を無効にしています（`ENABLE_APP_SANDBOX = NO`）。次の3つの機能が、App Sandbox の中では使えないためです。

1. 修飾キー以外の切り替えキー（F13・英数など）の入力を、ほかのアプリに渡さないようにするには、入力を変更できるイベントタップ（`CGEventTapOptions.defaultTap`）が必要で、アクセシビリティの許可を使います。App Sandbox の中で使えるのは、入力を見るだけのタップ（`listenOnly`）です。
2. 日本語入力の設定（ほかのアプリの設定ドメイン `com.apple.inputmethod.Kotoeri`）を書き換えるには、App Sandbox の外にある設定に書き込む必要があります。
3. 日本語入力に設定の変更を知らせる分散通知には、新しい値を `userInfo` に入れて送る必要があります。App Sandbox の中のアプリは、`userInfo` の付いた分散通知を送れません。

App Sandbox の代わりに、Hardened Runtime を有効にし、ネットワークを含む外部との通信を行うコードを持たない（次の節で確かめられる）ことで、権限を悪用されにくくしています。

## 「送信しない」ことの確かめ方

### ソースコード

アプリ本体（`macOS/PunctuationToggle/`・`Windows/PunctuationToggle/`）には、ネットワークを使うコード（`URLSession`・`HttpClient`・ソケットなど）がありません。次のコマンドで何も見つからないことを確かめられます。

```sh
git grep -nE "URLSession|NWConnection|Socket|HttpClient|WebClient|WebRequest|System\.Net" -- macOS/PunctuationToggle Windows/PunctuationToggle Windows/PunctuationToggle.Core
```

例外は、メニューの「最新版を確認…」で Releases のページの URL をブラウザーに渡す処理（`NSWorkspace.open`・`Process.Start`）だけで、通信するのはブラウザーです。

### 配布されたファイル

- macOS: `codesign -dv --verbose=4 /Applications/PunctuationToggle.app` の `CodeDirectory` の行に `runtime`（Hardened Runtime）があること、`codesign -d --entitlements - /Applications/PunctuationToggle.app` に `com.apple.security.cs.allow-dyld-environment-variables` や `com.apple.security.cs.disable-library-validation` が無いことを確かめられます。
- 両方: README の手順で来歴証明を確かめると、そのファイルが公開されたソースコードから GitHub Actions でビルドされたことが分かります。

### 動いているアプリ

アプリを使っている間に、通信していないことを確かめられます。

- macOS: `lsof -i -a -p "$(pgrep -x PunctuationToggle)"` で何も表示されないこと。継続して見る場合は `nettop -p "$(pgrep -x PunctuationToggle)"`。
- Windows（PowerShell）: `Get-NetTCPConnection -OwningProcess (Get-Process PunctuationToggle).Id` と `Get-NetUDPEndpoint -OwningProcess (Get-Process PunctuationToggle).Id` で何も表示されないこと。継続して見る場合は、リソース モニターの「ネットワーク」タブ。

## 診断用のログ

「切り替えが効かない」という報告の切り分けのために、診断用のログを検討しましたが、今は用意していません。キー入力を監視するアプリがファイルに書き出す仕組みを持つこと自体を、必要になるまで避けるためです。不具合の報告では、Issue のテンプレートで OS・アプリ・日本語入力の版と切り替えキーを尋ねています。

将来用意する場合は、次の条件を守ります。

- 既定で無効にし、利用者がメニューから有効にしたときだけ記録する
- 記録するのは、起動・権限の状態の変化・句読点の切り替え（切り替え前後の句読点）・エラーの種類までとし、キーコードなど入力の内容は記録しない
- 保存場所・保持期間・削除の方法を README と SECURITY.md に書く
- 「入力の内容を記録しない」ことを、ログを書き出す関数の引数の型（キーコードを受け取れない形）とテストで確かめる
