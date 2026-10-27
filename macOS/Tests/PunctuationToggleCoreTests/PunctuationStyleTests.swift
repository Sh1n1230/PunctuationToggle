import Testing
@testable import PunctuationToggleCore

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

    @Test("設定が無いときは「、。」とみなす")
    func systemDefault() {
        #expect(PunctuationStyle.systemDefault == .toutenKuten)
    }

    @Test("どの設定からも2回切り替えると「，．」か「、。」に落ち着く", arguments: PunctuationStyle.allCases)
    func toggledTwice(style: PunctuationStyle) {
        #expect([.toutenKuten, .commaPeriod].contains(style.toggled.toggled))
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
