using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PunctuationToggle;

/// <summary>
/// 低レベルキーボード・マウスフックで、すべてのアプリへの入力を監視する。
/// </summary>
/// <remarks>
/// フックはこのオブジェクトを作ったスレッドのメッセージループで呼ばれる。
/// 処理に時間がかかると Windows にフックを外されるため、ハンドラーでは重い処理をしないこと。
/// </remarks>
internal sealed class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_XBUTTONDOWN = 0x020B;
    private const int WM_MOUSEHWHEEL = 0x020E;
    private const short KeyDownBit = unchecked((short)0x8000);

    // フックの処理を終え、入力をほかのアプリに渡さないことを示す戻り値
    private static readonly IntPtr SuppressInput = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private readonly Func<KeyboardInput, bool> inputHandler;

    // GC に回収されないよう、デリゲートをフィールドで保持する
    private readonly HookProc keyboardProc;
    private readonly HookProc mouseProc;
    private readonly IntPtr keyboardHook;
    private readonly IntPtr mouseHook;

    /// <param name="inputHandler">入力ごとに呼ばれる。true を返すと、その入力はほかのアプリに渡さない。</param>
    /// <exception cref="Win32Exception">フックを登録できなかった場合。</exception>
    public KeyboardHook(Func<KeyboardInput, bool> inputHandler)
    {
        this.inputHandler = inputHandler;
        keyboardProc = KeyboardProc;
        mouseProc = MouseProc;

        var module = GetModuleHandle(null);
        keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc, module, 0);
        if (keyboardHook == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        // マウスのフックは Ctrl＋クリックの判定に使うだけなので、登録できなくても動作は続ける
        mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc, module, 0);
    }

    public void Dispose()
    {
        UnhookWindowsHookEx(keyboardHook);
        if (mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(mouseHook);
        }
    }

    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && ToKeyboardInput(wParam.ToInt32(), lParam) is { } input && inputHandler(input))
        {
            return SuppressInput;
        }
        return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 &&
            wParam.ToInt32() is WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN or WM_XBUTTONDOWN
                or WM_MOUSEWHEEL or WM_MOUSEHWHEEL)
        {
            // マウスの入力は渡さないことがないので、戻り値は使わない
            inputHandler(KeyboardInput.Mouse);
        }
        return CallNextHookEx(mouseHook, nCode, wParam, lParam);
    }

    private static KeyboardInput? ToKeyboardInput(int message, IntPtr lParam)
    {
        var virtualKey = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam).vkCode;

        switch (message)
        {
            case WM_KEYDOWN or WM_SYSKEYDOWN:
                // フックはキーの状態が更新される前に呼ばれるので、
                // このキーがすでに押されていればリピートと判断できる
                var isRepeat = IsKeyDown(virtualKey);
                var otherModifiersHeld = VirtualKeys.Modifiers.Any(
                    modifier => modifier != virtualKey && IsKeyDown(modifier));
                return KeyboardInput.KeyDown(virtualKey, isRepeat, otherModifiersHeld);

            case WM_KEYUP or WM_SYSKEYUP:
                return KeyboardInput.KeyUp(virtualKey);

            default:
                return null;
        }
    }

    private static bool IsKeyDown(int virtualKey) => (GetAsyncKeyState(virtualKey) & KeyDownBit) != 0;
}
