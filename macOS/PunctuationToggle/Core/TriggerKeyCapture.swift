/// 切り替えキーの設定画面で、次に押されたキーを読み取る。
///
/// - 修飾キーは、単独で押して離したときに読み取る（Command＋C のような組み合わせは無視する）。
/// - それ以外のキーは、押したときに読み取る。読み取ったキーの入力はほかのアプリに渡さない。
///   ただし、修飾キーと一緒に押したキー（Command＋F5 など）は、OS やアプリのショートカットなので読み取らずに渡す。
/// - Esc でキャンセルする。
/// - 設定画面が前面にない間の入力は `handleWhileInactive(_:)` に渡し、読み取らずにほかのアプリへ渡す。
nonisolated struct TriggerKeyCapture: Sendable {
    nonisolated enum Outcome: Equatable, Sendable {
        /// まだキーが押されていない
        case waiting
        /// 切り替えキーとして使えるキーが押された
        case captured(TriggerKey)
        /// 切り替えキーとして使えないキーが押された（読み取りは続ける）
        case rejected(keyCode: UInt16)
        /// Esc が押された
        case cancelled
    }

    nonisolated struct Result: Equatable, Sendable {
        var outcome: Outcome
        var shouldSuppress: Bool
    }

    // 単独で押されている修飾キー
    private var pendingModifierKeyCode: UInt16?
    // 押し下げを渡さなかったキー（離したときの入力も渡さない）
    private var suppressedKeyCode: UInt16?

    mutating func handle(_ input: KeyboardInput) -> Result {
        switch input {
        case let .modifierChanged(keyCode, isPressed: true, otherModifiersHeld):
            let isAlone = pendingModifierKeyCode == nil && !otherModifiersHeld
            pendingModifierKeyCode = isAlone ? keyCode : nil
            return Result(outcome: .waiting, shouldSuppress: false)

        case let .modifierChanged(keyCode, isPressed: false, _):
            guard pendingModifierKeyCode == keyCode else {
                return Result(outcome: .waiting, shouldSuppress: false)
            }
            pendingModifierKeyCode = nil
            return Result(outcome: Self.outcome(for: keyCode), shouldSuppress: false)

        // 修飾キーと一緒に押したキーは、VoiceOver のオン・オフ（Command＋F5）などのショートカットなので、
        // 読み取らずに渡す（読み取ると、F5 が切り替えキーとして保存され、ショートカットも効かなくなる）
        case .keyDown(keyCode: _, isRepeat: _, modifiersHeld: true):
            pendingModifierKeyCode = nil
            return Result(outcome: .waiting, shouldSuppress: false)

        case let .keyDown(keyCode, _, _):
            pendingModifierKeyCode = nil
            suppressedKeyCode = keyCode
            let outcome = keyCode == KeyCode.escape ? .cancelled : Self.outcome(for: keyCode)
            return Result(outcome: outcome, shouldSuppress: true)

        case let .keyUp(keyCode):
            let shouldSuppress = suppressedKeyCode == keyCode
            if shouldSuppress {
                suppressedKeyCode = nil
            }
            return Result(outcome: .waiting, shouldSuppress: shouldSuppress)

        case .mouse:
            pendingModifierKeyCode = nil
            return Result(outcome: .waiting, shouldSuppress: false)
        }
    }

    /// 設定画面が前面にない（ほかのアプリを操作している）ときの入力を処理する。
    /// キーは読み取らず、入力もほかのアプリに渡す。
    /// ただし、前面にあったときに押し下げを渡さなかったキーは、離したときの入力も渡さない。
    mutating func handleWhileInactive(_ input: KeyboardInput) -> Result {
        pendingModifierKeyCode = nil
        guard case let .keyUp(keyCode) = input, suppressedKeyCode == keyCode else {
            return Result(outcome: .waiting, shouldSuppress: false)
        }
        suppressedKeyCode = nil
        return Result(outcome: .waiting, shouldSuppress: true)
    }

    private static func outcome(for keyCode: UInt16) -> Outcome {
        if let key = TriggerKey(keyCode: keyCode) {
            .captured(key)
        } else {
            .rejected(keyCode: keyCode)
        }
    }
}
