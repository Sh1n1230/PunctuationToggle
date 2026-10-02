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

            // タップはメインの RunLoop に登録しているので、メインスレッドで呼ばれる
            return MainActor.assumeIsolated {
                let monitor = Unmanaged<KeyboardMonitor>.fromOpaque(userInfo).takeUnretainedValue()
                let shouldSuppress = monitor.handle(type: type, event: event)
                return shouldSuppress ? nil : Unmanaged.passUnretained(event)
            }
        }

        guard let tap = CGEvent.tapCreate(
            tap: .cgSessionEventTap,
            place: .headInsertEventTap,
            options: .defaultTap,
            eventsOfInterest: eventMask,
            callback: callback,
            userInfo: Unmanaged.passUnretained(self).toOpaque()
        ) else {
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

    private func handle(type: CGEventType, event: CGEvent) -> Bool {
        switch type {
        case .tapDisabledByTimeout, .tapDisabledByUserInput:
            // 処理が遅いなどの理由で macOS にタップを無効化された場合は、有効に戻す
            if let eventTap {
                CGEvent.tapEnable(tap: eventTap, enable: true)
            }
            return false

        default:
            guard let input = KeyboardInput(type: type, event: event) else { return false }
            return inputHandler?(input) ?? false
        }
    }
}

extension KeyboardInput {
    /// Command・Shift・Option・Control（左右を区別しない）
    private static let shortcutModifierFlags: CGEventFlags = [
        .maskCommand, .maskShift, .maskAlternate, .maskControl,
    ]

    /// `CGEvent` から変換する。判定に使わない入力（Caps Lock など）では nil を返す。
    init?(type: CGEventType, event: CGEvent) {
        let keyCode = UInt16(truncatingIfNeeded: event.getIntegerValueField(.keyboardEventKeycode))
        let flags = event.flags

        switch type {
        case .flagsChanged:
            guard let modifier = TriggerKey.Modifier(keyCode: keyCode) else { return nil }
            let otherModifierMasks = TriggerKey.Modifier.allFlagMasks & ~modifier.flagMask
            self = .modifierChanged(
                keyCode: keyCode,
                isPressed: flags.rawValue & modifier.flagMask != 0,
                otherModifiersHeld: flags.rawValue & otherModifierMasks != 0
            )

        case .keyDown:
            self = .keyDown(
                keyCode: keyCode,
                isRepeat: event.getIntegerValueField(.keyboardEventAutorepeat) != 0,
                modifiersHeld: !flags.intersection(Self.shortcutModifierFlags).isEmpty
            )

        case .keyUp:
            self = .keyUp(keyCode: keyCode)

        case .leftMouseDown, .rightMouseDown, .otherMouseDown, .scrollWheel:
            self = .mouse

        default:
            return nil
        }
    }
}
