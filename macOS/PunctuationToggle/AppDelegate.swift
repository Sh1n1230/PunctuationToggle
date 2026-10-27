import AppKit
import ServiceManagement

/// メニューバーに現在の句読点を表示し、切り替えキーで切り替える。
final class AppDelegate: NSObject, NSApplicationDelegate, NSMenuDelegate {
    private let keyboardMonitor = KeyboardMonitor()
    private var detector = TriggerKeyDetector(triggerKey: TriggerKeyStore.triggerKey)
    private var triggerKeyRecorder: TriggerKeyRecorderWindowController?
    private var permissionRetryTimer: Timer?
    private var isShowingSaveFailureAlert = false

    private var statusItem: NSStatusItem?
    private let toggleMenuItem = NSMenuItem()
    private let usageMenuItem = NSMenuItem()
    private let changeTriggerKeyMenuItem = NSMenuItem()
    private let launchAtLoginMenuItem = NSMenuItem()

    /// `kAXTrustedCheckOptionPrompt` の値（ApplicationServices の AXUIElement.h）。
    /// 定数はグローバルな `var` として読み込まれ、Swift 6 の言語モードでは並行処理で安全でないとして使えないため、値を書く
    private static let trustedCheckOptionPromptKey = "AXTrustedCheckOptionPrompt"

    func applicationDidFinishLaunching(_ notification: Notification) {
        // 二重に起動すると切り替えキーで2回切り替わって元に戻ってしまうため、後から起動した方は何もせずに終わる
        // （~/Applications のアプリとビルドしたアプリなど、別の場所のアプリでも同じバンドル ID なら対象にする）
        if isAnotherInstanceRunning() {
            NSApp.terminate(nil)
            return
        }

        createStatusItem()

        keyboardMonitor.inputHandler = { [weak self] input in
            self?.handle(input) ?? false
        }

        // システム設定側で変更された場合も表示を追従させる
        DistributedNotificationCenter.default().addObserver(
            self,
            selector: #selector(punctuationSettingDidChange),
            name: JapaneseInputPunctuation.didChangeNotification,
            object: nil,
            suspensionBehavior: .deliverImmediately
        )

        startKeyboardMonitor()
        updateStatusItem()
    }

    func applicationWillTerminate(_ notification: Notification) {
        keyboardMonitor.stop()
    }

    /// 先に起動した同じアプリが動いているか。
    /// 同時に起動した場合に両方が終わらないよう、起動した時刻（同じならプロセス ID）が早い方を残す
    private func isAnotherInstanceRunning() -> Bool {
        guard let bundleIdentifier = Bundle.main.bundleIdentifier else { return false }
        let current = NSRunningApplication.current
        let currentOrder = (current.launchDate ?? .distantFuture, current.processIdentifier)
        return NSRunningApplication.runningApplications(withBundleIdentifier: bundleIdentifier).contains { other in
            other.processIdentifier != current.processIdentifier && !other.isTerminated
                && (other.launchDate ?? .distantFuture, other.processIdentifier) < currentOrder
        }
    }

    // MARK: - 入力の処理

    private func handle(_ input: KeyboardInput) -> Bool {
        if let triggerKeyRecorder {
            return triggerKeyRecorder.handle(input)
        }

        let decision = detector.handle(input)
        if decision.shouldToggle {
            // 入力の監視を止めないよう、設定の書き換えは監視のコールバックを抜けてから行う
            DispatchQueue.main.async { [weak self] in
                self?.togglePunctuationStyle()
            }
        }
        return decision.shouldSuppress
    }

    private func togglePunctuationStyle() {
        let newStyle = JapaneseInputPunctuation.current.toggled
        if !JapaneseInputPunctuation.set(newStyle) {
            NSLog("句読点の設定を保存できませんでした: \(newStyle.symbols)")
            showSaveFailureAlert()
        }
        updateStatusItem()
    }

    /// 句読点の設定を保存できなかったことを知らせる。
    /// 切り替えキーを続けて押したときにアラートが重ならないよう、表示中は新しく出さない
    private func showSaveFailureAlert() {
        guard !isShowingSaveFailureAlert else { return }
        isShowingSaveFailureAlert = true
        defer { isShowingSaveFailureAlert = false }

        let alert = NSAlert()
        alert.alertStyle = .warning
        alert.messageText = "句読点の設定を変更できませんでした"
        alert.informativeText = "日本語入力の設定ファイルに書き込めませんでした。句読点は切り替わっていません。"
        NSApp.activate()
        alert.runModal()
    }

    // MARK: - アクセシビリティの許可

    private func startKeyboardMonitor() {
        if keyboardMonitor.start() {
            return
        }

        // 許可を求めるダイアログを出し、許可されるまで定期的に再試行する
        let options = [Self.trustedCheckOptionPromptKey: true] as CFDictionary
        AXIsProcessTrustedWithOptions(options)

        // メインの RunLoop に登録したタイマーなので、メインスレッドで呼ばれる
        permissionRetryTimer = Timer.scheduledTimer(withTimeInterval: 1.0, repeats: true) { [weak self] _ in
            MainActor.assumeIsolated {
                guard let self, self.keyboardMonitor.start() else { return }
                self.permissionRetryTimer?.invalidate()
                self.permissionRetryTimer = nil
                self.updateStatusItem()
            }
        }
    }

