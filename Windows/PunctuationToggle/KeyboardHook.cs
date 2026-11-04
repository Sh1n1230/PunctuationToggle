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
internal sealed partial class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_XBUTTONDOWN = 0x020B;
    private const int WM_MOUSEHWHEEL = 0x020E;
    private const short KeyDownBit = unchecked((short)0x8000);

    // フックの処理を終え、入力をほかのアプリに渡さないことを示す戻り値
    private static readonly IntPtr SuppressInput = 1;

    // https://learn.microsoft.com/windows/win32/api/winuser/ns-winuser-kbdllhookstruct
    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public nuint dwExtraInfo;
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    // LibraryImport はデリゲートの引数を扱えない（SYSLIB1051）ため、関数ポインターを渡す
    [LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
    private static partial IntPtr SetWindowsHookEx(int idHook, IntPtr lpfn, IntPtr hMod, uint dwThreadId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnhookWindowsHookEx(IntPtr hhk);

    [LibraryImport("user32.dll")]
    private static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll")]
    private static partial short GetAsyncKeyState(int vKey);

    // LibraryImport は末尾の W を補わないため、Unicode 版の名前を書く
    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr GetModuleHandle(string? lpModuleName);

    private readonly Func<KeyboardInput, bool> inputHandler;

    // 関数ポインターだけを渡したデリゲートが GC に回収されないよう、フィールドで保持する
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
        keyboardHook = SetWindowsHookEx(
            WH_KEYBOARD_LL, Marshal.GetFunctionPointerForDelegate(keyboardProc), module, 0);
        if (keyboardHook == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        // マウスのフックは Ctrl＋クリックの判定に使うだけなので、登録できなくても動作は続ける
        mouseHook = SetWindowsHookEx(WH_MOUSE_LL, Marshal.GetFunctionPointerForDelegate(mouseProc), module, 0);
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
        var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
        // 仮想キーコードは 1〜254、flags は下位の数ビットだけを使うので、int に収まる
        return KeyboardHookInput.FromHookMessage(message, (int)hookStruct.vkCode, (int)hookStruct.flags, IsKeyDown);
    }

    private static bool IsKeyDown(int virtualKey) => (GetAsyncKeyState(virtualKey) & KeyDownBit) != 0;
}
