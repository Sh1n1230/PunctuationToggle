import Foundation

/// 切り替えキーの設定を UserDefaults に保存する。
enum TriggerKeyStore {
    private static let keyCodeDefaultsKey = "TriggerKeyCode"

    static var triggerKey: TriggerKey {
        get {
            let defaults = UserDefaults.standard
            guard defaults.object(forKey: keyCodeDefaultsKey) != nil,
                  let keyCode = UInt16(exactly: defaults.integer(forKey: keyCodeDefaultsKey)),
                  let key = TriggerKey(keyCode: keyCode)
            else {
                return .defaultKey
            }
            return key
        }
        set {
            UserDefaults.standard.set(Int(newValue.keyCode), forKey: keyCodeDefaultsKey)
        }
    }
}
