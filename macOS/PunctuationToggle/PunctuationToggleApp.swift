import SwiftUI
import AppKit
import ServiceManagement

@main
struct PunctuationToggleApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self)
    private var appDelegate

    var body: some Scene {
        Settings {
            EmptyView()
        }
    }
}

/// macOS標準の日本語入力の「句読点の種類」設定を読み書きする。
/// システム設定の画面で変更したときと同じ通知を送るので、
/// 入力中の日本語入力にも即座に反映され、未確定文字も失われない。
enum JapaneseIMPunctuation {
    private static let domain = "com.apple.inputmethod.Kotoeri" as CFString
    private static let key = "JIMPrefPunctuationTypeKey"

    static let didChangeNotification = Notification.Name(
        "com.apple.inputmethod.JIM.PreferencesDidChangeNotification"
    )
    private static let notificationObject = "com.apple.JIMPreferences"

    /// 設定値（システム設定のポップアップの並び順）
    ///   0: 。と、  1: 。と，  2: ．と、  3: ．と，
    static let japaneseStyle = 0
    static let fullWidthStyle = 3

    static var current: Int {
        CFPreferencesAppSynchronize(domain)
        let value = CFPreferencesCopyAppValue(key as CFString, domain)
        return (value as? Int) ?? japaneseStyle
    }

    static func set(_ value: Int) {
        CFPreferencesSetAppValue(key as CFString, value as CFNumber, domain)
        CFPreferencesAppSynchronize(domain)

        // 日本語入力は userInfo に入った値を見て設定を更新する
        DistributedNotificationCenter.default().postNotificationName(
            didChangeNotification,
            object: notificationObject,
            userInfo: [key: value],
            deliverImmediately: true
        )
    }
}

final class AppDelegate: NSObject, NSApplicationDelegate {
    private var statusItem: NSStatusItem?
    private var toggleMenuItem: NSMenuItem?
    private var loginMenuItem: NSMenuItem?
    private var eventTap: CFMachPort?
    private var runLoopSource: CFRunLoopSource?
    private var retryTimer: Timer?

    // 右Commandを押してから離すまでの間に、他のキーが押されたか
    private var rightCommandIsDown = false
    private var rightCommandUsedWithOtherKey = false

    private let rightCommandKeyCode: Int64 = 54

    private var fullWidthMode: Bool {
        JapaneseIMPunctuation.current == JapaneseIMPunctuation.fullWidthStyle
    }

    func applicationDidFinishLaunching(_ notification: Notification) {
        createStatusItem()
        updateStatusItem()

        // システム設定側で変更された場合も表示を追従させる
        DistributedNotificationCenter.default().addObserver(
            self,
            selector: #selector(punctuationSettingChanged),
            name: JapaneseIMPunctuation.didChangeNotification,
            object: nil,
            suspensionBehavior: .deliverImmediately
        )

        requestAccessibilityPermission()

        if !startEventTap() {
            // 権限が付与されるまで定期的に再試行する
            retryTimer = Timer.scheduledTimer(
                withTimeInterval: 1.0,
                repeats: true
            ) { [weak self] timer in
                MainActor.assumeIsolated {
                    guard let self else { return }
                    if self.startEventTap() {
                        timer.invalidate()
                        self.retryTimer = nil
                        self.updateStatusItem()
                    }
                }
            }
        }
    }

    func applicationWillTerminate(_ notification: Notification) {
        stopEventTap()
    }

    // MARK: - Status item

    private func createStatusItem() {
        statusItem = NSStatusBar.system.statusItem(
            withLength: NSStatusItem.variableLength
        )

        let menu = NSMenu()

        let toggleItem = NSMenuItem(
            title: "，． モード",
            action: #selector(toggleModeFromMenu),
            keyEquivalent: ""
        )
        toggleItem.target = self
        menu.addItem(toggleItem)
        toggleMenuItem = toggleItem

        let hintItem = NSMenuItem(
            title: "右Commandを単独で押すと切り替え",
            action: nil,
            keyEquivalent: ""
        )
        hintItem.isEnabled = false
        menu.addItem(hintItem)

        menu.addItem(.separator())

        let loginItem = NSMenuItem(
            title: "ログイン時に起動",
            action: #selector(toggleLaunchAtLogin),
            keyEquivalent: ""
        )
        loginItem.target = self
        menu.addItem(loginItem)
        loginMenuItem = loginItem

        let permissionItem = NSMenuItem(
            title: "アクセシビリティ設定を開く",
            action: #selector(openAccessibilitySettings),
            keyEquivalent: ""
        )
        permissionItem.target = self
        menu.addItem(permissionItem)

        menu.addItem(.separator())

        let quitItem = NSMenuItem(
            title: "終了",
            action: #selector(quitApplication),
            keyEquivalent: "q"
        )
        quitItem.target = self
        menu.addItem(quitItem)

        statusItem?.menu = menu
    }

    @objc
    private func toggleModeFromMenu() {
        toggleMode()
    }

    @objc
    private func punctuationSettingChanged() {
        updateStatusItem()
    }

