# セキュリティポリシー

脆弱性を見つけた場合は、公開の Issue ではなく、GitHub の [Private vulnerability reporting](https://github.com/Sh1n1230/PunctuationToggle/security/advisories/new) から非公開で報告してください。

対象は最新のリリースです。本アプリはキー入力を監視するため（macOS ではアクセシビリティ権限、Windows ではグローバルキーボードフック）、入力内容の取得・保存・送信は一切行わない設計を維持します。

本アプリは、更新の有無の確認を含め、自動で外部と通信しません。新しいバージョンは、利用者がメニューの「最新版を確認…」を選んだときにだけ、ブラウザーで Releases のページを開いて確かめる設計です。
