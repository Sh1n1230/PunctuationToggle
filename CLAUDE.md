# CLAUDE.md

このリポジトリで作業するときの要点。ビルド・テスト・コーディング規約・リリースの詳細は [CONTRIBUTING.md](CONTRIBUTING.md) にあり、そちらが正。ここには、それを読んだうえで間違えやすい点だけを書く。

## 全体の構成

- 同じ機能（切り替えキーの単独押しで、日本語入力の句読点を「、。」⇔「，．」に切り替える常駐アプリ）を、macOS 版（Swift）と Windows 版（C#）で別々に実装している。
- `converters/` は、書いた文章の句読点を変換するコマンド（Rust・C#・Python）。常駐アプリとは独立している。

## macOS 版と Windows 版の対応

判定ロジックは両 OS で同じ仕様を保つ。片方を変えたら、もう片方とテストも揃える。

| 役割 | macOS（`macOS/PunctuationToggle/`） | Windows（`Windows/`） |
|---|---|---|
| OS に依存しない判定（テスト対象） | `Core/` | `PunctuationToggle.Core/` |
| 入力の抽象 | `Core/KeyboardInput.swift`（修飾キーは `modifierChanged`） | `KeyboardInput.cs`（修飾キーも `KeyDown`/`KeyUp`、リピートあり） |
| 単独押しの検出 | `Core/TriggerKeyDetector.swift` | `TriggerKeyDetector.cs` |
| 設定画面でのキーの読み取り | `Core/TriggerKeyCapture.swift` | `TriggerKeyCapture.cs` |
| 選べるキー | `Core/TriggerKey.swift`（修飾キー9・英数・かな・F1〜F20） | `TriggerKey.cs`（修飾キー8・変換など5・F1〜F24） |
| OS の入力 → `KeyboardInput` | `KeyboardMonitor.swift`（CGEventTap） | `PunctuationToggle/KeyboardHook.cs`（低レベルフック） |
| 句読点設定の読み書き | `JapaneseInputPunctuation.swift`（非公開の設定キーと通知） | `MicrosoftImeSettings.cs` + `MicrosoftImeOption1.cs`（レジストリ option1 のビット16〜17） |
| 切り替えキーの保存 | `TriggerKeyStore.swift`（UserDefaults） | `PunctuationToggle/AppSettings.cs`（HKCU） |
| テスト | `macOS/Tests/`（Swift Testing） | `PunctuationToggle.Tests/`（xUnit） |

- 依存の向きは「アプリ本体 → Core」の一方向。Core から OS の API（AppKit、CoreGraphics、Win32、レジストリ）を使わない。
- 判定の仕様を変えるときは、先に Core のテストを追加・修正する。
- 既知の不具合を記録するテストは、Swift は `withKnownIssue`、xUnit は `[Fact(Skip = "既知の不具合: …")]` で書き、CI を落とさない。直したら外す。

## 作業時の注意

- コメント・テスト名・ユーザーに表示する文字列は日本語。テスト名は日本語の文（Swift は `@Test("…")`、C# はメソッド名そのもの）。
- 名前は省略しない（`index` であって `i` ではない）。意味の分からない数値は名前付きの定数にし、出典をコメントに書く。
- Windows 版は `TreatWarningsAsErrors` と `AnalysisLevel=latest-recommended`。警告 1 つでビルドが失敗する。
- Windows 版のテストのパッケージは `packages.lock.json` で固定している。手元で restore して lock ファイルが変わったら、意図した変更かを確かめる（CI は食い違うと失敗する）。
- 変換コマンドの3実装は、オプション・終了コード（成功 0、読み書きの失敗 1、不正な引数 2）・バイト単位で入出力を保つことを揃える。`converters/tests/cases/**` は改行コードを含めてそのまま扱う（`.gitattributes` で `-text`）。エディタの自動整形で改行を変えない。
- キー入力の監視や日本語入力の設定の書き換えは自動テストできない。アプリの動作を変えたら、CONTRIBUTING.md の「手動での動作確認」を行う（できない場合はそう伝える）。

## 手元の環境で検証できないとき

- Windows 版と C# の変換コマンドは .NET 10 SDK が必要。手元の SDK が古い場合、リポジトリのファイルは書き換えずに、作業用ディレクトリへコピーして `TargetFramework` を読み替えれば、Core のテストと書式の確認だけはできる（Windows Forms のアプリ本体はできない）。その場合は、そうして確認したことを明記する。
- ruff・ShellCheck の版は `.github/lint/requirements.txt` で固定している。