    @objc
    private func toggleLaunchAtLogin() {
        let service = SMAppService.mainApp

        do {
            if service.status == .enabled {
                try service.unregister()
            } else {
                try service.register()
            }
        } catch {
            NSLog("ログイン項目の変更に失敗: \(error)")
        }

        updateStatusItem()
    }

    @objc
    private func quitApplication() {
        NSApplication.shared.terminate(nil)
    }

    @objc
    private func openAccessibilitySettings() {
        guard let url = URL(
            string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility"
        ) else {
            return
        }

        NSWorkspace.shared.open(url)
    }

    private func toggleMode() {
        JapaneseIMPunctuation.set(
            fullWidthMode
                ? JapaneseIMPunctuation.japaneseStyle
                : JapaneseIMPunctuation.fullWidthStyle
        )

        updateStatusItem()
    }

    private func updateStatusItem() {
        guard let button = statusItem?.button else { return }

        let fullWidthMode = fullWidthMode

        if eventTap == nil {
            button.title = "⚠︎"
            button.toolTip = "アクセシビリティの許可が必要です"
        } else if fullWidthMode {
            button.title = "，．"
            button.toolTip = "全角コンマ・ピリオドモード"
        } else {
            button.title = "、。"
            button.toolTip = "日本語句読点モード"
        }

        toggleMenuItem?.state = fullWidthMode ? .on : .off
        loginMenuItem?.state =
            SMAppService.mainApp.status == .enabled ? .on : .off
    }

    // MARK: - Accessibility

    private func requestAccessibilityPermission() {
        let options: NSDictionary = [
            kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true
        ]

        AXIsProcessTrustedWithOptions(options)
    }

    // MARK: - Event tap

    private func startEventTap() -> Bool {
        guard eventTap == nil else { return true }

        let eventMask =
            (1 << CGEventType.keyDown.rawValue) |
            (1 << CGEventType.flagsChanged.rawValue)

        let callback: CGEventTapCallBack = { _, type, event, userInfo in
            guard let userInfo else {
                return Unmanaged.passUnretained(event)
            }

            // タップはメインRunLoopに登録しているので、メインスレッドで呼ばれる
            return MainActor.assumeIsolated {
                let appDelegate = Unmanaged<AppDelegate>
                    .fromOpaque(userInfo)
                    .takeUnretainedValue()

                appDelegate.handleEvent(type: type, event: event)
                return Unmanaged.passUnretained(event)
            }
        }

        guard let tap = CGEvent.tapCreate(
            tap: .cgSessionEventTap,
            place: .headInsertEventTap,
            options: .defaultTap,
            eventsOfInterest: CGEventMask(eventMask),
            callback: callback,
            userInfo: Unmanaged.passUnretained(self).toOpaque()
        ) else {
            return false
        }

        guard let source = CFMachPortCreateRunLoopSource(
            kCFAllocatorDefault,
            tap,
            0
        ) else {
            CFMachPortInvalidate(tap)
            return false
        }

        CFRunLoopAddSource(CFRunLoopGetMain(), source, .commonModes)
        CGEvent.tapEnable(tap: tap, enable: true)

        eventTap = tap
        runLoopSource = source
        return true
    }

    private func stopEventTap() {
        if let eventTap {
            CGEvent.tapEnable(tap: eventTap, enable: false)
            CFMachPortInvalidate(eventTap)
        }

        if let runLoopSource {
            CFRunLoopRemoveSource(
                CFRunLoopGetMain(),
                runLoopSource,
                .commonModes
            )
        }

        runLoopSource = nil
        eventTap = nil
    }

    /// キー入力は監視するだけで、書き換えや破棄はしない。
    private func handleEvent(type: CGEventType, event: CGEvent) {
        switch type {
        case .tapDisabledByTimeout, .tapDisabledByUserInput:
            // macOSによってタップが無効化された場合は再度有効化
            if let eventTap {
                CGEvent.tapEnable(tap: eventTap, enable: true)
            }

        case .flagsChanged:
            handleFlagsChanged(event)

        case .keyDown:
            if rightCommandIsDown {
                // 右Command + 何かのキー（ショートカット）として使われた
                rightCommandUsedWithOtherKey = true
            }

        default:
            break
        }
    }

    /// 右Commandを「単独で押して離した」ときだけモードを切り替える。
    /// 右Command + C などのショートカットでは切り替えない。
    /// イベント自体は通常どおり流すので、Commandキーとしての動作は残る。
    private func handleFlagsChanged(_ event: CGEvent) {
        let keyCode = event.getIntegerValueField(.keyboardEventKeycode)

        guard keyCode == rightCommandKeyCode else {
            if rightCommandIsDown {
                // 右Commandを押したまま他の修飾キーが押された
                rightCommandUsedWithOtherKey = true
            }
            return
        }

        // NX_DEVICERCMDKEYMASK: 右Commandが押されているかを示すデバイス依存フラグ
        let deviceRightCommandMask: UInt64 = 0x10
        let isDown = event.flags.rawValue & deviceRightCommandMask != 0

        if isDown {
            rightCommandIsDown = true
            rightCommandUsedWithOtherKey = false
        } else if rightCommandIsDown {
            rightCommandIsDown = false

            if !rightCommandUsedWithOtherKey {
                toggleMode()
            }
        }
    }
}
