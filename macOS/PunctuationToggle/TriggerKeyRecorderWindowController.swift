import AppKit

/// 切り替えキーを設定するウィンドウ。
///
/// 開いている間は `handle(_:)` に入力を渡してもらい、次に押されたキーを新しい切り替えキーとして読み取る。
/// キー入力は `KeyboardMonitor` で受け取る（修飾キーの単独押しはウィンドウのイベントでは扱いにくいため）。
/// ほかのアプリでの入力を読み取ったり握りつぶしたりしないよう、このウィンドウが前面にある間だけ読み取り、
/// ほかのアプリに切り替えたら設定を中止して閉じる。
///
/// 開いている間は切り替えキーが効かないため、ウィンドウは常に見えるようにしておく。
/// ほかのアプリが前面のときにメニューから開くと、macOS がこのアプリを前面にしないことがあるため、
/// その場合はウィンドウをクリックしてもらうよう案内する。
final class TriggerKeyRecorderWindowController: NSWindowController, NSWindowDelegate {
    /// 読み取りが終わったときに1度だけ呼ばれる。キャンセルされた場合は nil。
    private let completion: (TriggerKey?) -> Void
    private var capture = TriggerKeyCapture()
    private var isFinished = false

    private let statusLabel = NSTextField(wrappingLabelWithString: "")
    private var lastAnnouncement: String?

    init(currentKey: TriggerKey, completion: @escaping (TriggerKey?) -> Void) {
        self.completion = completion

        let window = NSPanel(
            contentRect: NSRect(x: 0, y: 0, width: 380, height: 180),
            styleMask: [.titled, .closable],
            backing: .buffered,
            defer: false
        )
        window.title = "切り替えキーの設定"
        window.isReleasedWhenClosed = false
        window.level = .floating
        // NSPanel は既定でアプリが前面でない間は隠れる。前面にできなかったときに見えないまま開き続けないよう、常に表示する
        window.hidesOnDeactivate = false
        super.init(window: window)
        window.delegate = self

        buildContent(in: window, currentKey: currentKey)
        window.center()

        // 前面にしてから、ほかのアプリに切り替えた場合は中止する（開いたまま切り替えキーが効かない状態を残さない）
        NotificationCenter.default.addObserver(
            self,
            selector: #selector(applicationDidResignActive),
            name: NSApplication.didResignActiveNotification,
            object: nil
        )
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("init(coder:) is not supported")
    }

    /// 入力を1つ処理する。true を返した入力はほかのアプリに渡さない。
    func handle(_ input: KeyboardInput) -> Bool {
        guard !isFinished else { return false }

        let isActive = NSApp.isActive && window?.isKeyWindow == true
        let result = isActive ? capture.handle(input) : capture.handleWhileInactive(input)
        if !isActive, case .keyDown = input {
            showInactiveMessage()
        }
        switch result.outcome {
        case .waiting:
            break
        case let .captured(key):
            finish(with: key)
        case let .rejected(keyCode):
            showRejectedMessage(keyCode: keyCode)
        case .cancelled:
            finish(with: nil)
        }
        return result.shouldSuppress
    }

    /// 入力を取りこぼした後に、読み取りの途中の状態（押している修飾キーなど）を破棄する
    func resetCapture() {
        capture.reset()
    }

    /// 読み取りを中止して閉じる（キー入力の監視が止まったときなど）
    func cancelCapture() {
        finish(with: nil)
    }

    func windowWillClose(_ notification: Notification) {
        // タイトルバーのボタンで閉じられた場合（ウィンドウはすでに閉じつつある）
        finish(with: nil, closesWindow: false)
    }

    // MARK: - Private

