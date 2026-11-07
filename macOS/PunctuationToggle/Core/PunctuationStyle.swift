/// macOS 標準の日本語入力の「句読点の種類」設定。
/// rawValue は `com.apple.inputmethod.Kotoeri` ドメインの `JIMPrefPunctuationTypeKey` に保存される値で、
/// システム設定のポップアップの並び順と一致する。
nonisolated enum PunctuationStyle: Int, CaseIterable, Sendable {
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

    /// 日本語入力の設定ファイルから読んだ値（`JIMPrefPunctuationTypeKey` を整数として読んだもの）から求める。
    /// 値が無い・範囲外の場合は、日本語入力と同じく `systemDefault` とみなす
    init(storedValue: Int?) {
        self = storedValue.flatMap(PunctuationStyle.init(rawValue:)) ?? .systemDefault
    }

    /// メニューバーなどに表示する、句読点の2文字
    var symbols: String {
        switch self {
        case .toutenKuten: "、。"
        case .commaKuten: "，。"
        case .toutenPeriod: "、．"
        case .commaPeriod: "，．"
        }
    }

    /// VoiceOver などで読み上げる名前。
    /// 記号だけ（「、。」）では読み上げで区別しにくいため、句読点の呼び名を添える
    var spokenName: String {
        switch self {
        case .toutenKuten: "読点と句点（、。）"
        case .commaKuten: "コンマと句点（，。）"
        case .toutenPeriod: "読点とピリオド（、．）"
        case .commaPeriod: "コンマとピリオド（，．）"
        }
    }

    /// 切り替えキーを押したときの切り替え先。
    /// 「，．」なら「、。」に、それ以外（システム設定で選んだ「，。」「、．」を含む）なら「，．」にする。
    var toggled: PunctuationStyle {
        self == .commaPeriod ? .toutenKuten : .commaPeriod
    }
}
