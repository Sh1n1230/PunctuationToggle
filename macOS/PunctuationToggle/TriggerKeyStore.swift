import Foundation

/// 切り替えキーの設定を UserDefaults に保存する。
enum TriggerKeyStore {
    private static let keyCodeDefaultsKey = "TriggerKeyCode"

    static var triggerKey: TriggerKey {
        get {
            let defaults = UserDefaults.standard
            // integer(forKey:) は、値が無いときも 0 を返すので、値があるかを先に確かめる。
            // 文字列（"54" など、defaults write で型を間違えた場合）も数値に変換される
            let storedKeyCode =
                defaults.object(forKey: keyCodeDefaultsKey) != nil
                ? defaults.integer(forKey: keyCodeDefaultsKey) : nil
            return TriggerKey(storedKeyCode: storedKeyCode)
        }
        set {
            UserDefaults.standard.set(Int(newValue.keyCode), forKey: keyCodeDefaultsKey)
        }
    }
}
