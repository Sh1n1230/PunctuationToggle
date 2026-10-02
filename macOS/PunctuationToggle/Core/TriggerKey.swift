/// 句読点を切り替えるキー。
///
/// 修飾キー（Command など）は「単独で押して離したとき」に切り替え、ショートカットとしての動作は残す。
/// それ以外のキー（F13 や「英数」など）は、修飾キーなしで押されたときに切り替え、
/// そのキー本来の入力はほかのアプリに渡さない。
///
/// 文字の入力に使うキーを選ぶと文字が打てなくなるため、
/// 選べるのは修飾キー・ファンクションキー・「英数」「かな」に限っている（`supportedKeys`）。
struct TriggerKey: Hashable, Sendable {
    /// macOS の仮想キーコード（`kVK_*`）
    let keyCode: UInt16

    /// 未対応のキーコードでは nil を返す。
    init?(keyCode: UInt16) {
        guard Self.supportedKeys[keyCode] != nil else { return nil }
        self.keyCode = keyCode
    }

    static let rightCommand = TriggerKey(keyCode: KeyCode.rightCommand)!

    /// 初期設定の切り替えキー
    static let defaultKey = rightCommand

    var displayName: String {
        info.displayName
    }

    /// 修飾キーのときはその種類、それ以外は nil
    var modifier: Modifier? {
        info.modifier
    }

    var isModifier: Bool {
        modifier != nil
    }

    // init で supportedKeys に含まれることを確認しているので、必ず見つかる
    private var info: KeyInfo {
        Self.supportedKeys[keyCode]!
    }

    /// メニューに表示する操作の説明
    var usageDescription: String {
        isModifier ? "\(displayName)を単独で押すと切り替え" : "\(displayName)を押すと切り替え"
    }
}

// MARK: - キーの一覧

extension TriggerKey {
    /// 修飾キーの種類と、押されているかを判定するための `CGEventFlags` のビット。
    /// 左右を区別するため、デバイス依存のビット（IOKit の `NX_DEVICE*KEYMASK`）を使う。
    enum Modifier: CaseIterable, Sendable {
        case leftControl, rightControl
        case leftShift, rightShift
        case leftOption, rightOption
        case leftCommand, rightCommand
        case function

        var flagMask: UInt64 {
            switch self {
            case .leftControl: 0x0000_0001   // NX_DEVICELCTLKEYMASK
            case .leftShift: 0x0000_0002     // NX_DEVICELSHIFTKEYMASK
            case .rightShift: 0x0000_0004    // NX_DEVICERSHIFTKEYMASK
            case .leftCommand: 0x0000_0008   // NX_DEVICELCMDKEYMASK
            case .rightCommand: 0x0000_0010  // NX_DEVICERCMDKEYMASK
            case .leftOption: 0x0000_0020    // NX_DEVICELALTKEYMASK
            case .rightOption: 0x0000_0040   // NX_DEVICERALTKEYMASK
            case .rightControl: 0x0000_2000  // NX_DEVICERCTLKEYMASK
            case .function: 0x0080_0000      // kCGEventFlagMaskSecondaryFn
            }
        }

        /// すべての修飾キーのビットを合わせたもの
        static let allFlagMasks: UInt64 = allCases.reduce(0) { masks, modifier in masks | modifier.flagMask }

        /// 修飾キーのキーコードから種類を求める。修飾キーでない場合や Caps Lock は nil。
        init?(keyCode: UInt16) {
            guard let modifier = TriggerKey.supportedKeys[keyCode]?.modifier else { return nil }
            self = modifier
        }
    }

    struct KeyInfo: Sendable {
        let displayName: String
        let modifier: Modifier?
    }

    /// 切り替えキーとして選べるキー（キーコード → 情報）
    static let supportedKeys: [UInt16: KeyInfo] = {
        var keys: [UInt16: KeyInfo] = [
            KeyCode.rightCommand: KeyInfo(displayName: "右Command", modifier: .rightCommand),
            KeyCode.leftCommand: KeyInfo(displayName: "左Command", modifier: .leftCommand),
            KeyCode.leftShift: KeyInfo(displayName: "左Shift", modifier: .leftShift),
            KeyCode.rightShift: KeyInfo(displayName: "右Shift", modifier: .rightShift),
            KeyCode.leftOption: KeyInfo(displayName: "左Option", modifier: .leftOption),
            KeyCode.rightOption: KeyInfo(displayName: "右Option", modifier: .rightOption),
            KeyCode.leftControl: KeyInfo(displayName: "左Control", modifier: .leftControl),
            KeyCode.rightControl: KeyInfo(displayName: "右Control", modifier: .rightControl),
            KeyCode.function: KeyInfo(displayName: "fn", modifier: .function),
            KeyCode.eisu: KeyInfo(displayName: "英数", modifier: nil),
            KeyCode.kana: KeyInfo(displayName: "かな", modifier: nil),
        ]
        for (index, keyCode) in KeyCode.functionKeys.enumerated() {
            keys[keyCode] = KeyInfo(displayName: "F\(index + 1)", modifier: nil)
        }
        return keys
    }()
}

/// macOS の仮想キーコード（Carbon の `kVK_*` と同じ値）
enum KeyCode {
    static let escape: UInt16 = 53
    static let capsLock: UInt16 = 57

    static let rightCommand: UInt16 = 54
    static let leftCommand: UInt16 = 55
    static let leftShift: UInt16 = 56
    static let leftOption: UInt16 = 58
    static let leftControl: UInt16 = 59
    static let rightShift: UInt16 = 60
    static let rightOption: UInt16 = 61
    static let rightControl: UInt16 = 62
    static let function: UInt16 = 63

    /// JIS キーボードの「英数」「かな」
    static let eisu: UInt16 = 102
    static let kana: UInt16 = 104

    /// F1〜F20 の順
    static let functionKeys: [UInt16] = [
        122, 120, 99, 118, 96, 97, 98, 100, 101, 109,
        103, 111, 105, 107, 113, 106, 64, 79, 80, 90,
    ]
}
