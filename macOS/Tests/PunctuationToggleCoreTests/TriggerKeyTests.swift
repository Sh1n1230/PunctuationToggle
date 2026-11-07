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

    @Test(
        "文字キー・Esc・Caps Lock は切り替えキーにできない",
        arguments: [
            UInt16(0),  // A
            49,  // Space
            36,  // Return
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

    @Test("修飾キーのキーコードから種類を求められ、Caps Lock や修飾キー以外は nil になる")
    func modifierFromKeyCode() {
        for (keyCode, info) in TriggerKey.supportedKeys {
            #expect(TriggerKey.Modifier(keyCode: keyCode) == info.modifier)
        }
        #expect(TriggerKey.Modifier(keyCode: KeyCode.capsLock) == nil)
        #expect(TriggerKey.Modifier(keyCode: KeyCode.escape) == nil)
        #expect(TriggerKey.Modifier(keyCode: 0) == nil)
    }

    @Test("すべての修飾キーのビットを合わせた値")
    func allFlagMasks() {
        #expect(TriggerKey.Modifier.allFlagMasks.nonzeroBitCount == TriggerKey.Modifier.allCases.count)
        for modifier in TriggerKey.Modifier.allCases {
            #expect(TriggerKey.Modifier.allFlagMasks & modifier.flagMask == modifier.flagMask)
        }
    }

    @Test("修飾キーかどうか", arguments: [KeyCode.eisu, KeyCode.kana] + KeyCode.functionKeys)
    func nonModifierKeys(keyCode: UInt16) {
        let key = TriggerKey(keyCode: keyCode)
        #expect(key != nil)
        #expect(key?.isModifier == false)
        #expect(key?.modifier == nil)
    }

    @Test("選べるキーは修飾キー9個・英数・かな・F1〜F20")
    func supportedKeyCount() {
        #expect(TriggerKey.supportedKeys.count == TriggerKey.Modifier.allCases.count + 2 + 20)
    }

    @Test("同じキーコードのキーは等しい")
    func equality() {
        #expect(TriggerKey(keyCode: KeyCode.rightCommand) == TriggerKey.rightCommand)
        #expect(TriggerKey(keyCode: KeyCode.leftCommand) != TriggerKey.rightCommand)
    }

    @Test("操作の説明")
    func usageDescription() {
        #expect(TriggerKey.rightCommand.usageDescription == "右Commandを単独で押すと切り替え")
        #expect(TriggerKey(keyCode: KeyCode.kana)!.usageDescription == "かなを押すと切り替え")
    }
}

@Suite("選べるキーの名前")
struct TriggerKeyNameTests {
    /// 修飾キー・英数・かなの、キーコードと表示名と修飾キーかどうか。
    /// 名前は README の「切り替えキーの変更」の表と、設定画面の案内（TriggerKeyRecorderWindowController の instructionText）
    /// に書いたキーと揃える。キーを増やしたり名前を変えたりしたら、README と案内も直す
    private static let expectedKeys: [(keyCode: UInt16, displayName: String, isModifier: Bool)] = [
        (KeyCode.leftCommand, "左Command", true),
        (KeyCode.rightCommand, "右Command", true),
        (KeyCode.leftShift, "左Shift", true),
        (KeyCode.rightShift, "右Shift", true),
        (KeyCode.leftOption, "左Option", true),
        (KeyCode.rightOption, "右Option", true),
        (KeyCode.leftControl, "左Control", true),
        (KeyCode.rightControl, "右Control", true),
        (KeyCode.function, "fn", true),
        (KeyCode.eisu, "英数", false),
        (KeyCode.kana, "かな", false),
    ]

    @Test("修飾キー9個と英数・かなの名前と種類")
    func namesOfModifiersAndJapaneseKeys() throws {
        for expected in Self.expectedKeys {
            let key = try #require(TriggerKey(keyCode: expected.keyCode), "キーコード \(expected.keyCode)")
            #expect(key.displayName == expected.displayName, "キーコード \(expected.keyCode)")
            #expect(key.isModifier == expected.isModifier, "キーコード \(expected.keyCode)")
        }
    }

    @Test("選べるキーは、上の表のキーと F1〜F20 だけ")
    func supportedKeysAreExactlyListed() {
        let listedKeyCodes = Set(Self.expectedKeys.map(\.keyCode)).union(KeyCode.functionKeys)
        #expect(Set(TriggerKey.supportedKeys.keys) == listedKeyCodes)
    }
}
