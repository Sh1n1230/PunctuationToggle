/// macOS 標準の日本語入力の「句読点の種類」設定。
/// rawValue は `com.apple.inputmethod.Kotoeri` ドメインの `JIMPrefPunctuationTypeKey` に保存される値で、
/// システム設定のポップアップの並び順と一致する。
enum PunctuationStyle: Int, CaseIterable, Sendable {
    /// 「、。」
    case toutenKuten = 0
    /// 「，。」
    case commaKuten = 1
    /// 「、．」
    case toutenPeriod = 2
    /// 「，．」
    case commaPeriod = 3

    /// 設定が一度も変更されていないときの値
    static let systemDefault: PunctuationStyle = .toutenKuten

    /// メニューバーなどに表示する、句読点の2文字
    var symbols: String {
        switch self {
        case .toutenKuten: "、。"
        case .commaKuten: "，。"
        case .toutenPeriod: "、．"
        case .commaPeriod: "，．"
        }
    }

    /// 切り替えキーを押したときの切り替え先。
    /// 「，．」なら「、。」に、それ以外（システム設定で選んだ「，。」「、．」を含む）なら「，．」にする。
    var toggled: PunctuationStyle {
        self == .commaPeriod ? .toutenKuten : .commaPeriod
    }
}
