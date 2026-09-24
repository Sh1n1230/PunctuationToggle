using System.Runtime.InteropServices;

namespace PunctuationToggle;

/// <summary>
/// 低レベルキーボードフックで、右Ctrlが単独で押されて離されたことを検出する。
/// キー入力は監視するだけで、握りつぶしたり書き換えたりはしない。
/// </summary>
sealed class RightControlWatcher : IDisposable
{
    const int WH_KEYBOARD_LL = 13;
    const int WH_MOUSE_LL = 14;
    const int WM_KEYDOWN = 0x0100;
    const int WM_KEYUP = 0x0101;
    const int WM_SYSKEYDOWN = 0x0104;
    const int WM_SYSKEYUP = 0x0105;
    const int WM_LBUTTONDOWN = 0x0201;
    const int WM_RBUTTONDOWN = 0x0204;
    const int WM_MBUTTONDOWN = 0x0207;
    const int WM_MOUSEWHEEL = 0x020A;
    const int VK_RCONTROL = 0xA3;

    [StructLayout(LayoutKind.Sequential)]
    struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetModuleHandle(string? lpModuleName);

    // GC に回収されないよう、デリゲートをフィールドで保持する
    readonly HookProc keyboardProc;
    readonly HookProc mouseProc;
    readonly IntPtr keyboardHook;
    readonly IntPtr mouseHook;

    // 右Ctrlを押してから離すまでの間に、他のキーやマウスボタンが押されたか
    bool rightControlIsDown;
    bool rightControlUsedWithOtherInput;

    /// 右Ctrlが単独で押されて離されたときに呼ばれる（フックを呼び出したスレッド上）
    public event Action? Tapped;

    public RightControlWatcher()
    {
        keyboardProc = KeyboardProc;
        mouseProc = MouseProc;
        var module = GetModuleHandle(null);
        keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc, module, 0);
        mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc, module, 0);
        if (keyboardHook == IntPtr.Zero)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var info = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var message = (int)wParam;
            var isDown = message is WM_KEYDOWN or WM_SYSKEYDOWN;
            var isUp = message is WM_KEYUP or WM_SYSKEYUP;

            if (info.vkCode == VK_RCONTROL)
            {
                if (isDown && !rightControlIsDown)
                {
                    rightControlIsDown = true;
                    rightControlUsedWithOtherInput = false;
                }
                else if (isUp && rightControlIsDown)
                {
                    rightControlIsDown = false;
                    if (!rightControlUsedWithOtherInput)
                    {
                        Tapped?.Invoke();
                    }
                }
            }
            else if (isDown && rightControlIsDown)
            {
                rightControlUsedWithOtherInput = true;
            }
        }
        return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
    }

    IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && rightControlIsDown &&
            (int)wParam is WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN or WM_MOUSEWHEEL)
        {
            // Ctrl＋クリック、Ctrl＋ホイールなど
            rightControlUsedWithOtherInput = true;
        }
        return CallNextHookEx(mouseHook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        UnhookWindowsHookEx(keyboardHook);
        if (mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(mouseHook);
        }
    }
}
