import Testing
@testable import PunctuationToggleCore

@Suite("使えないキーの名前")
struct KeyNamesTests {
    @Test("文字にならないキーは、表の名前になる")
    func namedKeys() {
        #expect(KeyNames.name(for: KeyCode.space) == "スペース")
        #expect(KeyNames.name(for: KeyCode.returnKey) == "Return")
        #expect(KeyNames.name(for: KeyCode.tab) == "Tab")
        #expect(KeyNames.name(for: KeyCode.delete) == "Delete")
        #expect(KeyNames.name(for: KeyCode.leftArrow) == "←")
        #expect(KeyNames.name(for: KeyCode.upArrow) == "↑")
    }

    @Test("テンキーの数字は「テンキーの 0」〜「テンキーの 9」")
    func keypadDigits() {
        let names = KeyCode.keypadDigits.map { keyCode in KeyNames.name(for: keyCode) }
        #expect(names == (0...9).map { digit in "テンキーの \(digit)" })
    }

    @Test("文字キーは表に無い（今のキーボード配列で求める）", arguments: [UInt16(0), 1, 18])  // A・S・1
    func characterKeysAreNotInTable(keyCode: UInt16) {
        #expect(KeyNames.name(for: keyCode) == nil)
    }
}

@Suite("入力ソースの判定")
struct InputSourceSupportTests {
    @Test(
        "macOS 標準の日本語入力（ローマ字入力・かな入力）では知らせない",
        arguments: [
            "com.apple.inputmethod.Kotoeri.RomajiTyping.Japanese",
            "com.apple.inputmethod.Kotoeri.KanaTyping.Japanese",
            "com.apple.inputmethod.Kotoeri.RomajiTyping.Japanese.Katakana",
        ])
    func appleJapaneseInput(inputSourceID: String) {
        #expect(!InputSourceSupport.isUnsupportedJapaneseInputSource(inputSourceID: inputSourceID, languages: ["ja"]))
    }

    @Test(
        "ほかの日本語入力では知らせる",
        arguments: ["com.google.inputmethod.Japanese.base", "com.justsystems.inputmethod.atok34.Japanese"])
    func otherJapaneseInput(inputSourceID: String) {
        #expect(InputSourceSupport.isUnsupportedJapaneseInputSource(inputSourceID: inputSourceID, languages: ["ja"]))
    }

    @Test("英字の入力ソースでは知らせない")
    func nonJapaneseInputSource() {
        #expect(
            !InputSourceSupport.isUnsupportedJapaneseInputSource(
                inputSourceID: "com.apple.keylayout.ABC", languages: ["en", "fr"]))
        #expect(
            !InputSourceSupport.isUnsupportedJapaneseInputSource(
                inputSourceID: "com.google.inputmethod.Japanese.Roman", languages: ["en"]))
        #expect(!InputSourceSupport.isUnsupportedJapaneseInputSource(inputSourceID: "unknown", languages: []))
    }
}

@Suite("アプリの場所")
struct ApplicationLocationTests {
    private static let home = "/Users/someone"

    @Test(
        "「アプリケーション」フォルダーと ~/Applications にあれば案内しない",
        arguments: [
            "/Applications/PunctuationToggle.app",
            "/Applications/Utilities/PunctuationToggle.app",
            "/Users/someone/Applications/PunctuationToggle.app",
        ])
    func inApplications(bundlePath: String) {
        #expect(
            !ApplicationLocation.shouldSuggestMovingToApplications(bundlePath: bundlePath, homeDirectory: Self.home))
    }

    @Test(
        "ほかの場所や App Translocation で実行されていれば案内する",
        arguments: [
            "/Users/someone/Downloads/PunctuationToggle.app",
            "/private/var/folders/xy/abc/T/AppTranslocation/0123-4567/d/PunctuationToggle.app",
            "/Users/someone/Desktop/Applications/PunctuationToggle.app",
            "/Users/other/Applications/PunctuationToggle.app",
            "/ApplicationsBackup/PunctuationToggle.app",
        ])
    func outsideApplications(bundlePath: String) {
        #expect(ApplicationLocation.shouldSuggestMovingToApplications(bundlePath: bundlePath, homeDirectory: Self.home))
    }
}
