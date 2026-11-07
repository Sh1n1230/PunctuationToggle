import Carbon.HIToolbox
import Foundation

/// 今のキーボード配列で、キーが入力する文字を求める（使えないキーを押したときに示す名前に使う）。
///
/// キーコードはキーの位置を表し、同じキーでも配列（JIS・US・AZERTY など）によって入力する文字が違うため、
/// 文字キーの名前は表（Core の KeyNames）に持たず、ここで求める。Windows 版の KeyboardLayoutKeyNames と同じ役割。
enum KeyboardLayoutKeyNames {
    /// UCKeyTranslate で受け取る文字の最大の長さ（1つのキーが入力する文字は、ほとんど1文字）
    private static let maximumCharacterCount = 4

    /// キーの名前。文字にならないキーや、配列の情報を得られない場合は nil
    static func name(for keyCode: UInt16) -> String? {
        // 日本語入力を使っている間も、キーの配列には英字の入力ソース（ABC など）の配列が使われる
        guard let inputSource = TISCopyCurrentKeyboardLayoutInputSource()?.takeRetainedValue(),
            let layoutDataPointer = TISGetInputSourceProperty(inputSource, kTISPropertyUnicodeKeyLayoutData)
        else {
            return nil
        }
        let layoutData = Unmanaged<CFData>.fromOpaque(layoutDataPointer).takeUnretainedValue()
        guard let layoutBytes = CFDataGetBytePtr(layoutData) else { return nil }

        var deadKeyState: UInt32 = 0
        var characters = [UniChar](repeating: 0, count: maximumCharacterCount)
        var characterCount = 0
        let status = layoutBytes.withMemoryRebound(to: UCKeyboardLayout.self, capacity: 1) { layout in
            UCKeyTranslate(
                layout,
                keyCode,
                UInt16(kUCKeyActionDisplay),
                0,  // 修飾キーなし
                UInt32(LMGetKbdType()),
                OptionBits(kUCKeyTranslateNoDeadKeysMask),
                &deadKeyState,
                maximumCharacterCount,
                &characterCount,
                &characters
            )
        }
        guard status == noErr, characterCount > 0 else { return nil }

        let text = String(utf16CodeUnits: characters, count: characterCount)
            .trimmingCharacters(in: .whitespacesAndNewlines.union(.controlCharacters))
        // キーの刻印と同じく、英字は大文字で示す
        return text.isEmpty ? nil : text.uppercased()
    }
}
