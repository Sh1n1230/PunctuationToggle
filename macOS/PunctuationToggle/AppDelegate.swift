import AppKit
import OSLog
import ServiceManagement

/// メニューバーに現在の句読点を表示し、切り替えキーで切り替える。
final class AppDelegate: NSObject, NSApplicationDelegate, NSMenuDelegate {
    private let keyboardMonitor = KeyboardMonitor()
    private var detector = TriggerKeyDetector(triggerKey: TriggerKeyStore.triggerKey)
    private var triggerKeyRecorder: TriggerKeyRecorderWindowController?
    private var permissionRetryTimer: Timer?
    /// `permissionRetryTimer` の間隔（秒）。待っていないときは nil
    private var permissionRetryInterval: Double?
    /// アクセシビリティの許可を待ち始めた時刻。待っていないときは nil
    private var permissionWaitStartDate: Date?
    private var isShowingSaveFailureAlert = false
    /// ログイン項目として起動されたか。起動の Apple Event は起動の処理の間しか読めないので、覚えておく
    private var isLaunchedAsLoginItem = false

    private var statusItem: NSStatusItem?
    private let toggleMenuItem = NSMenuItem()
    private let usageMenuItem = NSMenuItem()
    /// セキュア入力が有効で切り替えキーが届かないことを知らせる項目（有効な間だけ表示する）
    private let secureInputMenuItem = NSMenuItem()
    /// 句読点が切り替わらない日本語入力を使っていることを知らせる項目（その間だけ表示する）
    private let unsupportedInputSourceMenuItem = NSMenuItem()
    private let changeTriggerKeyMenuItem = NSMenuItem()
    private let launchAtLoginMenuItem = NSMenuItem()

    /// `kAXTrustedCheckOptionPrompt` の値（ApplicationServices の AXUIElement.h）。
    /// 定数はグローバルな `var` として読み込まれ、Swift 6 の言語モードでは並行処理で安全でないとして使えないため、値を書く
    private static let trustedCheckOptionPromptKey = "AXTrustedCheckOptionPrompt"

    func applicationWillFinishLaunching(_ notification: Notification) {
        isLaunchedAsLoginItem = LaunchGuidance.isLaunchedAsLoginItem()
    }

    func applicationDidFinishLaunching(_ notification: Notification) {
        // 二重に起動すると切り替えキーで2回切り替わって元に戻ってしまうため、後から起動した方は終わる
        // （~/Applications のアプリとビルドしたアプリなど、別の場所のアプリでも同じバンドル ID なら対象にする）。
        // 利用者が開いた場合は、何も起きないように見えないよう知らせる。ログイン時の自動起動では、ログインのたびに
        // 知らせることになるので、黙って終わる
        if let earlierInstance = earlierRunningInstance() {
            if !isLaunchedAsLoginItem {
                LaunchGuidance.showAlreadyRunningAlert(runningApplication: earlierInstance)
            }
            NSApp.terminate(nil)
            return
        }

        createStatusItem()

        keyboardMonitor.inputHandler = { [weak self] input in
            self?.handle(input) ?? false
        }
        keyboardMonitor.inputDidReset = { [weak self] in
            self?.handleInputReset()
        }

        // システム設定側で変更された場合も表示を追従させる
        DistributedNotificationCenter.default().addObserver(
            self,
            selector: #selector(punctuationSettingDidChange),
            name: JapaneseInputPunctuation.didChangeNotification,
            object: nil,
            suspensionBehavior: .deliverImmediately
        )

        // アクセシビリティの許可は場所に結び付くため、許可を求める前に案内する
        LaunchGuidance.suggestMovingToApplicationsIfNeeded()

        startKeyboardMonitor()
        updateStatusItem()
    }

