import Testing
@testable import PunctuationToggleCore

// 両 OS に共通の読み取りは、testdata/trigger_key_vectors.json（TriggerKeyVectorTests）で確かめる。
// ここには macOS だけで選べるキー（fn・英数・かな・F14〜F20）の読み取りを置く。

@Suite("設定画面でのキーの読み取り（macOS だけのキー）")
struct TriggerKeyCaptureTests {
    @Test("fn を単独で押して離すと、fn を読み取る")
    func functionModifierAlone() {
        var capture = TriggerKeyCapture()
        let pressed = capture.handle(
            .modifierChanged(keyCode: KeyCode.function, isPressed: true, otherModifiersHeld: false)
        )
        #expect(pressed == TriggerKeyCapture.Result(outcome: .waiting, shouldSuppress: false))

        let released = capture.handle(
            .modifierChanged(keyCode: KeyCode.function, isPressed: false, otherModifiersHeld: false)
        )
        #expect(released.outcome == .captured(TriggerKey(keyCode: KeyCode.function)!))
        // 修飾キーの入力は止めない
        #expect(!released.shouldSuppress)
    }

    @Test(
        "英数・かな・F14〜F20 は、押したときに読み取り、入力をほかのアプリに渡さない",
        arguments: [KeyCode.eisu, KeyCode.kana] + KeyCode.functionKeys[13...]
    )
    func macOSOnlyRegularKeys(keyCode: UInt16) throws {
        var capture = TriggerKeyCapture()
        let result = capture.handle(.keyDown(keyCode: keyCode, isRepeat: false, modifiersHeld: false))
        #expect(result.outcome == .captured(try #require(TriggerKey(keyCode: keyCode))))
        #expect(result.shouldSuppress)
    }

    @Test("F14〜F20 は7つある")
    func macOSOnlyFunctionKeyCount() {
        #expect(KeyCode.functionKeys[13...].count == 7)
    }
}
