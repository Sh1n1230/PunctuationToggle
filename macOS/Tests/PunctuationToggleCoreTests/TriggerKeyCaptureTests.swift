import Testing
@testable import PunctuationToggleCore

@Suite("切り替えキーの読み取り")
struct TriggerKeyCaptureTests {
    private var capture = TriggerKeyCapture()

    @Test("修飾キーは単独で押して離したときに読み取る")
    mutating func modifierTap() {
        let pressed = capture.handle(
            .modifierChanged(keyCode: KeyCode.rightOption, isPressed: true, otherModifiersHeld: false)
        )
        #expect(pressed == .init(outcome: .waiting, shouldSuppress: false))

        let released = capture.handle(
            .modifierChanged(keyCode: KeyCode.rightOption, isPressed: false, otherModifiersHeld: false)
        )
        #expect(released == .init(outcome: .captured(TriggerKey(keyCode: KeyCode.rightOption)!), shouldSuppress: false))
    }

    @Test("修飾キーの組み合わせは読み取らない")
    mutating func modifierCombination() {
        _ = capture.handle(.modifierChanged(keyCode: KeyCode.leftShift, isPressed: true, otherModifiersHeld: false))
        _ = capture.handle(.modifierChanged(keyCode: KeyCode.rightCommand, isPressed: true, otherModifiersHeld: true))
        #expect(
            capture.handle(.modifierChanged(keyCode: KeyCode.rightCommand, isPressed: false, otherModifiersHeld: true))
                .outcome == .waiting
        )
        #expect(
            capture.handle(.modifierChanged(keyCode: KeyCode.leftShift, isPressed: false, otherModifiersHeld: false))
                .outcome == .waiting
        )
    }

    @Test("修飾キーを押したままクリックした場合は読み取らない")
    mutating func modifierWithMouse() {
        _ = capture.handle(.modifierChanged(keyCode: KeyCode.rightCommand, isPressed: true, otherModifiersHeld: false))
        _ = capture.handle(.mouse)
        #expect(
            capture.handle(.modifierChanged(keyCode: KeyCode.rightCommand, isPressed: false, otherModifiersHeld: false))
                .outcome == .waiting
        )
    }

    @Test("修飾キー以外は押したときに読み取り、押し下げと離したときの入力は渡さない")
    mutating func regularKey() {
        let eisu = capture.handle(.keyDown(keyCode: KeyCode.eisu, isRepeat: false, modifiersHeld: false))
        #expect(eisu == .init(outcome: .captured(TriggerKey(keyCode: KeyCode.eisu)!), shouldSuppress: true))
        #expect(capture.handle(.keyUp(keyCode: KeyCode.eisu)) == .init(outcome: .waiting, shouldSuppress: true))
    }

    @Test("文字キーは使えないキーとして扱い、入力は渡さない")
    mutating func rejectedKey() {
        let letterA: UInt16 = 0
        let result = capture.handle(.keyDown(keyCode: letterA, isRepeat: false, modifiersHeld: false))
        #expect(result == .init(outcome: .rejected(keyCode: letterA), shouldSuppress: true))
        #expect(capture.handle(.keyUp(keyCode: letterA)).shouldSuppress)
    }

    @Test("Esc でキャンセルする")
    mutating func escape() {
        let result = capture.handle(.keyDown(keyCode: KeyCode.escape, isRepeat: false, modifiersHeld: false))
        #expect(result == .init(outcome: .cancelled, shouldSuppress: true))
    }

    @Test("押し下げを渡したキーの、離したときの入力は渡す")
    mutating func unrelatedKeyUp() {
        #expect(!capture.handle(.keyUp(keyCode: 0)).shouldSuppress)
    }

    @Test("設定画面が前面にない間は、キーを読み取らず入力も渡す")
    mutating func inactiveKey() {
        let letterA: UInt16 = 0
        let pressed = capture.handleWhileInactive(.keyDown(keyCode: letterA, isRepeat: false, modifiersHeld: false))
        #expect(pressed == .init(outcome: .waiting, shouldSuppress: false))
        #expect(capture.handleWhileInactive(.keyUp(keyCode: letterA)) == .init(outcome: .waiting, shouldSuppress: false))

        let eisu = capture.handleWhileInactive(.keyDown(keyCode: KeyCode.eisu, isRepeat: false, modifiersHeld: false))
        #expect(eisu == .init(outcome: .waiting, shouldSuppress: false))
    }

    @Test("設定画面が前面にない間に押し始めた修飾キーは、前面に戻ってから離しても読み取らない")
    mutating func inactiveModifier() {
        _ = capture.handleWhileInactive(
            .modifierChanged(keyCode: KeyCode.rightCommand, isPressed: true, otherModifiersHeld: false)
        )
        #expect(
            capture.handle(.modifierChanged(keyCode: KeyCode.rightCommand, isPressed: false, otherModifiersHeld: false))
                .outcome == .waiting
        )
    }

    @Test("前面にあるときに渡さなかったキーは、前面でなくなってから離しても渡さない")
    mutating func suppressedKeyReleasedWhileInactive() {
        let letterA: UInt16 = 0
        _ = capture.handle(.keyDown(keyCode: letterA, isRepeat: false, modifiersHeld: false))
        #expect(capture.handleWhileInactive(.keyUp(keyCode: letterA)).shouldSuppress)
    }
}
