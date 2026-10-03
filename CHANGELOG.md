# 変更履歴

このプロジェクトの主な変更点を記録します。書式は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に、バージョン番号は [セマンティック バージョニング](https://semver.org/lang/ja/) に従います。

## [Unreleased]

### 修正

- Windows で、Python 版の変換コマンドの使い方とエラーが UTF-8 ではなく cp932 で表示される問題を修正しました。
- Windows の Git Bash で `make csharp-dotnet` を実行すると、Linux 向けにビルドしようとする問題を修正しました。
- 変換コマンドのテストで、Python の実装を削除した場合に SKIP ではなく失敗になる問題を修正しました。

### ドキュメント

- 変換コマンドの言語の選定に、Windows（Ryzen 7 9700X）での計測結果を追加し、macOS と Windows のどちらでも成り立つ理由に書き直しました。
- クローンしたあとに、使わない OS 版や変換コマンドの実装を削除してよいことを README に書きました。

## [1.1.0] - 2026-10-03

### 追加

- Windows 版を追加しました。右Ctrl を単独で押すと Microsoft IME の句読点を切り替えます。
- 切り替えキーを自由に変えられるようにしました。メニューの「切り替えキーを変更…」を選び、使いたいキーを押して設定します。修飾キーのほか、ファンクションキーや「英数」「かな」（Windows では「変換」「無変換」など）も選べます。
- 書き終えた文章の句読点をまとめて変換するコマンド（`converters/`）を、C#・Python・Rust で追加しました。`--reverse` で「，．」から「、。」にも変換できます。
- メニューに「PunctuationToggle について」（Windows では「バージョン情報」）を追加しました。
- アンインストール用のスクリプト（`macOS/uninstall.sh`、`Windows/uninstall.ps1`）を追加しました。
- 単体テスト、変換コマンドのテスト、GitHub Actions による CI とリリースの自動化を追加しました。

### 変更

- macOS 版のバンドルIDを `com.example.PunctuationToggle` から `io.github.sh1n1230.PunctuationToggle` に変更しました。v1.0.0 から更新した場合は、アクセシビリティの許可と「ログイン時に起動」を設定し直してください（`macOS/install.sh` は古い許可を自動で削除します）。
- macOS 版の動作環境を macOS 26.5 以降から macOS 26 以降に広げました。
- システム設定で「，。」や「、．」を選んでいる場合、メニューバーにその句読点をそのまま表示するようにしました。
- ディレクトリ構成を `macOS/`、`Windows/`、`converters/` に分けました。

### 修正

- macOS 版で、Command＋クリックや Command＋スクロールのあとに切り替わってしまう問題を修正しました。
- macOS 版で、Shift などほかの修飾キーを押したまま切り替えキーを押すと切り替わってしまう問題を修正しました。
- 「ログイン時に起動」の変更に失敗したときに、エラーを表示するようにしました。

## [1.0.0] - 2026-09-23

### 追加

- 右Command を単独で押して離すと、macOS 標準の日本語入力の句読点を「、。」⇔「，．」に切り替えるメニューバーアプリを公開しました。
- 変換中の未確定文字を壊さずに切り替えられます。
- メニューバーに現在のモードを表示し、メニューからも切り替えられます。
- 「ログイン時に起動」を設定できます。
- 再ビルド後もアクセシビリティの許可が有効になるインストールスクリプト（`install.sh`）を用意しました。

[Unreleased]: https://github.com/Sh1n1230/PunctuationToggle/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/Sh1n1230/PunctuationToggle/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Sh1n1230/PunctuationToggle/releases/tag/v1.0.0
