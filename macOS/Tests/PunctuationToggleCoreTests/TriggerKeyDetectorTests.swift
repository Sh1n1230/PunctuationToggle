import Testing
@testable import PunctuationToggleCore

// 両 OS に共通の判定は、testdata/trigger_key_vectors.json（TriggerKeyVectorTests）で確かめる。
// ここには macOS 版だけのキーや、入力の列では表せない操作（切り替えキーの変更・reset()）のテストを置く。

@Suite("修飾キーを切り替えキーにした場合")
struct ModifierTriggerTests {
    private var detector = TriggerKeyDetector(triggerKey: .rightCommand)

    private func press(_ keyCode: UInt16) -> KeyboardInput {
        .modifierChanged(keyCode: keyCode, isPressed: true, otherModifiersHeld: false)
    }

    private func release(_ keyCode: UInt16) -> KeyboardInput {
        .modifierChanged(keyCode: keyCode, isPressed: false, otherModifiersHeld: false)
    }

    @Test("fn キーも単独押しで切り替える")
    mutating func functionModifier() {
        detector.triggerKey = TriggerKey(keyCode: KeyCode.function)!
        _ = detector.handle(press(KeyCode.function))
        #expect(detector.handle(release(KeyCode.function)).shouldToggle)
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

    @Test("英数キーも押したときに切り替える")
    mutating func eisu() {
        detector.triggerKey = TriggerKey(keyCode: KeyCode.eisu)!
        #expect(
            detector.handle(.keyDown(keyCode: KeyCode.eisu, isRepeat: false, modifiersHeld: false))
                == InputDecision(shouldToggle: true, shouldSuppress: true)
        )
    }

    @Test("切り替えキーを変えると、押し下げ中の状態は破棄される")
    mutating func changingTriggerKeyResetsState() {
        _ = detector.handle(.keyDown(keyCode: Self.f13, isRepeat: false, modifiersHeld: false))
        detector.triggerKey = TriggerKey(keyCode: KeyCode.eisu)!
        // 古いキーを離した入力は、もう切り替えキーの入力ではないのでそのまま渡す
        #expect(detector.handle(.keyUp(keyCode: Self.f13)) == .passThrough)
        #expect(detector.handle(.keyDown(keyCode: KeyCode.eisu, isRepeat: false, modifiersHeld: false)).shouldToggle)
    }

    @Test("reset() で押し下げ中の状態を破棄する")
    mutating func resetClearsSuppression() {
        _ = detector.handle(.keyDown(keyCode: Self.f13, isRepeat: false, modifiersHeld: false))
        detector.reset()
        #expect(detector.handle(.keyUp(keyCode: Self.f13)) == .passThrough)
    }
}
