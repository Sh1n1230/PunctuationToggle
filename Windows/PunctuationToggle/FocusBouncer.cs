using System.Runtime.InteropServices;

namespace PunctuationToggle;

/// <summary>
/// 前面のウィンドウから一瞬だけフォーカスを奪って戻す。
/// Microsoft IME は入力欄にフォーカスが戻ったときにレジストリの設定を読み直すので、
/// これで書き換えた句読点の設定が入力中のアプリにも反映される。
/// </summary>
internal sealed class FocusBouncer : IDisposable
{
    // フォーカスを戻すまでの時間。短すぎると IME が設定を読み直さないことがある。
    private const int ReturnDelayMilliseconds = 50;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

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

    public void Bounce()
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero || foregroundWindow == bounceWindow.Handle)
        {
            return;
        }
        windowToReturnTo = foregroundWindow;

        // 前面のウィンドウのスレッドに入力を接続すると、SetForegroundWindow の制限を受けない
        var foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
        var currentThreadId = GetCurrentThreadId();
        AttachThreadInput(currentThreadId, foregroundThreadId, fAttach: true);
        bounceWindow.Show();
        SetForegroundWindow(bounceWindow.Handle);
        AttachThreadInput(currentThreadId, foregroundThreadId, fAttach: false);

        returnTimer.Start();
    }

    private void ReturnFocus()
    {
        returnTimer.Stop();
        SetForegroundWindow(windowToReturnTo);
        bounceWindow.Hide();
    }

    public void Dispose()
    {
        returnTimer.Dispose();
        bounceWindow.Dispose();
    }
}
