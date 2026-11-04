/// キーボード・マウスの入力のうち、切り替えキーの判定に必要な情報だけを取り出したもの。
/// `CGEvent` から取り出した値で作る（`init(kind:keyCode:flags:isAutorepeat:)` と `KeyboardMonitor` を参照）。
enum KeyboardInput: Equatable, Sendable {
    /// 修飾キーが押された、または離された。
    /// `otherModifiersHeld` は、このキー以外の修飾キーが押されたままになっているか。
    case modifierChanged(keyCode: UInt16, isPressed: Bool, otherModifiersHeld: Bool)

    /// 修飾キー以外のキーが押された。
    /// `modifiersHeld` は、Command・Shift・Option・Control のいずれかが押されているか。
    case keyDown(keyCode: UInt16, isRepeat: Bool, modifiersHeld: Bool)

    /// 修飾キー以外のキーが離された。
    case keyUp(keyCode: UInt16)

    /// マウスのクリックやスクロール。
    /// Command＋クリックなどを、修飾キーの単独押しと区別するために使う。
    case mouse
}

/// 入力イベントの種類のうち、判定に使うもの。
/// Core は CoreGraphics を使わないため、`CGEventType` の代わりに使う。
enum KeyboardEventKind: Sendable {
    /// `CGEventType.keyDown`
    case keyDown
    /// `CGEventType.keyUp`
    case keyUp
    /// `CGEventType.flagsChanged`（修飾キーの押し下げと離し）
    case flagsChanged
    /// マウスのボタンの押し下げとスクロール
    case mouse
}

/// `CGEventFlags` の生の値のうち、左右を区別しない修飾キーのビット。
/// 値は CoreGraphics の CGEventTypes.h の `kCGEventFlagMask*` と同じ。
enum EventFlagMask {
    static let shift: UInt64 = 0x0002_0000  // kCGEventFlagMaskShift
    static let control: UInt64 = 0x0004_0000  // kCGEventFlagMaskControl
    static let option: UInt64 = 0x0008_0000  // kCGEventFlagMaskAlternate
    static let command: UInt64 = 0x0010_0000  // kCGEventFlagMaskCommand

    /// ショートカットに使う修飾キー（Command・Shift・Option・Control）。
    /// fn は矢印キーなどと一緒に押されるとき macOS が自動で立てるため、含めない。
    static let shortcutModifiers: UInt64 = shift | control | option | command
}

extension KeyboardInput {
    /// 入力イベントから取り出した値で作る。判定に使わない入力（Caps Lock など）では nil を返す。
    /// - Parameters:
    ///   - kind: イベントの種類。
    ///   - keyCode: キーコード（`CGEventField.keyboardEventKeycode`）。マウスでは使わない。
    ///   - flags: 修飾キーのフラグの生の値（`CGEventFlags.rawValue`）。
    ///   - isAutorepeat: キーの押しっぱなしによるリピートか（`CGEventField.keyboardEventAutorepeat`）。
    init?(kind: KeyboardEventKind, keyCode: UInt16, flags: UInt64, isAutorepeat: Bool) {
        switch kind {
        case .flagsChanged:
            guard let modifier = TriggerKey.Modifier(keyCode: keyCode) else { return nil }
            // 左右を区別するため、デバイス依存のビットで、このキーとほかの修飾キーの状態を求める
            let otherModifierMasks = TriggerKey.Modifier.allFlagMasks & ~modifier.flagMask
            self = .modifierChanged(
                keyCode: keyCode,
                isPressed: flags & modifier.flagMask != 0,
                otherModifiersHeld: flags & otherModifierMasks != 0
            )

        case .keyDown:
            self = .keyDown(
                keyCode: keyCode,
                isRepeat: isAutorepeat,
                modifiersHeld: flags & EventFlagMask.shortcutModifiers != 0
            )

        case .keyUp:
            self = .keyUp(keyCode: keyCode)

        case .mouse:
            self = .mouse
        }
    }
}
