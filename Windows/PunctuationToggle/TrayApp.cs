using System.Drawing.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PunctuationToggle;

/// <summary>
/// タスクトレイに現在の句読点を表示し、右Ctrlの単独押しで切り替える。
/// </summary>
sealed class TrayApp : ApplicationContext
{
    const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValueName = "PunctuationToggle";

    readonly NotifyIcon notifyIcon = new();
    readonly ToolStripMenuItem toggleMenuItem = new();
    readonly ToolStripMenuItem loginMenuItem = new("ログイン時に起動");
    readonly RightControlWatcher watcher = new();
    readonly FocusBouncer focusBouncer = new();
    // IME の設定画面側で変更された場合も表示を追従させる
    readonly System.Windows.Forms.Timer pollTimer = new() { Interval = 2000 };
    readonly SynchronizationContext uiContext;
    Icon? currentIcon;
    bool? shownFullWidthMode;

    static bool FullWidthMode =>
        MicrosoftImePunctuation.Current == MicrosoftImePunctuation.FullWidthStyle;

    public TrayApp()
    {
        uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        toggleMenuItem.Click += (_, _) => Toggle(fromMenu: true);
        loginMenuItem.Click += (_, _) => SetLaunchAtLogin(!loginMenuItem.Checked);
        var menu = new ContextMenuStrip();
        menu.Items.Add(toggleMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(loginMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("終了", null, (_, _) => ExitThread());
        menu.Opening += (_, _) => loginMenuItem.Checked = LaunchAtLogin;

        notifyIcon.ContextMenuStrip = menu;
        notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                Toggle(fromMenu: true);
            }
        };
        notifyIcon.Visible = true;
        UpdateIcon();

        // フックの中では重い処理をせず、メッセージループに戻ってから切り替える
        // （フックの処理が遅いと Windows にフックを外される）
        watcher.Tapped += () => uiContext.Post(_ => Toggle(fromMenu: false), null);

        pollTimer.Tick += (_, _) => UpdateIcon();
        pollTimer.Start();
    }

    void Toggle(bool fromMenu)
    {
        MicrosoftImePunctuation.Set(FullWidthMode
            ? MicrosoftImePunctuation.JapaneseStyle
            : MicrosoftImePunctuation.FullWidthStyle);
        UpdateIcon();

        // トレイのクリックやメニューの場合は、そのときにフォーカスが入力欄から外れているので不要
        if (!fromMenu)
        {
            focusBouncer.Bounce();
        }
    }

    void UpdateIcon()
    {
        var fullWidth = FullWidthMode;
        if (fullWidth == shownFullWidthMode)
        {
            return;
        }
        shownFullWidthMode = fullWidth;

        var label = fullWidth ? "，．" : "、。";
        var oldIcon = currentIcon;
        currentIcon = CreateTextIcon(label);
        notifyIcon.Icon = currentIcon;
        notifyIcon.Text = $"PunctuationToggle: {label}";
        toggleMenuItem.Text = fullWidth ? "「、。」に切り替え（右Ctrl）" : "「，．」に切り替え（右Ctrl）";
        oldIcon?.Dispose();
    }

    // 句読点の2文字をそのままアイコンにする
    static Icon CreateTextIcon(string label)
    {
        const int size = 32;
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);
            using var font = new Font("Yu Gothic UI", 20, FontStyle.Bold, GraphicsUnit.Pixel);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            var color = IsTaskbarLight ? Color.Black : Color.White;
            using var brush = new SolidBrush(color);
            // 全角の句読点は字面の左下に寄っているので、少し右上にずらして中央に見せる
            g.DrawString(label, font, brush, new RectangleF(6, -4, size, size), format);
        }
        var handle = bitmap.GetHicon();
        using var temporary = Icon.FromHandle(handle);
        var icon = (Icon)temporary.Clone();
        DestroyIcon(handle);
        return icon;
    }

    static bool IsTaskbarLight
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("SystemUsesLightTheme") is int value && value != 0;
        }
    }

    static bool LaunchAtLogin
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(RunValueName) is string;
        }
    }

    static void SetLaunchAtLogin(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
        {
            key.SetValue(RunValueName, $"\"{Application.ExecutablePath}\"");
        }
        else
        {
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr hIcon);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            pollTimer.Dispose();
            watcher.Dispose();
            focusBouncer.Dispose();
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            currentIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