    private func buildContent(in window: NSWindow, currentKey: TriggerKey) {
        let titleLabel = NSTextField(labelWithString: "新しい切り替えキーを押してください")
        titleLabel.font = .boldSystemFont(ofSize: NSFont.systemFontSize + 1)

        let currentKeyLabel = NSTextField(labelWithString: "現在の切り替えキー: \(currentKey.displayName)")

        statusLabel.stringValue = Self.instructionText
        statusLabel.textColor = .secondaryLabelColor
        statusLabel.font = .systemFont(ofSize: NSFont.smallSystemFontSize)
        statusLabel.preferredMaxLayoutWidth = 340

        let resetButton = NSButton(
            title: "初期設定（\(TriggerKey.defaultKey.displayName)）に戻す",
            target: self,
            action: #selector(resetToDefault)
        )
        let cancelButton = NSButton(title: "キャンセル", target: self, action: #selector(cancel))

        let buttonRow = NSStackView(views: [resetButton, cancelButton])
        buttonRow.orientation = .horizontal

        let contentStack = NSStackView(views: [titleLabel, currentKeyLabel, statusLabel, buttonRow])
        contentStack.orientation = .vertical
        contentStack.alignment = .leading
        contentStack.spacing = 10
        contentStack.edgeInsets = NSEdgeInsets(top: 20, left: 20, bottom: 20, right: 20)
        contentStack.setCustomSpacing(16, after: statusLabel)

        window.contentView = contentStack
    }

    private static let instructionText = """
        修飾キー（Command・Shift・Option・Control・fn）は単独で押して離してください。\
        ほかに F1〜F20、英数、かな が使えます。Esc でキャンセルします。\
        F1〜F12 などは、キーボードの設定によっては、明るさや音量などの特殊キーとして働き、設定できない場合があります\
        （fn を押しながら押すと設定できることがあります）。
        """

    private func showInactiveMessage() {
        showWarning("このウィンドウをクリックしてから、キーを押してください。")
    }

    private func showRejectedMessage(keyCode: UInt16) {
        showWarning("そのキー（\(Self.keyName(for: keyCode))）は使えません。")
    }

    /// 使えないキーの名前。文字にならないキーは Core の表、文字キーは今のキーボード配列での文字で示し、
    /// どちらでも求められないときだけキーコードを示す
    private static func keyName(for keyCode: UInt16) -> String {
        KeyNames.name(for: keyCode) ?? KeyboardLayoutKeyNames.name(for: keyCode) ?? "キーコード \(keyCode)"
    }

    /// 案内の1行目に注意を表示する。
    /// 小さい文字のオレンジ・赤は背景とのコントラストが足りない（WCAG 2.x の 4.5:1 を下回る）ため、
    /// 文字は文字用の色にし、注意であることは記号で示す（色だけに頼らない）
    private func showWarning(_ message: String) {
        let font = statusLabel.font ?? .systemFont(ofSize: NSFont.smallSystemFontSize)
        let text = NSMutableAttributedString()
        if let symbol = NSImage(systemSymbolName: "exclamationmark.triangle.fill", accessibilityDescription: "注意") {
            let attachment = NSTextAttachment()
            attachment.image = symbol.withSymbolConfiguration(.init(pointSize: font.pointSize, weight: .regular))
            text.append(NSAttributedString(attachment: attachment))
            text.append(NSAttributedString(string: " "))
        }
        text.append(NSAttributedString(string: message, attributes: [.foregroundColor: NSColor.labelColor]))
        text.append(
            NSAttributedString(
                string: "\n" + Self.instructionText,
                attributes: [.foregroundColor: NSColor.secondaryLabelColor]
            )
        )
        text.addAttribute(.font, value: font, range: NSRange(location: 0, length: text.length))
        statusLabel.attributedStringValue = text
        announce(message)
    }

    /// 案内が変わったことを VoiceOver に読み上げさせる。
    /// 画面を見ていない利用者に、キーを押しても何も起きないように見えないようにする。
    /// 同じ案内を続けて読み上げないよう、前回と同じ文は送らない
    private func announce(_ message: String) {
        guard message != lastAnnouncement else { return }
        lastAnnouncement = message
        NSAccessibility.post(
            element: statusLabel,
            notification: .announcementRequested,
            userInfo: [
                .announcement: message,
                .priority: NSAccessibilityPriorityLevel.high.rawValue,
            ]
        )
    }

    @objc
    private func resetToDefault() {
        finish(with: .defaultKey)
    }

    @objc
    private func applicationDidResignActive(_ notification: Notification) {
        finish(with: nil)
    }

    @objc
    private func cancel() {
        finish(with: nil)
    }

    private func finish(with key: TriggerKey?, closesWindow: Bool = true) {
        guard !isFinished else { return }
        isFinished = true
        completion(key)
        if closesWindow {
            close()
        }
    }
}
