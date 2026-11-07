import Carbon.HIToolbox
import CoreGraphics
import IOKit.hidsystem
import Testing
@testable import PunctuationToggleCore

// Core は OS の API を使わないため、キーコードと修飾キーのビットを数値で書いている。
// テストは macOS でしか動かないので、ここでは SDK の定数と比べて、値の写し間違いが無いことを確かめる。

@Suite("Core の定数と SDK の定数")
struct KeyCodeConstantTests {
    @Test("キーコードが Carbon の kVK_* と一致する")
    func keyCodes() {
        #expect(Int(KeyCode.escape) == kVK_Escape)
        #expect(Int(KeyCode.capsLock) == kVK_CapsLock)
        #expect(Int(KeyCode.rightCommand) == kVK_RightCommand)
        #expect(Int(KeyCode.leftCommand) == kVK_Command)
        #expect(Int(KeyCode.leftShift) == kVK_Shift)
        #expect(Int(KeyCode.leftOption) == kVK_Option)
        #expect(Int(KeyCode.leftControl) == kVK_Control)
        #expect(Int(KeyCode.rightShift) == kVK_RightShift)
        #expect(Int(KeyCode.rightOption) == kVK_RightOption)
        #expect(Int(KeyCode.rightControl) == kVK_RightControl)
        #expect(Int(KeyCode.function) == kVK_Function)
        #expect(Int(KeyCode.eisu) == kVK_JIS_Eisu)
        #expect(Int(KeyCode.kana) == kVK_JIS_Kana)
        #expect(Int(KeyCode.returnKey) == kVK_Return)
        #expect(Int(KeyCode.tab) == kVK_Tab)
        #expect(Int(KeyCode.space) == kVK_Space)
        #expect(Int(KeyCode.delete) == kVK_Delete)
        #expect(Int(KeyCode.keypadEnter) == kVK_ANSI_KeypadEnter)
        #expect(Int(KeyCode.help) == kVK_Help)
        #expect(Int(KeyCode.home) == kVK_Home)
        #expect(Int(KeyCode.pageUp) == kVK_PageUp)
        #expect(Int(KeyCode.forwardDelete) == kVK_ForwardDelete)
        #expect(Int(KeyCode.end) == kVK_End)
        #expect(Int(KeyCode.pageDown) == kVK_PageDown)
        #expect(Int(KeyCode.leftArrow) == kVK_LeftArrow)
        #expect(Int(KeyCode.rightArrow) == kVK_RightArrow)
        #expect(Int(KeyCode.downArrow) == kVK_DownArrow)
        #expect(Int(KeyCode.upArrow) == kVK_UpArrow)
    }

    @Test("テンキーの 0〜9 のキーコードが kVK_ANSI_Keypad0〜9 と一致する")
    func keypadDigitKeyCodes() {
        let sdkKeypadDigits = [
            kVK_ANSI_Keypad0, kVK_ANSI_Keypad1, kVK_ANSI_Keypad2, kVK_ANSI_Keypad3, kVK_ANSI_Keypad4,
            kVK_ANSI_Keypad5, kVK_ANSI_Keypad6, kVK_ANSI_Keypad7, kVK_ANSI_Keypad8, kVK_ANSI_Keypad9,
        ]
        #expect(KeyCode.keypadDigits.map(Int.init) == sdkKeypadDigits)
    }

    @Test("F1〜F20 のキーコードが kVK_F1〜kVK_F20 と一致する")
    func functionKeyCodes() {
        let sdkFunctionKeys = [
            kVK_F1, kVK_F2, kVK_F3, kVK_F4, kVK_F5, kVK_F6, kVK_F7, kVK_F8, kVK_F9, kVK_F10,
            kVK_F11, kVK_F12, kVK_F13, kVK_F14, kVK_F15, kVK_F16, kVK_F17, kVK_F18, kVK_F19, kVK_F20,
        ]
        #expect(KeyCode.functionKeys.map(Int.init) == sdkFunctionKeys)
    }

    @Test("左右を区別しない修飾キーのビットが CGEventFlags と一致する")
    func eventFlagMasks() {
        #expect(EventFlagMask.shift == CGEventFlags.maskShift.rawValue)
        #expect(EventFlagMask.control == CGEventFlags.maskControl.rawValue)
        #expect(EventFlagMask.option == CGEventFlags.maskAlternate.rawValue)
        #expect(EventFlagMask.command == CGEventFlags.maskCommand.rawValue)
    }

    @Test("左右を区別する修飾キーのビットが IOKit の NX_DEVICE*KEYMASK と一致する")
    func deviceDependentFlagMasks() {
        #expect(TriggerKey.Modifier.leftControl.flagMask == UInt64(NX_DEVICELCTLKEYMASK))
        #expect(TriggerKey.Modifier.rightControl.flagMask == UInt64(NX_DEVICERCTLKEYMASK))
        #expect(TriggerKey.Modifier.leftShift.flagMask == UInt64(NX_DEVICELSHIFTKEYMASK))
        #expect(TriggerKey.Modifier.rightShift.flagMask == UInt64(NX_DEVICERSHIFTKEYMASK))
        #expect(TriggerKey.Modifier.leftOption.flagMask == UInt64(NX_DEVICELALTKEYMASK))
        #expect(TriggerKey.Modifier.rightOption.flagMask == UInt64(NX_DEVICERALTKEYMASK))
        #expect(TriggerKey.Modifier.leftCommand.flagMask == UInt64(NX_DEVICELCMDKEYMASK))
        #expect(TriggerKey.Modifier.rightCommand.flagMask == UInt64(NX_DEVICERCMDKEYMASK))
        #expect(TriggerKey.Modifier.function.flagMask == CGEventFlags.maskSecondaryFn.rawValue)
    }
}
