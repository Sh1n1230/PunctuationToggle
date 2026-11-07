import Carbon.HIToolbox

/// 切り替えキーや句読点の切り替えが効かない状態かを、メニューで知らせるために確かめる。
enum InputEnvironment {
    /// セキュア入力（パスワードの入力欄など）が有効か。
    /// 有効な間は、どのアプリが前面でもキー入力がイベントタップに届かず、切り替えキーが効かない（TN2150）。
    /// どのプロセスが有効にしているかは公開の API では分からない
    static var isSecureEventInputEnabled: Bool {
        IsSecureEventInputEnabled()
    }

    /// 今の入力ソースが、句読点の切り替えが効かない日本語入力（Google 日本語入力・ATOK など）なら、その名前を返す
    static func unsupportedJapaneseInputSourceName() -> String? {
        guard let inputSource = TISCopyCurrentKeyboardInputSource()?.takeRetainedValue(),
            let inputSourceID = stringProperty(of: inputSource, key: kTISPropertyInputSourceID)
        else {
            return nil
        }
        let languages = arrayProperty(of: inputSource, key: kTISPropertyInputSourceLanguages) ?? []
        guard InputSourceSupport.isUnsupportedJapaneseInputSource(inputSourceID: inputSourceID, languages: languages)
        else {
            return nil
        }
        return stringProperty(of: inputSource, key: kTISPropertyLocalizedName) ?? inputSourceID
    }

    private static func stringProperty(of inputSource: TISInputSource, key: CFString) -> String? {
        guard let pointer = TISGetInputSourceProperty(inputSource, key) else { return nil }
        return Unmanaged<CFString>.fromOpaque(pointer).takeUnretainedValue() as String
    }

    private static func arrayProperty(of inputSource: TISInputSource, key: CFString) -> [String]? {
        guard let pointer = TISGetInputSourceProperty(inputSource, key) else { return nil }
        return Unmanaged<CFArray>.fromOpaque(pointer).takeUnretainedValue() as? [String]
    }
}