    /// 起動中にもう一度開かれたとき（Finder や Spotlight から）に、メニューを開く。
    /// ウィンドウを持たないアプリなので、何もしないと何も起きないように見え、メニューバーの項目が隠れているときには
    /// メニューを開く手段も無くなるため
    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows flag: Bool) -> Bool {
        statusItem?.button?.performClick(nil)
        return false
    }

    func applicationWillTerminate(_ notification: Notification) {
        keyboardMonitor.stop()
    }

    /// 先に起動して動いている同じアプリ。無ければ nil。
    /// 同時に起動した場合に両方が終わらないよう、起動した時刻（同じならプロセス ID）が早い方を残す（判定は Core）
    private func earlierRunningInstance() -> NSRunningApplication? {
        guard let bundleIdentifier = Bundle.main.bundleIdentifier else { return nil }
        let others = NSRunningApplication.runningApplications(withBundleIdentifier: bundleIdentifier)
        let current = RunningInstance(NSRunningApplication.current)
        guard let earlier = current.earliestEarlierInstance(among: others.map(RunningInstance.init)) else {
            return nil
        }
        return others.first { other in other.processIdentifier == earlier.processIdentifier }
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

    /// 入力を取りこぼした可能性があるとき（イベントタップが一時的に無効にされたとき）に、判定の途中の状態を破棄する。
    /// 押している間のほかの入力（右Command＋C の C など）を取りこぼしたまま、離したときに単独押しと判断しないため
    private func handleInputReset() {
        detector.reset()
        triggerKeyRecorder?.resetCapture()
        // 許可を外されたことでタップが無効にされた場合は、ここで気付ける
        stopKeyboardMonitorIfPermissionRevoked()
    }

    private func togglePunctuationStyle() {
        let newStyle = JapaneseInputPunctuation.current.toggled
        if !JapaneseInputPunctuation.set(newStyle) {
            AppLog.logger.error("句読点の設定を保存できませんでした: \(newStyle.symbols, privacy: .public)")
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
        startWaitingForPermission()
    }

    /// 許可されたかを、定期的に（とメニューを開いたときに）確かめ始める。間隔は `PermissionRetrySchedule` で決める
    private func startWaitingForPermission() {
        permissionWaitStartDate = Date()
        schedulePermissionRetry(interval: PermissionRetrySchedule.interval(elapsedSinceWaitStart: 0))
    }

    private func stopWaitingForPermission() {
        permissionRetryTimer?.invalidate()
        permissionRetryTimer = nil
        permissionRetryInterval = nil
        permissionWaitStartDate = nil
    }

    private func schedulePermissionRetry(interval: Double) {
        permissionRetryTimer?.invalidate()
        permissionRetryInterval = interval
        // メインの RunLoop に登録したタイマーなので、メインスレッドで呼ばれる
        permissionRetryTimer = Timer.scheduledTimer(withTimeInterval: interval, repeats: true) { [weak self] _ in
            MainActor.assumeIsolated {
                self?.retryKeyboardMonitor()
            }
        }
    }

    /// アクセシビリティが許可されていれば監視を始める。許可を待っていない場合は何もしない
    private func retryKeyboardMonitor() {
        guard let permissionWaitStartDate else { return }

        if keyboardMonitor.start() {
            stopWaitingForPermission()
            updateStatusItem()
            return
        }

        let interval = PermissionRetrySchedule.interval(
            elapsedSinceWaitStart: Date().timeIntervalSince(permissionWaitStartDate)
        )
        if interval != permissionRetryInterval {
            schedulePermissionRetry(interval: interval)
        }
    }

    /// 監視を始めた後にアクセシビリティの許可が外されていたら、監視を止めて許可を待つ状態に戻す。
    /// 許可を外されてもイベントタップは残り、入力が届かなくなるだけなので、メニューバーの表示を「⚠︎」にして知らせる。
    /// 利用者が自分で外した場合もあるので、許可を求めるダイアログは出さない
    private func stopKeyboardMonitorIfPermissionRevoked() {
        guard keyboardMonitor.isRunning, !keyboardMonitor.isPermitted else { return }

        keyboardMonitor.stop()
        detector.reset()
        // 設定画面は入力が届かないと閉じられなくなる（Esc が効かない）ので、閉じる
        triggerKeyRecorder?.cancelCapture()
        startWaitingForPermission()
        updateStatusItem()
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

        secureInputMenuItem.title = "セキュア入力が有効なため、切り替えキーは使えません（パスワードの入力欄を閉じると戻ります）"
        secureInputMenuItem.isEnabled = false
        secureInputMenuItem.isHidden = true
        menu.addItem(secureInputMenuItem)

        unsupportedInputSourceMenuItem.isEnabled = false
        unsupportedInputSourceMenuItem.isHidden = true
        menu.addItem(unsupportedInputSourceMenuItem)

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

        menu.addItem(withTitle: Self.latestReleaseMenuTitle, action: #selector(openLatestRelease), keyEquivalent: "")
            .target = self
        menu.addItem(withTitle: "PunctuationToggle について", action: #selector(showAboutPanel), keyEquivalent: "")
            .target = self
        menu.addItem(withTitle: "終了", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")

        statusItem.menu = menu
    }

    /// 「最新版を確認…」の項目の名前。リリースのページと見比べられるよう、今の版を添える
    private static var latestReleaseMenuTitle: String {
        guard let version = Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String else {
            return "最新版を確認…"
        }
        return "最新版を確認…（今の版: \(version)）"
    }

    func menuNeedsUpdate(_ menu: NSMenu) {
        // 許可を外したことは入力が届かなくなるだけで知らされないため、メニューを開いたときに確かめる
        stopKeyboardMonitorIfPermissionRevoked()
        // 許可を待つ間隔を延ばした後も、メニューを開いたときにはすぐ確かめる
        retryKeyboardMonitor()
        updateStatusItem()
        updateInputEnvironmentMenuItems()
    }

    /// 切り替えキーや句読点の切り替えが効かない状態を、メニューを開いたときに知らせる
    private func updateInputEnvironmentMenuItems() {
        secureInputMenuItem.isHidden = !InputEnvironment.isSecureEventInputEnabled

        if let inputSourceName = InputEnvironment.unsupportedJapaneseInputSourceName() {
            unsupportedInputSourceMenuItem.title = "今の入力ソース（\(inputSourceName)）では、句読点は切り替わりません"
            unsupportedInputSourceMenuItem.isHidden = false
        } else {
            unsupportedInputSourceMenuItem.isHidden = true
        }
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
        updateLaunchAtLoginMenuItem()
    }

    private func updateLaunchAtLoginMenuItem() {
        let status = SMAppService.mainApp.status
        launchAtLoginMenuItem.state = status == .enabled ? .on : .off
        // 登録はできたが、システム設定の「ログイン項目と機能拡張」でオフにされているなど、利用者の操作が要る状態
        launchAtLoginMenuItem.title =
            status == .requiresApproval ? "ログイン時に起動（システム設定で承認が必要）" : "ログイン時に起動"
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
        // 承認待ちの状態では、登録し直しても変わらないので、承認する場所（システム設定のログイン項目）を開く
        if service.status == .requiresApproval {
            SMAppService.openSystemSettingsLoginItems()
            return
        }
        do {
            if service.status == .enabled {
                try service.unregister()
            } else {
                try service.register()
            }
        } catch {
            AppLog.logger.error("ログイン項目の変更に失敗しました: \(String(describing: error), privacy: .public)")
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

extension RunningInstance {
    init(_ application: NSRunningApplication) {
        self.init(
            launchDate: application.launchDate,
            processIdentifier: application.processIdentifier,
            isTerminated: application.isTerminated
        )
    }
}
