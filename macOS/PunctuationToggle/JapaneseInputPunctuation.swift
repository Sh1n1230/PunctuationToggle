import Foundation

/// macOS 標準の日本語入力の「句読点の種類」設定を読み書きする。
///
/// システム設定の画面で変更したときと同じ通知を送るので、
/// 入力中の日本語入力にも即座に反映され、未確定文字も失われない。
/// 設定キーと通知はどちらも非公開のため、macOS のアップデートで変わる可能性がある。
enum JapaneseInputPunctuation {
    private static let preferencesDomain = "com.apple.inputmethod.Kotoeri" as CFString
    private static let punctuationTypeKey = "JIMPrefPunctuationTypeKey"

    /// 日本語入力の設定が変わったときにシステム設定が送る分散通知
    static let didChangeNotification = Notification.Name(
        "com.apple.inputmethod.JIM.PreferencesDidChangeNotification"
    )
    private static let notificationObject = "com.apple.JIMPreferences"

    static var current: PunctuationStyle {
        CFPreferencesAppSynchronize(preferencesDomain)
        let storedValue = CFPreferencesCopyAppValue(punctuationTypeKey as CFString, preferencesDomain)
        guard let rawValue = storedValue as? Int,
              let style = PunctuationStyle(rawValue: rawValue)
        else {
            return .systemDefault
        }
        return style
    }

    static func set(_ style: PunctuationStyle) {
        CFPreferencesSetAppValue(
            punctuationTypeKey as CFString,
            style.rawValue as CFNumber,
            preferencesDomain
        )
        CFPreferencesAppSynchronize(preferencesDomain)

        // 日本語入力は userInfo に入った値を見て設定を更新する
        DistributedNotificationCenter.default().postNotificationName(
            didChangeNotification,
            object: notificationObject,
            userInfo: [punctuationTypeKey: style.rawValue],
            deliverImmediately: true
        )
    }
}
