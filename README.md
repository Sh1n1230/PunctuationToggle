# PunctuationToggle

右Commandキーを押すだけで、macOS標準の日本語入力の句読点を **「、。」⇔「，．」** に切り替えるメニューバーアプリです。

論文やレポートは「，．」、普段の文章は「、。」というように、システム設定を開かずにその場で切り替えられます。

- サードパーティ製ツール不要（Swift と macOS 標準 API のみ）
- 変換中の未確定文字を壊さずに切り替え可能
- どのアプリでも同じように動作（Terminal、エディタ、ブラウザなど）

## 使い方

| 操作 | 動作 |
|---|---|
| 右Commandを**単独で**押して離す | 「、。」⇔「，．」を切り替え |
| 右Command＋C などのショートカット | 通常どおり動作（切り替えない） |
| 左Command | 何もしない |

現在のモードはメニューバーに「、。」または「，．」と表示されます。メニューからも切り替えられ、「ログイン時に起動」も設定できます。

## 動作環境

- macOS 26 以降（Xcode 26 でビルド）
- macOS 標準の日本語入力（ローマ字入力）

## ビルドとインストール

1. リポジトリをクローンして `PunctuationToggle.xcodeproj` を Xcode で開きます。
2. 必要に応じて、Signing & Capabilities でバンドルID（`com.example.PunctuationToggle`）とチームを自分のものに変更します。
3. Product → Archive、またはコマンドラインでビルドします。

   ```sh
   xcodebuild -project PunctuationToggle.xcodeproj -scheme PunctuationToggle \
     -configuration Release -derivedDataPath build
   cp -R build/Build/Products/Release/PunctuationToggle.app ~/Applications/
   open ~/Applications/PunctuationToggle.app
   ```

4. システム設定 →「プライバシーとセキュリティ」→「アクセシビリティ」で PunctuationToggle を許可します。許可されるとメニューバーの「⚠︎」が「、。」に変わります。

> **注意:** アドホック署名（チーム未設定）でビルドし直すと、アクセシビリティの許可が一覧上はオンのまま無効になることがあります。その場合は一覧から一度削除して、もう一度追加してください。

## 仕組み

macOS 標準の日本語入力には、もともと「句読点の種類」という設定があります（システム設定 → キーボード → 入力ソース → 日本語 - ローマ字入力）。このアプリは、その設定をシステム設定の画面と同じ方法で書き換えています。

1. `CGEventTap` で右Command（キーコード 54）が単独で押されたことを検出します。キー入力は監視するだけで、書き換えません。
2. `com.apple.inputmethod.Kotoeri` ドメインの `JIMPrefPunctuationTypeKey` を `0`（、。）と `3`（，．）の間で切り替えます。
3. システム設定と同じ分散通知 `com.apple.inputmethod.JIM.PreferencesDidChangeNotification` を送り、実行中の日本語入力に即座に反映させます。

キー入力の文字を差し替えるのではなく日本語入力そのものの設定を切り替えるため、変換中の文字列や、Option をメタキーとして使う設定の Terminal などでも正しく動作します。

`JIMPrefPunctuationTypeKey` の値は次のとおりです。

| 値 | 句読点 |
|---|---|
| 0 | 、。 |
| 1 | ，。 |
| 2 | 、． |
| 3 | ，． |

## 注意事項

- 日本語入力の非公開の設定キーと通知を使っているため、今後の macOS アップデートで動かなくなる可能性があります。
- アプリを終了しても、句読点の設定は最後に切り替えた状態のまま残ります。

## ライセンス

[MIT License](LICENSE)
