import Testing
@testable import PunctuationToggleCore

@Suite("入力イベントの値からの変換")
struct KeyboardInputTests {
    // CGEventFlags の生の値。flagsChanged では、左右を区別しないビットとデバイス依存のビットが両方立つ
    private static let rightCommandFlags = EventFlagMask.command | TriggerKey.Modifier.rightCommand.flagMask
    private static let leftShiftFlags = EventFlagMask.shift | TriggerKey.Modifier.leftShift.flagMask
    private static let functionFlag = TriggerKey.Modifier.function.flagMask

    @Test("右Command だけのビットが立った flagsChanged は、ほかの修飾キーなしの押し下げになる")
    func modifierPressedAlone() {
        let input = KeyboardInput(
            kind: .flagsChanged,
            keyCode: KeyCode.rightCommand,
            flags: Self.rightCommandFlags,
            isAutorepeat: false
        )
        #expect(input == .modifierChanged(keyCode: KeyCode.rightCommand, isPressed: true, otherModifiersHeld: false))
    }

    @Test("右Command と左Shift のビットが立っていれば、ほかの修飾キーが押されている")
    func modifierPressedWithOtherModifier() {
        let input = KeyboardInput(
            kind: .flagsChanged,
            keyCode: KeyCode.rightCommand,
            flags: Self.rightCommandFlags | Self.leftShiftFlags,
            isAutorepeat: false
        )
        #expect(input == .modifierChanged(keyCode: KeyCode.rightCommand, isPressed: true, otherModifiersHeld: true))
    }

    @Test("左Command のデバイス依存のビットだけでも、右Command から見てほかの修飾キーが押されている")
    func otherSideOfSameModifier() {
        let input = KeyboardInput(
            kind: .flagsChanged,
            keyCode: KeyCode.rightCommand,
            flags: Self.rightCommandFlags | TriggerKey.Modifier.leftCommand.flagMask,
            isAutorepeat: false
        )
        #expect(input == .modifierChanged(keyCode: KeyCode.rightCommand, isPressed: true, otherModifiersHeld: true))
    }

    @Test("そのキーのビットが消えた flagsChanged は離したことになる")
    func modifierReleased() {
        let input = KeyboardInput(kind: .flagsChanged, keyCode: KeyCode.rightCommand, flags: 0, isAutorepeat: false)
        #expect(input == .modifierChanged(keyCode: KeyCode.rightCommand, isPressed: false, otherModifiersHeld: false))
    }

    @Test("Caps Lock や修飾キーでないキーの flagsChanged は判定に使わない", arguments: [KeyCode.capsLock, 0])
    func unsupportedFlagsChanged(keyCode: UInt16) {
        #expect(KeyboardInput(kind: .flagsChanged, keyCode: keyCode, flags: 0x0001_0000, isAutorepeat: false) == nil)
    }

    @Test(
        "Command・Shift・Option・Control のどれかが押された keyDown は、修飾キーが押されている",
        arguments: [EventFlagMask.command, EventFlagMask.shift, EventFlagMask.option, EventFlagMask.control]
    )
    func keyDownWithShortcutModifier(flags: UInt64) {
        let input = KeyboardInput(kind: .keyDown, keyCode: KeyCode.eisu, flags: flags, isAutorepeat: false)
        #expect(input == .keyDown(keyCode: KeyCode.eisu, isRepeat: false, modifiersHeld: true))
    }

    @Test("fn のビットだけの keyDown は、修飾キーが押されていない")
    func keyDownWithFunctionOnly() {
        // F1〜F12 や矢印キーでは、fn を押していなくても macOS が fn のビットを立てる
        let input = KeyboardInput(kind: .keyDown, keyCode: 122, flags: Self.functionFlag, isAutorepeat: false)
        #expect(input == .keyDown(keyCode: 122, isRepeat: false, modifiersHeld: false))
    }

    @Test("押しっぱなしの keyDown はリピートになる")
    func keyDownRepeat() {
        let input = KeyboardInput(kind: .keyDown, keyCode: KeyCode.kana, flags: 0, isAutorepeat: true)
        #expect(input == .keyDown(keyCode: KeyCode.kana, isRepeat: true, modifiersHeld: false))
    }

    @Test("keyUp とマウスの入力")
    func keyUpAndMouse() {
        #expect(
            KeyboardInput(kind: .keyUp, keyCode: KeyCode.kana, flags: EventFlagMask.command, isAutorepeat: false)
                == .keyUp(keyCode: KeyCode.kana)
        )
        #expect(KeyboardInput(kind: .mouse, keyCode: 0, flags: EventFlagMask.command, isAutorepeat: false) == .mouse)
    }
}
