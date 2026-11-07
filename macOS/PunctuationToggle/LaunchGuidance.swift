import AppKit
import Carbon.HIToolbox

/// 起動したときに、利用者に知らせること（二重起動・アプリの場所）。
enum LaunchGuidance {
    /// 移すよう案内するアラートで「今後表示しない」を選んだかを保存する UserDefaults のキー
    private static let suppressMoveToApplicationsDefaultsKey = "SuppressMoveToApplicationsAlert"

    /// ログイン項目として起動されたか（利用者が自分で開いたのではないか）。
    /// 起動の Apple Event（kAEOpenApplication）の keyAEPropData が keyAELaunchedAsLogInItem かで判断する。
    /// `applicationWillFinishLaunching(_:)` か `applicationDidFinishLaunching(_:)` の中で呼ぶ（その間だけ起動の Apple Event が読める）
    static func isLaunchedAsLoginItem() -> Bool {
        guard let event = NSAppleEventManager.shared().currentAppleEvent,
            event.eventClass == AEEventClass(kCoreEventClass),
            event.eventID == AEEventID(kAEOpenApplication)
        else {
            return false
        }
        return event.paramDescriptor(forKeyword: AEKeyword(keyAEPropData))?.enumCodeValue
            == OSType(keyAELaunchedAsLogInItem)
    }

    /// 先に起動した同じアプリが動いているので終了することを知らせる。
    /// 新しい版をダウンロードしたのに古い版が動き続けている場合などに、起動しても何も起きないように見えないようにする
    static func showAlreadyRunningAlert(runningApplication: NSRunningApplication) {
        var details: [String] = []
        if let bundleURL = runningApplication.bundleURL {
            details.append("場所: \(bundleURL.path)")
            if let version = Bundle(url: bundleURL)?.object(forInfoDictionaryKey: "CFBundleShortVersionString")
                as? String
            {
                details.append("版: \(version)")
            }
        }
        details.append("こちらは終了します。別の版を使う場合は、動いている方をメニューの「終了」で終了してから開いてください。")

        let alert = NSAlert()
        alert.alertStyle = .informational
        alert.messageText = "PunctuationToggle はすでに起動しています"
        alert.informativeText = details.joined(separator: "\n")
        NSApp.activate()
        alert.runModal()
    }

    /// 「アプリケーション」フォルダーの外から起動した場合に、移すよう案内する。「今後表示しない」を選ぶと、以後は案内しない
    static func suggestMovingToApplicationsIfNeeded() {
        let defaults = UserDefaults.standard
        guard !defaults.bool(forKey: suppressMoveToApplicationsDefaultsKey),
            ApplicationLocation.shouldSuggestMovingToApplications(
                bundlePath: Bundle.main.bundlePath,
                homeDirectory: NSHomeDirectory()
            )
        else {
            return
        }

        let alert = NSAlert()
        alert.alertStyle = .warning
        alert.messageText = "PunctuationToggle を「アプリケーション」フォルダーに移してください"
        alert.informativeText = """
            「ダウンロード」フォルダーなどから起動すると、アクセシビリティの許可が起動のたびにやり直しになったり、\
            「ログイン時に起動」が効かなかったりします。\
            PunctuationToggle を終了し、Finder で「アプリケーション」フォルダーに移してから開き直してください。
            """
        alert.showsSuppressionButton = true
        alert.suppressionButton?.title = "今後表示しない"
        NSApp.activate()
        alert.runModal()
        if alert.suppressionButton?.state == .on {
            defaults.set(true, forKey: suppressMoveToApplicationsDefaultsKey)
        }
    }
}
