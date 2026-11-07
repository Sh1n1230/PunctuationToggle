/// 今の入力ソースで、句読点の切り替えが効くかの判定。
///
/// 本アプリが書き換えるのは macOS 標準の日本語入力の設定だけなので、ほかの日本語入力（Google 日本語入力・ATOK など）
/// を使っている間は、メニューバーの表示は変わっても、入力される句読点は変わらない。
/// 英字の入力ソース（ABC など）では句読点を入力しないので、日本語入力に切り替えたときに効けばよく、知らせない。
nonisolated enum InputSourceSupport {
    /// macOS 標準の日本語入力の入力ソース ID の始まり（ローマ字入力・かな入力の各モード）
    static let japaneseInputSourceIDPrefix = "com.apple.inputmethod.Kotoeri."

    /// 日本語の入力ソースを表す言語コード（`kTISPropertyInputSourceLanguages` の値）
    private static let japaneseLanguageCode = "ja"

    /// 句読点が切り替わらない日本語の入力ソースか。
    /// - Parameters:
    ///   - inputSourceID: `kTISPropertyInputSourceID` の値
    ///   - languages: `kTISPropertyInputSourceLanguages` の値（最初のものが主な言語）
    static func isUnsupportedJapaneseInputSource(inputSourceID: String, languages: [String]) -> Bool {
        languages.first == japaneseLanguageCode && !inputSourceID.hasPrefix(japaneseInputSourceIDPrefix)
    }
}
