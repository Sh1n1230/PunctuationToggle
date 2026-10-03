/// 1つの入力に対して、句読点を切り替えるか、入力をほかのアプリに渡さないかの判断。
struct InputDecision: Equatable, Sendable {
    var shouldToggle = false
    var shouldSuppress = false

    static let passThrough = InputDecision()
}

/// 入力を順に受け取り、切り替えキーが押されたことを検出する。
///
/// - 修飾キーの場合: 単独で押して離したときだけ切り替える。押している間にほかのキー・修飾キー・
///   マウスが使われた場合（Command＋C など）は切り替えない。入力はすべてそのまま渡す。
/// - それ以外のキーの場合: 修飾キーなしで押されたときに切り替え、そのキーの入力は渡さない。
///   修飾キーと一緒に押された場合（Command＋F13 など）はショートカットとしてそのまま渡す。
struct TriggerKeyDetector: Sendable {
    var triggerKey: TriggerKey {
        didSet { reset() }
    }

    // 修飾キーを押してから離すまでの間に、ほかの入力があったか
    private var isTriggerModifierHeld = false
    private var wasUsedWithOtherInput = false

    // 修飾キー以外の切り替えキーを押し下げ中で、離すまで入力を渡さないか
    private var isSuppressingTriggerKey = false

    init(triggerKey: TriggerKey) {
        self.triggerKey = triggerKey
    }

    mutating func handle(_ input: KeyboardInput) -> InputDecision {
        triggerKey.isModifier ? handleWithModifierTrigger(input) : handleWithRegularTrigger(input)
    }

    mutating func reset() {
        isTriggerModifierHeld = false
        wasUsedWithOtherInput = false
        isSuppressingTriggerKey = false
    }

    private mutating func handleWithModifierTrigger(_ input: KeyboardInput) -> InputDecision {
        switch input {
        case let .modifierChanged(keyCode, isPressed, otherModifiersHeld)
            where keyCode == triggerKey.keyCode:
            if isPressed {
                isTriggerModifierHeld = true
                // Shift を押したまま右Command を押した場合なども、単独押しとはみなさない
                wasUsedWithOtherInput = otherModifiersHeld
            } else if isTriggerModifierHeld {
                isTriggerModifierHeld = false
                return InputDecision(shouldToggle: !wasUsedWithOtherInput)
            }

        case .modifierChanged(_, isPressed: true, _), .keyDown, .mouse:
            if isTriggerModifierHeld {
                wasUsedWithOtherInput = true
            }

        case .modifierChanged(_, isPressed: false, _), .keyUp:
            break
        }
        return .passThrough
    }

    private mutating func handleWithRegularTrigger(_ input: KeyboardInput) -> InputDecision {
        switch input {
        case let .keyDown(keyCode, isRepeat, modifiersHeld) where keyCode == triggerKey.keyCode:
            if isSuppressingTriggerKey {
                // 押しっぱなしによるリピート
                return InputDecision(shouldSuppress: true)
            }
            if modifiersHeld {
                return .passThrough
            }
            isSuppressingTriggerKey = true
            return InputDecision(shouldToggle: !isRepeat, shouldSuppress: true)

        case let .keyUp(keyCode) where keyCode == triggerKey.keyCode:
            if isSuppressingTriggerKey {
                isSuppressingTriggerKey = false
                return InputDecision(shouldSuppress: true)
            }
            return .passThrough

        default:
            return .passThrough
        }
    }
}
