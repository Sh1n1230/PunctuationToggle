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
    /// フックで受け取った入力を変換する。判定に使わない入力では null を返す。
    /// </summary>
    /// <param name="message">フックの wParam（WM_KEYDOWN など）。</param>
    /// <param name="virtualKey">KBDLLHOOKSTRUCT の vkCode。</param>
    /// <param name="isKeyDown">
    /// 仮想キーコードのキーが押されているか（GetAsyncKeyState）。
    /// フックはキーの状態が更新される前に呼ばれるので、押し下げのキー自身は前回の状態が返る。
    /// </param>
    public static KeyboardInput? FromHookMessage(int message, int virtualKey, Func<int, bool> isKeyDown)
    {
        ArgumentNullException.ThrowIfNull(isKeyDown);

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
