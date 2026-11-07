/// 切り替えキーに使えないキーを押したときに、利用者に示すキーの名前。
///
/// 文字にならないキー（スペース・Return・矢印など）だけを表にしている。
/// 文字キー（A・数字・記号など）は、キーボードの配列によってキーコードと文字の対応が変わるため、
/// アプリ本体で今のキーボード配列での文字を求める（KeyboardLayoutKeyNames）。Windows 版の VirtualKeyNames と同じ分担。
nonisolated enum KeyNames {
    private static let names: [UInt16: String] = [
        KeyCode.returnKey: "Return",
        KeyCode.tab: "Tab",
        KeyCode.space: "スペース",
        KeyCode.delete: "Delete",
        KeyCode.keypadEnter: "Enter",
        KeyCode.help: "Help",
        KeyCode.home: "Home",
        KeyCode.pageUp: "Page Up",
        KeyCode.forwardDelete: "Forward Delete",
        KeyCode.end: "End",
        KeyCode.pageDown: "Page Down",
        KeyCode.leftArrow: "←",
        KeyCode.rightArrow: "→",
        KeyCode.downArrow: "↓",
        KeyCode.upArrow: "↑",
        KeyCode.capsLock: "Caps Lock",
    ]

    /// 表にあるキーの名前。表に無いキーは nil
    static func name(for keyCode: UInt16) -> String? {
        if let digit = KeyCode.keypadDigits.firstIndex(of: keyCode) {
            return "テンキーの \(digit)"
        }
        return names[keyCode]
    }
}
