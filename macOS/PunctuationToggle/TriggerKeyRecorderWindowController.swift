import AppKit

/// 切り替えキーを設定するウィンドウ。
///
/// 開いている間は `handle(_:)` に入力を渡してもらい、次に押されたキーを新しい切り替えキーとして読み取る。
/// キー入力は `KeyboardMonitor` で受け取る（修飾キーの単独押しはウィンドウのイベントでは扱いにくいため）。
/// ほかのアプリでの入力を読み取ったり握りつぶしたりしないよう、このウィンドウが前面にある間だけ読み取り、
/// ほかのアプリに切り替えたら設定を中止して閉じる。
final class TriggerKeyRecorderWindowController: NSWindowController, NSWindowDelegate {
    /// 読み取りが終わったときに1度だけ呼ばれる。キャンセルされた場合は nil。
    private let completion: (TriggerKey?) -> Void
    private var capture = TriggerKeyCapture()
    private var isFinished = false

    private let statusLabel = NSTextField(wrappingLabelWithString: "")

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
        super.init(window: window)
        window.delegate = self

        buildContent(in: window, currentKey: currentKey)
        window.center()

        // NSPanel はアプリが前面でなくなると隠れる。隠れたまま開いていると切り替えキーが効かなくなるため、中止して閉じる
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
        ほかに F1〜F20、英数、かな が使えます。Esc でキャンセルします。
        """

    private func showRejectedMessage(keyCode: UInt16) {
        statusLabel.stringValue = "そのキー（キーコード \(keyCode)）は使えません。\n" + Self.instructionText
        statusLabel.textColor = .systemRed
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
