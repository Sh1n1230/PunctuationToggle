using System.Runtime.InteropServices;

namespace PunctuationToggle;

/// <summary>
/// 前面のウィンドウから一瞬だけフォーカスを奪って戻す。
/// Microsoft IME は入力欄にフォーカスが戻ったときにレジストリの設定を読み直すので、
/// これで書き換えた句読点の設定が入力中のアプリにも反映される。
/// </summary>
sealed class FocusBouncer : IDisposable
{
    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    // 画面外に置く、タスクバーにも Alt+Tab にも出ない 1px のウィンドウ
    sealed class BounceWindow : Form
    {
        const int WS_EX_TOOLWINDOW = 0x00000080;

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
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW;
                return cp;
            }
        }
    }

    readonly BounceWindow window = new();
    readonly System.Windows.Forms.Timer returnTimer = new() { Interval = 50 };
    IntPtr returnTo;

    public FocusBouncer()
    {
        returnTimer.Tick += (_, _) => Return();
    }

    public void Bounce()
    {
        var target = GetForegroundWindow();
        if (target == IntPtr.Zero || target == window.Handle)
        {
            return;
        }
        returnTo = target;

        // フォアグラウンドのスレッドに入力を接続すると、SetForegroundWindow の制限を受けない
        var targetThread = GetWindowThreadProcessId(target, IntPtr.Zero);
        var currentThread = GetCurrentThreadId();
        AttachThreadInput(currentThread, targetThread, true);
        window.Show();
        SetForegroundWindow(window.Handle);
        AttachThreadInput(currentThread, targetThread, false);

        returnTimer.Start();
    }

    void Return()
    {
        returnTimer.Stop();
        SetForegroundWindow(returnTo);
        window.Hide();
    }

    public void Dispose()
    {
        returnTimer.Dispose();
        window.Dispose();
    }
}
