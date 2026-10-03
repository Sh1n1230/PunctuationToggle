/// キーボード・マウスの入力のうち、切り替えキーの判定に必要な情報だけを取り出したもの。
/// `CGEvent` から変換して使う（`KeyboardMonitor` を参照）。
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
