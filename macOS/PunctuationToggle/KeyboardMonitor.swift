import CoreGraphics
import Foundation

/// `CGEventTap` で、すべてのアプリへのキーボード・マウスの入力を監視する。
/// 利用にはアクセシビリティの許可が必要。
final class KeyboardMonitor {
    /// 入力ごとにメインスレッドで呼ばれる。true を返すと、その入力はほかのアプリに渡さない。
    var inputHandler: ((KeyboardInput) -> Bool)?

    private var eventTap: CFMachPort?
    private var runLoopSource: CFRunLoopSource?

    var isRunning: Bool {
        eventTap != nil
    }

    /// 監視を始める。アクセシビリティが許可されていない場合は false を返す。
    @discardableResult
    func start() -> Bool {
        guard eventTap == nil else { return true }

        let eventTypes: [CGEventType] = [
            .keyDown, .keyUp, .flagsChanged,
            .leftMouseDown, .rightMouseDown, .otherMouseDown, .scrollWheel,
        ]
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
            return shouldSuppress ? nil : Unmanaged.passUnretained(event)
        }

        guard
            let tap = CGEvent.tapCreate(
                tap: .cgSessionEventTap,
                place: .headInsertEventTap,
                options: .defaultTap,
                eventsOfInterest: eventMask,
                callback: callback,
                userInfo: Unmanaged.passUnretained(self).toOpaque()
            )
        else {
            return false
        }

        guard let source = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, tap, 0) else {
            CFMachPortInvalidate(tap)
            return false
        }

        CFRunLoopAddSource(CFRunLoopGetMain(), source, .commonModes)
        CGEvent.tapEnable(tap: tap, enable: true)

        eventTap = tap
        runLoopSource = source
        return true
    }

    func stop() {
        if let eventTap {
            CGEvent.tapEnable(tap: eventTap, enable: false)
            CFMachPortInvalidate(eventTap)
        }
        if let runLoopSource {
            CFRunLoopRemoveSource(CFRunLoopGetMain(), runLoopSource, .commonModes)
        }
        eventTap = nil
        runLoopSource = nil
    }

    private func handle(type: CGEventType, values: EventValues) -> Bool {
        switch type {
        case .tapDisabledByTimeout, .tapDisabledByUserInput:
            // 処理が遅いなどの理由で macOS にタップを無効化された場合は、有効に戻す
            if let eventTap {
                CGEvent.tapEnable(tap: eventTap, enable: true)
            }
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
