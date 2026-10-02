import Testing
@testable import PunctuationToggleCore

@Suite("切り替えキーの定義")
struct TriggerKeyTests {
    @Test("初期設定は右Command")
    func defaultKey() {
        #expect(TriggerKey.defaultKey.keyCode == 54)
        #expect(TriggerKey.defaultKey.displayName == "右Command")
        #expect(TriggerKey.defaultKey.isModifier)
    }

    @Test("文字キー・Esc・Caps Lock は切り替えキーにできない", arguments: [
        UInt16(0),  // A
        49,         // Space
        36,         // Return
        KeyCode.escape,
        KeyCode.capsLock,
    ])
    func unsupportedKeys(keyCode: UInt16) {
        #expect(TriggerKey(keyCode: keyCode) == nil)
    }

    @Test("ファンクションキーは F1〜F20 の名前になる")
    func functionKeyNames() {
        let names = KeyCode.functionKeys.compactMap { keyCode in TriggerKey(keyCode: keyCode)?.displayName }
        #expect(names == (1...20).map { number in "F\(number)" })
        #expect(Set(KeyCode.functionKeys).count == 20)
    }

    @Test("修飾キーには左右を区別するビットが割り当てられている")
    func modifierMasks() {
        let modifierKeys = TriggerKey.supportedKeys.values.compactMap(\.modifier)
        #expect(modifierKeys.count == TriggerKey.Modifier.allCases.count)

        let masks = TriggerKey.Modifier.allCases.map(\.flagMask)
        #expect(Set(masks).count == masks.count)
        for mask in masks {
            #expect(mask.nonzeroBitCount == 1)
        }
    }

    @Test("操作の説明")
    func usageDescription() {
        #expect(TriggerKey.rightCommand.usageDescription == "右Commandを単独で押すと切り替え")
        #expect(TriggerKey(keyCode: KeyCode.kana)!.usageDescription == "かなを押すと切り替え")
    }
}

@Suite("句読点の種類")
struct PunctuationStyleTests {
    @Test("システム設定の値と表示が対応している")
    func symbols() {
        #expect(PunctuationStyle(rawValue: 0)?.symbols == "、。")
        #expect(PunctuationStyle(rawValue: 1)?.symbols == "，。")
        #expect(PunctuationStyle(rawValue: 2)?.symbols == "、．")
        #expect(PunctuationStyle(rawValue: 3)?.symbols == "，．")
        #expect(PunctuationStyle(rawValue: 4) == nil)
    }

    @Test("「、。」と「，．」を行き来する")
    func toggled() {
        #expect(PunctuationStyle.toutenKuten.toggled == .commaPeriod)
        #expect(PunctuationStyle.commaPeriod.toggled == .toutenKuten)
    }

    @Test("混在した設定からは「，．」に切り替える")
    func toggledFromMixedStyles() {
        #expect(PunctuationStyle.commaKuten.toggled == .commaPeriod)
        #expect(PunctuationStyle.toutenPeriod.toggled == .commaPeriod)
    }
}
