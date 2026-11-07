import Testing
@testable import PunctuationToggleCore

@Suite("保存した切り替えキーの解釈")
struct StoredTriggerKeyTests {
    @Test("対応しているキーコードなら、そのキーになる", arguments: [KeyCode.rightCommand, KeyCode.function, KeyCode.eisu, 105])
    func supportedKeyCode(keyCode: UInt16) throws {
        #expect(TriggerKey(storedKeyCode: Int(keyCode)) == (try #require(TriggerKey(keyCode: keyCode))))
    }

    @Test(
        "値が無い・UInt16 に収まらない・未対応のキーコードなら、初期設定（右Command）になる",
        arguments: [
            nil,
            -1,
            65_536,
            Int.max,
            0,  // A（文字キー）
            Int(KeyCode.escape),
            Int(KeyCode.capsLock),
        ] as [Int?]
    )
    func fallsBackToDefault(storedKeyCode: Int?) {
        #expect(TriggerKey(storedKeyCode: storedKeyCode) == .defaultKey)
    }
}

@Suite("日本語入力の句読点の設定値の解釈")
struct StoredPunctuationStyleTests {
    @Test("0〜3 はシステム設定の並び順の句読点になる")
    func validValues() {
        #expect(PunctuationStyle(storedValue: 0) == .toutenKuten)
        #expect(PunctuationStyle(storedValue: 1) == .commaKuten)
        #expect(PunctuationStyle(storedValue: 2) == .toutenPeriod)
        #expect(PunctuationStyle(storedValue: 3) == .commaPeriod)
    }

    @Test("値が無い・範囲外なら「、。」とみなす", arguments: [nil, 4, -1, Int.max] as [Int?])
    func fallsBackToSystemDefault(storedValue: Int?) {
        #expect(PunctuationStyle(storedValue: storedValue) == .systemDefault)
    }
}
