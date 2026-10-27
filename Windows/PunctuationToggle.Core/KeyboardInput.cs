namespace PunctuationToggle;

public enum KeyboardInputKind
{
    KeyDown,
    KeyUp,

    /// <summary>
    /// マウスのクリックやホイール操作。
    /// Ctrl＋クリックなどを、修飾キーの単独押しと区別するために使う。
    /// </summary>
    Mouse,
}

/// <summary>
/// キーボード・マウスの入力のうち、切り替えキーの判定に必要な情報だけを取り出したもの。
/// 低レベルフックで受け取った入力から作る（<see cref="KeyboardHookInput"/> を参照）。
/// </summary>
/// <param name="Kind">入力の種類。</param>
/// <param name="VirtualKey">キーの仮想キーコード（<see cref="KeyboardInputKind.Mouse"/> では 0）。</param>
/// <param name="IsRepeat">キーを押しっぱなしにしたことによるリピートか。</param>
/// <param name="OtherModifiersHeld">このキー以外の修飾キー（Shift・Ctrl・Alt・Windows）が押されているか。</param>
public readonly record struct KeyboardInput(
    KeyboardInputKind Kind,
    int VirtualKey = 0,
    bool IsRepeat = false,
    bool OtherModifiersHeld = false)
{
    public static KeyboardInput KeyDown(int virtualKey, bool isRepeat = false, bool otherModifiersHeld = false) =>
        new(KeyboardInputKind.KeyDown, virtualKey, isRepeat, otherModifiersHeld);

    public static KeyboardInput KeyUp(int virtualKey) => new(KeyboardInputKind.KeyUp, virtualKey);

    public static KeyboardInput Mouse { get; } = new(KeyboardInputKind.Mouse);
}

/// <summary>
/// 低レベルキーボードフック（WH_KEYBOARD_LL）で受け取った入力を <see cref="KeyboardInput"/> に変換する。
/// </summary>
/// <remarks>Windows の API を使わずにテストできるよう、キーの状態の問い合わせは呼び出し元から受け取る。</remarks>
public static class KeyboardHookInput
{
    // フックの wParam で渡されるメッセージ（WinUser.h）
    public const int KeyDownMessage = 0x0100; // WM_KEYDOWN
    public const int KeyUpMessage = 0x0101; // WM_KEYUP
    public const int SystemKeyDownMessage = 0x0104; // WM_SYSKEYDOWN（Alt を押しているとき、または F10）
    public const int SystemKeyUpMessage = 0x0105; // WM_SYSKEYUP

    /// <summary>
    /// KBDLLHOOKSTRUCT の flags で、整合性レベルが低いプロセスから注入された入力を示すビット（LLKHF_LOWER_IL_INJECTED）。
    /// </summary>
    /// <remarks>
    /// このビットが立つ入力は LLKHF_INJECTED（0x10）も立つ。
    /// https://learn.microsoft.com/windows/win32/api/winuser/ns-winuser-kbdllhookstruct
    /// </remarks>
    public const int LowerIntegrityLevelInjectedFlag = 0x02;

    /// <summary>
    /// フックで受け取った入力を変換する。判定に使わない入力では null を返す。
    /// </summary>
    /// <param name="message">フックの wParam（WM_KEYDOWN など）。</param>
    /// <param name="virtualKey">KBDLLHOOKSTRUCT の vkCode。</param>
    /// <param name="flags">KBDLLHOOKSTRUCT の flags。</param>
    /// <param name="isKeyDown">
    /// 仮想キーコードのキーが押されているか（GetAsyncKeyState）。
    /// フックはキーの状態が更新される前に呼ばれるので、押し下げのキー自身は前回の状態が返る。
    /// </param>
    /// <remarks>
    /// 整合性レベルが低いプロセス（ブラウザーのサンドボックスなど）が注入した入力は、
    /// 利用者が押していないのに句読点を切り替えたり、キーを握りつぶしたりできないよう、判定に使わない。
    /// 同じ整合性レベルのプロセスが注入した入力（PowerToys の Keyboard Manager や AutoHotkey でキーを割り当て直したもの）は使う。
    /// </remarks>
    public static KeyboardInput? FromHookMessage(int message, int virtualKey, int flags, Func<int, bool> isKeyDown)
    {
        ArgumentNullException.ThrowIfNull(isKeyDown);

        if ((flags & LowerIntegrityLevelInjectedFlag) != 0)
        {
            return null;
        }

        switch (message)
        {
            case KeyDownMessage or SystemKeyDownMessage:
                // このキーがすでに押されていれば、押しっぱなしによるリピート
                var isRepeat = isKeyDown(virtualKey);
                var otherModifiersHeld = VirtualKeys.Modifiers.Any(
                    modifier => modifier != virtualKey && isKeyDown(modifier));
                return KeyboardInput.KeyDown(virtualKey, isRepeat, otherModifiersHeld);

            case KeyUpMessage or SystemKeyUpMessage:
                return KeyboardInput.KeyUp(virtualKey);

            default:
                return null;
        }
    }
}
