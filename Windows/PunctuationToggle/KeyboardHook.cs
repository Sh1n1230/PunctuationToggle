using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PunctuationToggle;

/// <summary>
/// 低レベルキーボード・マウスフックで、すべてのアプリへの入力を監視する。
/// </summary>
/// <remarks>
/// フックは、メッセージループだけを回す専用のスレッドで登録し、そのスレッドで呼ばれる。
/// 処理が LowLevelHooksTimeout を超えると Windows にフックを黙って外されるため、
/// UI の処理（アイコンの描画、レジストリの読み書き、ダイアログなど）と同じスレッドでは呼ばれないようにしている。
/// ハンドラーはこの専用のスレッドで呼ばれるので、重い処理や UI の操作は UI スレッドに Post すること。
/// https://learn.microsoft.com/windows/win32/winmsg/lowlevelkeyboardproc
/// </remarks>
internal sealed partial class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_QUIT = 0x0012;
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

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX;
        public int ptY;
        public uint lPrivate;
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

    [LibraryImport("user32.dll", EntryPoint = "GetMessageW")]
    private static partial int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [LibraryImport("user32.dll", EntryPoint = "PostThreadMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("kernel32.dll")]
    private static partial uint GetCurrentThreadId();

    // LibraryImport は末尾の W を補わないため、Unicode 版の名前を書く
    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr GetModuleHandle(string? lpModuleName);

    private readonly Func<KeyboardInput, bool> inputHandler;

    // 関数ポインターだけを渡したデリゲートが GC に回収されないよう、フィールドで保持する
    private readonly HookProc keyboardProc;
    private readonly HookProc mouseProc;
    private readonly Thread hookThread;

    // 以下は専用のスレッドだけが読み書きする（登録の結果は、コンストラクターが started を待ってから読む）
    private uint hookThreadId;
    private IntPtr keyboardHook;
    private IntPtr mouseHook;
    private int keyboardHookError;

    /// <param name="inputHandler">
    /// 入力ごとに、フックの専用のスレッドで呼ばれる。true を返すと、その入力はほかのアプリに渡さない。
    /// </param>
    /// <exception cref="Win32Exception">キーボードのフックを登録できなかった場合。</exception>
    public KeyboardHook(Func<KeyboardInput, bool> inputHandler)
    {
        this.inputHandler = inputHandler;
        keyboardProc = KeyboardProc;
        mouseProc = MouseProc;

        using var started = new ManualResetEventSlim();
        hookThread = new Thread(() => Run(started))
        {
            Name = "PunctuationToggle キーボードフック",
            // 終了処理の途中で止まっても、プロセスの終了を妨げない
            IsBackground = true,
        };
        hookThread.Start();
        started.Wait();

        if (keyboardHook == IntPtr.Zero)
        {
            hookThread.Join();
            throw new Win32Exception(keyboardHookError);
        }
    }

    public void Dispose()
    {
        // メッセージループを終わらせると、専用のスレッドがフックを外して終了する
        if (hookThread.IsAlive && PostThreadMessage(hookThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero))
        {
            hookThread.Join();
        }
    }

    /// <summary>専用のスレッドの本体。フックを登録し、メッセージループを回し、終わったらフックを外す。</summary>
    private void Run(ManualResetEventSlim started)
    {
        hookThreadId = GetCurrentThreadId();
        var module = GetModuleHandle(null);
        keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, Marshal.GetFunctionPointerForDelegate(keyboardProc), module, 0);
        if (keyboardHook == IntPtr.Zero)
        {
            keyboardHookError = Marshal.GetLastWin32Error();
            started.Set();
            return;
        }
        // マウスのフックは Ctrl＋クリックの判定に使うだけなので、登録できなくても動作は続ける
        mouseHook = SetWindowsHookEx(WH_MOUSE_LL, Marshal.GetFunctionPointerForDelegate(mouseProc), module, 0);
        started.Set();

        // 低レベルフックは、登録したスレッドがメッセージを待っている間に呼ばれる。
        // GetMessage は WM_QUIT で 0、エラーで -1 を返す
        while (GetMessage(out _, IntPtr.Zero, 0, 0) > 0)
        {
        }

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
