import ApplicationServices
import CoreGraphics
import Foundation

/// `CGEventTap` で、すべてのアプリへのキーボード・マウスの入力を監視する。
/// 利用にはアクセシビリティの許可が必要。
///
/// キー入力は、切り替えキーの入力をほかのアプリに渡さないために、入力を止められるタップ（`.defaultTap`）で受け取る。
/// マウスのクリックとスクロールは「修飾キーを押している間に使われたか」を知るためだけに使い、止めることはないので、
/// 入力の流れを止めないタップ（`.listenOnly`）で別に受け取る。入力を止められるタップは、コールバックが返るまで
/// その入力をほかのアプリに渡さないため、1秒に何十回も起きるスクロールを通すと、メインスレッドが詰まったときに
/// すべてのアプリのスクロールが遅れ、タップが遅すぎると macOS に無効にされる（キー入力の監視まで止まる）。
///
/// 2つのタップの入力は、どちらもメインの RunLoop で処理する。listen-only のタップには入力が後から届くことがあり、
/// 修飾キーを押したままクリックしてすぐに離すと、離した入力の後にクリックが届く可能性があるが、
/// 人の操作の間隔ではまず起きないため、許容している。
final class KeyboardMonitor {
    /// 入力ごとにメインスレッドで呼ばれる。true を返すと、その入力はほかのアプリに渡さない。
    /// マウスの入力では、返した値は使わない（止められない）。
    var inputHandler: ((KeyboardInput) -> Bool)?

    /// 入力を取りこぼした可能性があるとき（macOS にタップを一時的に無効にされたとき）に、メインスレッドで呼ばれる。
    /// 判定の途中の状態（押している修飾キーなど）は、実際の状態と食い違っている可能性があるので、破棄する
    var inputDidReset: (() -> Void)?

    /// 作ったイベントタップと、それをメインの RunLoop に登録したソース
    private struct EventTap {
        let port: CFMachPort
        let runLoopSource: CFRunLoopSource
    }

    private var keyTap: EventTap?
    private var mouseTap: EventTap?

    /// 監視を始めているか。
    /// 始めた後にアクセシビリティの許可が外されても true のままなので、許可は `isPermitted` で確かめる
    var isRunning: Bool {
        keyTap != nil
    }

    /// アクセシビリティが今も許可されているか。
    /// 許可を外されてもイベントタップは残り、入力が届かなくなるだけなので、別に確かめる
    var isPermitted: Bool {
        AXIsProcessTrusted()
    }

    private static let keyEventTypes: [CGEventType] = [.keyDown, .keyUp, .flagsChanged]
    private static let mouseEventTypes: [CGEventType] = [
        .leftMouseDown, .rightMouseDown, .otherMouseDown, .scrollWheel,
    ]

    isolated deinit {
        // userInfo に渡した self は保持していないので、解放した後にコールバックが呼ばれないよう、タップを片付ける
        stop()
    }

    /// 監視を始める。アクセシビリティが許可されていない場合は false を返す。
    @discardableResult
    func start() -> Bool {
        guard keyTap == nil else { return true }

        guard let keyTap = createTap(options: .defaultTap, eventTypes: Self.keyEventTypes) else {
            return false
        }
        guard let mouseTap = createTap(options: .listenOnly, eventTypes: Self.mouseEventTypes) else {
            Self.remove(keyTap)
            return false
        }
        self.keyTap = keyTap
        self.mouseTap = mouseTap
        return true
    }

    func stop() {
        for tap in [keyTap, mouseTap].compactMap(\.self) {
            Self.remove(tap)
        }
        keyTap = nil
        mouseTap = nil
    }

    private func createTap(options: CGEventTapOptions, eventTypes: [CGEventType]) -> EventTap? {
        let eventMask = eventTypes.reduce(CGEventMask(0)) { mask, eventType in mask | (1 << eventType.rawValue) }

        let callback: CGEventTapCallBack = { _, type, event, userInfo in
            guard let userInfo else {
                return Unmanaged.passUnretained(event)
            }

            // CGEvent は Sendable でないため、メインアクターに渡す前に判定に使う値だけを取り出す
            let values = EventValues(event)
            let monitor = Unmanaged<KeyboardMonitor>.fromOpaque(userInfo)

            // タップはメインの RunLoop に登録しているので、メインスレッドで呼ばれる
            let shouldSuppress = MainActor.assumeIsolated {
                monitor.takeUnretainedValue().handle(type: type, values: values)
            }
            // listen-only のタップでは、返した値は使われない
            return shouldSuppress ? nil : Unmanaged.passUnretained(event)
        }

        guard
            let port = CGEvent.tapCreate(
                tap: .cgSessionEventTap,
                place: .headInsertEventTap,
                options: options,
                eventsOfInterest: eventMask,
                callback: callback,
                userInfo: Unmanaged.passUnretained(self).toOpaque()
            )
        else {
            return nil
        }

        guard let source = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, port, 0) else {
            CFMachPortInvalidate(port)
            return nil
        }

        CFRunLoopAddSource(CFRunLoopGetMain(), source, .commonModes)
        CGEvent.tapEnable(tap: port, enable: true)
        return EventTap(port: port, runLoopSource: source)
    }

    private static func remove(_ tap: EventTap) {
        CGEvent.tapEnable(tap: tap.port, enable: false)
        CFMachPortInvalidate(tap.port)
        CFRunLoopRemoveSource(CFRunLoopGetMain(), tap.runLoopSource, .commonModes)
    }

    private func handle(type: CGEventType, values: EventValues) -> Bool {
        switch type {
        case .tapDisabledByTimeout, .tapDisabledByUserInput:
            // 処理が遅いなどの理由で macOS にタップを無効化された場合は、有効に戻す。
            // どちらのタップが無効にされたかはコールバックでは分からないので、両方を有効にする（有効なタップには何も起きない）。
            // 許可が外された場合は有効に戻し続けず、利用者に知らせる処理（inputDidReset を受けた側）に任せる
            if isPermitted {
                for tap in [keyTap, mouseTap].compactMap(\.self) {
                    CGEvent.tapEnable(tap: tap.port, enable: true)
                }
            }
            // 無効になっていた間の入力は届いていないので、判定の途中の状態を破棄してもらう
            inputDidReset?()
            return false

        default:
            guard let kind = KeyboardEventKind(type),
                let input = KeyboardInput(
                    kind: kind,
                    keyCode: values.keyCode,
                    flags: values.flags,
                    isAutorepeat: values.isAutorepeat
                )
            else {
                return false
            }
            return inputHandler?(input) ?? false
        }
    }
}

/// `CGEvent` から取り出した、判定に使う値
private struct EventValues: Sendable {
    let keyCode: UInt16
    let flags: UInt64
    let isAutorepeat: Bool

    nonisolated init(_ event: CGEvent) {
        keyCode = UInt16(truncatingIfNeeded: event.getIntegerValueField(.keyboardEventKeycode))
        flags = event.flags.rawValue
        isAutorepeat = event.getIntegerValueField(.keyboardEventAutorepeat) != 0
    }
}

extension KeyboardEventKind {
    /// 判定に使わない種類（タップの無効化の通知など）では nil を返す。
    init?(_ type: CGEventType) {
        switch type {
        case .keyDown: self = .keyDown
        case .keyUp: self = .keyUp
        case .flagsChanged: self = .flagsChanged
        case .leftMouseDown, .rightMouseDown, .otherMouseDown, .scrollWheel: self = .mouse
        default: return nil
        }
    }
}
