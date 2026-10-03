import AppKit
import ServiceManagement

/// メニューバーに現在の句読点を表示し、切り替えキーで切り替える。
final class AppDelegate: NSObject, NSApplicationDelegate, NSMenuDelegate {
    private let keyboardMonitor = KeyboardMonitor()
    private var detector = TriggerKeyDetector(triggerKey: TriggerKeyStore.triggerKey)
    private var triggerKeyRecorder: TriggerKeyRecorderWindowController?
    private var permissionRetryTimer: Timer?

    private var statusItem: NSStatusItem?
    private let toggleMenuItem = NSMenuItem()
    private let usageMenuItem = NSMenuItem()
    private let changeTriggerKeyMenuItem = NSMenuItem()
    private let launchAtLoginMenuItem = NSMenuItem()

    func applicationDidFinishLaunching(_ notification: Notification) {
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
        JapaneseInputPunctuation.set(JapaneseInputPunctuation.current.toggled)
        updateStatusItem()
    }

    // MARK: - アクセシビリティの許可

    private func startKeyboardMonitor() {
        if keyboardMonitor.start() {
            return
        }

        // 許可を求めるダイアログを出し、許可されるまで定期的に再試行する
        let options = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        AXIsProcessTrustedWithOptions(options)

        permissionRetryTimer = Timer.scheduledTimer(withTimeInterval: 1.0, repeats: true) { [weak self] timer in
            MainActor.assumeIsolated {
                guard let self, self.keyboardMonitor.start() else { return }
                timer.invalidate()
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
            if keyboardMonitor.isRunning {
                button.title = style.symbols
                button.toolTip = "句読点: \(style.symbols)（\(triggerKey.usageDescription)）"
            } else {
                button.title = "⚠︎"
                button.toolTip = "アクセシビリティの許可が必要です"
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
            triggerKeyRecorder.window?.makeKeyAndOrderFront(nil)
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

        NSApp.activate()
        recorder.showWindow(nil)
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
        guard let url = URL(
            string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility"
        ) else {
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
