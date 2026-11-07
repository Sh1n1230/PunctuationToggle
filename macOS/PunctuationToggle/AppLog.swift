import Foundation
import OSLog

/// 統合ログ（`log stream --predicate 'subsystem == "io.github.sh1n1230.PunctuationToggle"'` で見られる）。
///
/// 記録するのはエラーだけで、ファイルには書き出さない（docs/THREAT_MODEL.md の「診断用のログ」）。
/// キー入力に由来する値（キーコードなど）は、今後も記録しない。
nonisolated enum AppLog {
    static let logger = Logger(
        subsystem: Bundle.main.bundleIdentifier ?? "io.github.sh1n1230.PunctuationToggle",
        category: "app"
    )
}