    // MARK: - メニュー

    private func createStatusItem() {
        let statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        self.statusItem = statusItem

        let menu = NSMenu()
        menu.delegate = self

        toggleMenuItem.title = "「，．」モード"
        toggleMenuItem.action = #selector(toggleFromMenu)
        toggleMenuItem.target = self
        menu.addItem(toggleMenuItem)

        usageMenuItem.isEnabled = false
        menu.addItem(usageMenuItem)

        menu.addItem(.separator())

        changeTriggerKeyMenuItem.title = "切り替えキーを変更…"
        changeTriggerKeyMenuItem.action = #selector(showTriggerKeyRecorder)
        changeTriggerKeyMenuItem.target = self
        menu.addItem(changeTriggerKeyMenuItem)

        launchAtLoginMenuItem.title = "ログイン時に起動"
        launchAtLoginMenuItem.action = #selector(toggleLaunchAtLogin)
        launchAtLoginMenuItem.target = self
        menu.addItem(launchAtLoginMenuItem)

        menu.addItem(withTitle: "アクセシビリティ設定を開く", action: #selector(openAccessibilitySettings), keyEquivalent: "")
            .target = self

        menu.addItem(.separator())

        menu.addItem(withTitle: "最新版を確認…", action: #selector(openLatestRelease), keyEquivalent: "")
            .target = self
        menu.addItem(withTitle: "PunctuationToggle について", action: #selector(showAboutPanel), keyEquivalent: "")
            .target = self
        menu.addItem(withTitle: "終了", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")

        statusItem.menu = menu
    }

    func menuNeedsUpdate(_ menu: NSMenu) {
        updateStatusItem()
    }

    private func updateStatusItem() {
        let style = JapaneseInputPunctuation.current
        let triggerKey = detector.triggerKey

        if let button = statusItem?.button {
            // 記号だけでは VoiceOver の読み上げで意味が伝わりにくいため、読み上げる名前を別に付ける
            if keyboardMonitor.isRunning {
                button.title = style.symbols
                button.toolTip = "句読点: \(style.symbols)（\(triggerKey.usageDescription)）"
                button.setAccessibilityLabel("PunctuationToggle、句読点: \(style.spokenName)")
            } else {
                button.title = "⚠︎"
                button.toolTip = "アクセシビリティの許可が必要です"
                button.setAccessibilityLabel("PunctuationToggle、アクセシビリティの許可が必要です")
            }
        }

        toggleMenuItem.state = style == .commaPeriod ? .on : .off
        usageMenuItem.title = triggerKey.usageDescription
        changeTriggerKeyMenuItem.isEnabled = keyboardMonitor.isRunning
        changeTriggerKeyMenuItem.toolTip = keyboardMonitor.isRunning ? nil : "アクセシビリティの許可が必要です"
        launchAtLoginMenuItem.state = SMAppService.mainApp.status == .enabled ? .on : .off
    }

    @objc
    private func punctuationSettingDidChange() {
        updateStatusItem()
    }

    @objc
    private func toggleFromMenu() {
        togglePunctuationStyle()
    }

    @objc
    private func showTriggerKeyRecorder() {
        if let triggerKeyRecorder {
            NSApp.activate()
            triggerKeyRecorder.window?.makeKeyAndOrderFront(nil)
            triggerKeyRecorder.window?.orderFrontRegardless()
            return
        }

        let recorder = TriggerKeyRecorderWindowController(currentKey: detector.triggerKey) { [weak self] newKey in
            guard let self else { return }
            self.triggerKeyRecorder = nil
            if let newKey {
                TriggerKeyStore.triggerKey = newKey
                self.detector.triggerKey = newKey
            }
            self.updateStatusItem()
        }
        triggerKeyRecorder = recorder

        // ほかのアプリが前面だと macOS が前面にしないことがあるため、ウィンドウだけでも最前面に出す
        NSApp.activate()
        recorder.showWindow(nil)
        recorder.window?.orderFrontRegardless()
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
            NSLog("ログイン項目の変更に失敗しました: \(error)")
            let alert = NSAlert(error: error)
            alert.messageText = "「ログイン時に起動」を変更できませんでした"
            NSApp.activate()
            alert.runModal()
        }
        updateStatusItem()
    }

    @objc
    private func openAccessibilitySettings() {
        guard
            let url = URL(
                string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility"
            )
        else {
            return
        }
        NSWorkspace.shared.open(url)
    }

    /// 最新のリリースのページをブラウザーで開く。アプリ自身は通信せず、利用者がメニューを選んだときだけ開く
    @objc
    private func openLatestRelease() {
        guard let url = URL(string: "https://github.com/Sh1n1230/PunctuationToggle/releases/latest") else {
            return
        }
        NSWorkspace.shared.open(url)
    }

    @objc
    private func showAboutPanel() {
        NSApp.activate()
        NSApp.orderFrontStandardAboutPanel(nil)
    }
}
