using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PunctuationToggle;

/// <summary>フックで受け取った入力の扱い。</summary>
internal enum HookAction
{
    /// <summary>ほかのアプリに渡す。</summary>
    PassThrough,

    /// <summary>ほかのアプリに渡さない。</summary>
    Suppress,

    /// <summary>
    /// 離した入力をいったん渡さず、メニューを抑えるキーの押し下げと離しを送ってから、離した入力を送り直す。
    /// Windows キー・Alt キーの単独押しで、スタートメニューやメニューバーが開かないようにする。
    /// </summary>
    ReplayKeyUpWithMenuMask,
}

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

    // フックを登録し直すよう専用のスレッドに頼むメッセージ（アプリが自由に使える WM_APP 以降の値）
    private const uint ReinstallMessage = 0x8000 + 1; // WM_APP + 1
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_XBUTTONDOWN = 0x020B;
    private const int WM_MOUSEHWHEEL = 0x020E;
    private const short KeyDownBit = unchecked((short)0x8000);

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint LLKHF_EXTENDED = 0x01;

    // メニューを抑えるために送るキー。WinUser.h で割り当てが無い（unassigned）とされている 0xE8 を使う
    // （AutoHotkey の A_MenuMaskKey の既定値と同じ）。将来 Windows が割り当てた場合は、ほかの未割り当ての値に変える
    private const ushort MenuMaskVirtualKey = 0xE8;

    // このアプリが送った入力の dwExtraInfo に付ける印（"PTMK"）。フックで見分け、判定に使わずにそのまま渡す
    private const nuint SelfInjectedMarker = 0x50544D4B;

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

    // https://learn.microsoft.com/windows/win32/api/winuser/ns-winuser-input
    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion union;
    }

    // 共用体の大きさを MOUSEINPUT（いちばん大きいメンバー）に合わせるため、使わないメンバーも宣言する
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mouse;

        [FieldOffset(0)]
        public KEYBDINPUT keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
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

    [LibraryImport("user32.dll")]
    private static partial uint SendInput(uint cInputs, [In] INPUT[] pInputs, int cbSize);

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

    private readonly Func<KeyboardInput, HookAction> inputHandler;
    private readonly Action<Exception> handlerFailed;

    // 関数ポインターだけを渡したデリゲートが GC に回収されないよう、フィールドで保持する
    private readonly HookProc keyboardProc;
    private readonly HookProc mouseProc;
    private readonly Thread hookThread;

    // 以下は専用のスレッドだけが読み書きする（登録の結果は、コンストラクターが started を待ってから読む）
    private uint hookThreadId;
    private IntPtr keyboardHook;
    private IntPtr mouseHook;
    private int keyboardHookError;
    private int mouseHookError;

    /// <param name="inputHandler">入力ごとに、フックの専用のスレッドで呼ばれ、その入力の扱いを返す。</param>
    /// <param name="handlerFailed">
    /// <paramref name="inputHandler"/> が例外を投げたときに、フックの専用のスレッドで呼ばれる（その入力はほかのアプリに渡す）。
    /// </param>
    /// <exception cref="Win32Exception">キーボードのフックを登録できなかった場合。</exception>
    public KeyboardHook(Func<KeyboardInput, HookAction> inputHandler, Action<Exception> handlerFailed)
    {
        this.inputHandler = inputHandler;
        this.handlerFailed = handlerFailed;
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

    /// <summary>
    /// マウスのフックを登録できなかった場合の Win32 のエラーコード。登録できた場合は null。
    /// マウスのフックが無いと、修飾キーを押したままのクリックを単独押しと区別できない。
    /// </summary>
    public int? MouseHookErrorCode => mouseHookError == 0 ? null : mouseHookError;

    /// <summary>
    /// フックを外して登録し直す。処理が LowLevelHooksTimeout を超えて Windows にフックを外された場合
    /// （外されたことはアプリに知らされない）に、切り替えキーを使えるように戻す。
    /// </summary>
    public void Reinstall() =>
        PostThreadMessage(hookThreadId, ReinstallMessage, IntPtr.Zero, IntPtr.Zero);

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
        Install(module);
        started.Set();
        if (keyboardHook == IntPtr.Zero)
        {
            return;
        }

        // 低レベルフックは、登録したスレッドがメッセージを待っている間に呼ばれる。
        // GetMessage は WM_QUIT で 0、エラーで -1 を返す
        while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
        {
            if (message.message == ReinstallMessage)
            {
                Uninstall();
                Install(module);
            }
        }
        Uninstall();
    }

    private void Install(IntPtr module)
    {
        keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, Marshal.GetFunctionPointerForDelegate(keyboardProc), module, 0);
        keyboardHookError = keyboardHook == IntPtr.Zero ? Marshal.GetLastWin32Error() : 0;
        // マウスのフックは Ctrl＋クリックの判定に使うだけなので、登録できなくても動作は続ける
        mouseHook = SetWindowsHookEx(WH_MOUSE_LL, Marshal.GetFunctionPointerForDelegate(mouseProc), module, 0);
        mouseHookError = mouseHook == IntPtr.Zero ? Marshal.GetLastWin32Error() : 0;
    }

    private void Uninstall()
    {
        if (keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(keyboardHook);
            keyboardHook = IntPtr.Zero;
        }
        if (mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(mouseHook);
            mouseHook = IntPtr.Zero;
        }
    }

    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            // 自分で送り直した入力は、判定に使わずにそのまま渡す
            if (hookStruct.dwExtraInfo != SelfInjectedMarker && ToKeyboardInput(wParam.ToInt32(), hookStruct) is { } input)
            {
                switch (CallHandler(input))
                {
                    case HookAction.Suppress:
                        return SuppressInput;
                    // 送れなかった場合は、離した入力をそのまま渡す（キーが押されたままにならないように）
                    case HookAction.ReplayKeyUpWithMenuMask when SendMenuMaskAndKeyUp(hookStruct):
                        return SuppressInput;
                    case HookAction.PassThrough:
                    case HookAction.ReplayKeyUpWithMenuMask:
                    default:
                        break;
                }
            }
        }
        return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
    }

    /// <summary>
    /// メニューを抑えるキーの押し下げと離し、渡さなかった離した入力の順に送る。
    /// Windows キー・Alt キーを押してから離すまでの間にほかのキーが押されたことになり、スタートメニューやメニューバーが開かない。
    /// </summary>
    /// <returns>3つの入力をすべて送れたか。</returns>
    private static bool SendMenuMaskAndKeyUp(KBDLLHOOKSTRUCT keyUp)
    {
        INPUT[] inputs =
        [
            KeyboardEvent(MenuMaskVirtualKey, scanCode: 0, flags: 0),
            KeyboardEvent(MenuMaskVirtualKey, scanCode: 0, flags: KEYEVENTF_KEYUP),
            KeyboardEvent(
                (ushort)keyUp.vkCode,
                (ushort)keyUp.scanCode,
                KEYEVENTF_KEYUP | ((keyUp.flags & LLKHF_EXTENDED) != 0 ? KEYEVENTF_EXTENDEDKEY : 0)),
        ];
        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>()) == inputs.Length;
    }

    private static INPUT KeyboardEvent(ushort virtualKey, ushort scanCode, uint flags) => new()
    {
        type = INPUT_KEYBOARD,
        union = new InputUnion
        {
            keyboard = new KEYBDINPUT { wVk = virtualKey, wScan = scanCode, dwFlags = flags, dwExtraInfo = SelfInjectedMarker },
        },
    };

    private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 &&
            wParam.ToInt32() is WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN or WM_XBUTTONDOWN
                or WM_MOUSEWHEEL or WM_MOUSEHWHEEL)
        {
            // マウスの入力は渡さないことがないので、戻り値は使わない
            _ = CallHandler(KeyboardInput.Mouse);
        }
        return CallNextHookEx(mouseHook, nCode, wParam, lParam);
    }

    /// <summary>ハンドラーを呼ぶ。例外が起きた場合は、その入力をほかのアプリに渡す。</summary>
    private HookAction CallHandler(KeyboardInput input)
    {
        try
        {
            return inputHandler(input);
        }
        // フックのコールバックは user32 から直接呼ばれ、例外がここから外へ出ると CallNextHookEx を呼ばないまま
        // プロセスが終了する（その入力もほかのアプリに渡らない）。キー入力を止めないことを優先して、
        // 種類を問わず捕まえて入力を渡し、扱い（知らせて終了するなど）は呼び出し元に任せる
#pragma warning disable CA1031 // 上の理由で、すべての例外を捕まえる
        catch (Exception exception)
#pragma warning restore CA1031
        {
            handlerFailed(exception);
            return HookAction.PassThrough;
        }
    }

    private static KeyboardInput? ToKeyboardInput(int message, KBDLLHOOKSTRUCT hookStruct)
    {
        // 仮想キーコードは 1〜254、flags は下位の数ビットだけを使うので、int に収まる
        return KeyboardHookInput.FromHookMessage(message, (int)hookStruct.vkCode, (int)hookStruct.flags, IsKeyDown);
    }

    private static bool IsKeyDown(int virtualKey) => (GetAsyncKeyState(virtualKey) & KeyDownBit) != 0;
}
