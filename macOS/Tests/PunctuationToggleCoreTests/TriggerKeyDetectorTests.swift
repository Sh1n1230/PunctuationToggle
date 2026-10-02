import Testing
@testable import PunctuationToggleCore

@Suite("修飾キーを切り替えキーにした場合")
struct ModifierTriggerTests {
    private var detector = TriggerKeyDetector(triggerKey: .rightCommand)

    private func press(_ keyCode: UInt16, otherModifiersHeld: Bool = false) -> KeyboardInput {
        .modifierChanged(keyCode: keyCode, isPressed: true, otherModifiersHeld: otherModifiersHeld)
    }

    private func release(_ keyCode: UInt16, otherModifiersHeld: Bool = false) -> KeyboardInput {
        .modifierChanged(keyCode: keyCode, isPressed: false, otherModifiersHeld: otherModifiersHeld)
    }

    @Test("単独で押して離すと切り替える")
    mutating func tapAlone() {
        #expect(detector.handle(press(KeyCode.rightCommand)) == .passThrough)
        #expect(detector.handle(release(KeyCode.rightCommand)) == InputDecision(shouldToggle: true))
    }

    @Test("押すたびに切り替える")
    mutating func repeatedTaps() {
        for _ in 0..<3 {
            _ = detector.handle(press(KeyCode.rightCommand))
            #expect(detector.handle(release(KeyCode.rightCommand)).shouldToggle)
        }
    }

    @Test("ショートカット（右Command＋C）では切り替えない")
    mutating func shortcut() {
        _ = detector.handle(press(KeyCode.rightCommand))
        #expect(detector.handle(.keyDown(keyCode: 8, isRepeat: false, modifiersHeld: true)) == .passThrough)
        _ = detector.handle(.keyUp(keyCode: 8))
        #expect(detector.handle(release(KeyCode.rightCommand)) == .passThrough)

        // 次の単独押しは切り替える
        _ = detector.handle(press(KeyCode.rightCommand))
        #expect(detector.handle(release(KeyCode.rightCommand)).shouldToggle)
    }

    @Test("押している間にほかの修飾キーを押したら切り替えない")
    mutating func withOtherModifierPressedLater() {
        _ = detector.handle(press(KeyCode.rightCommand))
        _ = detector.handle(press(KeyCode.leftShift, otherModifiersHeld: true))
        _ = detector.handle(release(KeyCode.leftShift, otherModifiersHeld: true))
        #expect(!detector.handle(release(KeyCode.rightCommand)).shouldToggle)
    }

    @Test("ほかの修飾キーを押したまま押した場合は切り替えない")
    mutating func withOtherModifierAlreadyHeld() {
        _ = detector.handle(press(KeyCode.leftShift))
        _ = detector.handle(press(KeyCode.rightCommand, otherModifiersHeld: true))
        #expect(!detector.handle(release(KeyCode.rightCommand, otherModifiersHeld: true)).shouldToggle)
    }

    @Test("Command＋クリックでは切り替えない")
    mutating func withMouse() {
        _ = detector.handle(press(KeyCode.rightCommand))
        _ = detector.handle(.mouse)
        #expect(!detector.handle(release(KeyCode.rightCommand)).shouldToggle)
    }

    @Test("ほかの修飾キーの単独押しでは切り替えない")
    mutating func otherModifier() {
        _ = detector.handle(press(KeyCode.leftCommand))
        #expect(!detector.handle(release(KeyCode.leftCommand)).shouldToggle)
    }

    @Test("押していないのに離された入力だけが来ても切り替えない")
    mutating func releaseWithoutPress() {
        #expect(!detector.handle(release(KeyCode.rightCommand)).shouldToggle)
    }

    @Test("修飾キーの入力は渡さないことがない")
    mutating func neverSuppresses() {
        let inputs: [KeyboardInput] = [
            press(KeyCode.rightCommand),
            .keyDown(keyCode: 8, isRepeat: false, modifiersHeld: true),
            .keyUp(keyCode: 8),
            .mouse,
            release(KeyCode.rightCommand),
            press(KeyCode.rightCommand),
            release(KeyCode.rightCommand),
        ]
        for input in inputs {
            #expect(!detector.handle(input).shouldSuppress)
        }
    }

    @Test("切り替えキーを変えると、押し下げ中の状態は破棄される")
    mutating func changingTriggerKeyResetsState() {
        _ = detector.handle(press(KeyCode.rightCommand))
        detector.triggerKey = TriggerKey(keyCode: KeyCode.rightOption)!
        #expect(!detector.handle(release(KeyCode.rightCommand)).shouldToggle)

        _ = detector.handle(press(KeyCode.rightOption))
        #expect(detector.handle(release(KeyCode.rightOption)).shouldToggle)
    }
}

@Suite("修飾キー以外を切り替えキーにした場合")
struct RegularTriggerTests {
    private static let f13 = KeyCode.functionKeys[12]
    private var detector = TriggerKeyDetector(triggerKey: TriggerKey(keyCode: f13)!)

    @Test("押したときに切り替え、入力は渡さない")
    mutating func press() {
        #expect(
            detector.handle(.keyDown(keyCode: Self.f13, isRepeat: false, modifiersHeld: false))
                == InputDecision(shouldToggle: true, shouldSuppress: true)
        )
        #expect(detector.handle(.keyUp(keyCode: Self.f13)) == InputDecision(shouldSuppress: true))
    }

    @Test("押しっぱなしのリピートでは切り替えないが、入力は渡さない")
    mutating func autoRepeat() {
        _ = detector.handle(.keyDown(keyCode: Self.f13, isRepeat: false, modifiersHeld: false))
        for _ in 0..<5 {
            #expect(
                detector.handle(.keyDown(keyCode: Self.f13, isRepeat: true, modifiersHeld: false))
                    == InputDecision(shouldSuppress: true)
            )
        }
        #expect(detector.handle(.keyUp(keyCode: Self.f13)) == InputDecision(shouldSuppress: true))
    }

    @Test("修飾キーと一緒に押された場合はショートカットとしてそのまま渡す")
    mutating func withModifiers() {
        #expect(detector.handle(.keyDown(keyCode: Self.f13, isRepeat: false, modifiersHeld: true)) == .passThrough)
        #expect(detector.handle(.keyUp(keyCode: Self.f13)) == .passThrough)
    }

    @Test("ほかのキーには影響しない")
    mutating func otherKeys() {
        #expect(detector.handle(.keyDown(keyCode: 0, isRepeat: false, modifiersHeld: false)) == .passThrough)
        #expect(detector.handle(.keyUp(keyCode: 0)) == .passThrough)
        #expect(
            detector.handle(.modifierChanged(keyCode: KeyCode.rightCommand, isPressed: true, otherModifiersHeld: false))
                == .passThrough
        )
        #expect(
            detector.handle(.modifierChanged(keyCode: KeyCode.rightCommand, isPressed: false, otherModifiersHeld: false))
                == .passThrough
        )
    }

    @Test("起動前から押されていたキーのリピートでは切り替えない")
    mutating func repeatWithoutInitialPress() {
        #expect(
            detector.handle(.keyDown(keyCode: Self.f13, isRepeat: true, modifiersHeld: false))
                == InputDecision(shouldSuppress: true)
        )
    }
}
