using System.Runtime.InteropServices;

namespace PunctuationToggle;

/// <summary>
/// 前面のウィンドウから一瞬だけフォーカスを奪って戻す。
/// Microsoft IME は入力欄にフォーカスが戻ったときにレジストリの設定を読み直すので、
/// これで書き換えた句読点の設定が入力中のアプリにも反映される。
/// </summary>
internal sealed partial class FocusBouncer : IDisposable
{
    // フォーカスを戻すまでの時間。短すぎると IME が設定を読み直さないことがある。
    private const int ReturnDelayMilliseconds = 50;

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

    [LibraryImport("kernel32.dll")]
    private static partial uint GetCurrentThreadId();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

    /// <summary>画面外に置く、タスクバーにも Alt+Tab にも出ない 1px の透明なウィンドウ。</summary>
    private sealed class BounceWindow : Form
    {
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        public BounceWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Bounds = new Rectangle(-32000, -32000, 1, 1);
            Opacity = 0;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.ExStyle |= WS_EX_TOOLWINDOW;
                return createParams;
            }
        }
    }

    private readonly BounceWindow bounceWindow = new();
    private readonly System.Windows.Forms.Timer returnTimer = new() { Interval = ReturnDelayMilliseconds };
    private IntPtr windowToReturnTo;

    public FocusBouncer()
    {
        returnTimer.Tick += (_, _) => ReturnFocus();
    }

    /// <summary>前面のウィンドウからフォーカスを移し、少し後に戻す。</summary>
    /// <returns>
    /// フォーカスを移せたか。false の場合、書き換えた句読点の設定は、入力中のアプリでは次にフォーカスが移るまで反映されない。
    /// </returns>
    public bool Bounce()
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero || foregroundWindow == bounceWindow.Handle)
        {
            return false;
        }
        windowToReturnTo = foregroundWindow;

        // 前面のウィンドウのスレッドに入力を接続すると、SetForegroundWindow の制限を受けない。
        // 前面が自分のスレッドのウィンドウ（設定画面やダイアログ）なら接続は要らない（同じスレッドを指定すると失敗する）
        var foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
        var currentThreadId = GetCurrentThreadId();
        var isAttached = foregroundThreadId != currentThreadId
            && AttachThreadInput(currentThreadId, foregroundThreadId, fAttach: true);
        bounceWindow.Show();
        var isMoved = SetForegroundWindow(bounceWindow.Handle);
        if (isAttached)
        {
            AttachThreadInput(currentThreadId, foregroundThreadId, fAttach: false);
        }

        // フォーカスを移せなかった場合は、戻す必要も無い（前のウィンドウを前面にし直すと、かえってフォーカスが揺れる）
        if (!isMoved)
        {
            bounceWindow.Hide();
            return false;
        }
        returnTimer.Start();
        return true;
    }

    private void ReturnFocus()
    {
        returnTimer.Stop();
        // 戻すまでの間に元のウィンドウが閉じられた場合は、ほかのウィンドウのハンドルになっているおそれがあるので戻さない
        if (IsWindow(windowToReturnTo))
        {
            SetForegroundWindow(windowToReturnTo);
        }
        bounceWindow.Hide();
    }

    public void Dispose()
    {
        returnTimer.Dispose();
        bounceWindow.Dispose();
    }
}
