using Microsoft.Win32;

namespace PunctuationToggle;

/// <summary>
/// タスクトレイに現在の句読点を表示し、切り替えキーで切り替える。
/// </summary>
internal sealed class TrayApplication : ApplicationContext
{
    private const string RepositoryUrl = "https://github.com/Sh1n1230/PunctuationToggle";

    // IME の設定画面側で変更された場合も表示を追従させるための、設定を読み直す間隔
    private const int PollIntervalMilliseconds = 2000;

    private readonly NotifyIcon notifyIcon = new();
    private readonly ToolStripMenuItem toggleMenuItem = new();
    private readonly ToolStripMenuItem usageMenuItem = new() { Enabled = false };
    private readonly ToolStripMenuItem launchAtLoginMenuItem = new("ログイン時に起動");
    private readonly TriggerKeyDetector detector = new(AppSettings.TriggerKey);
    private readonly FocusBouncer focusBouncer = new();
    private readonly System.Windows.Forms.Timer pollTimer = new() { Interval = PollIntervalMilliseconds };
    private readonly SynchronizationContext uiContext;
    private readonly KeyboardHook keyboardHook;

    private TriggerKeyRecorderForm? triggerKeyRecorder;
    private Icon? currentIcon;
    private PunctuationStyle? displayedStyle;

    public TrayApplication()
    {
        // FocusBouncer のフォームを作った時点で、UI スレッドの SynchronizationContext が設定されている
        uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        // 登録できなかった場合はここで例外になる（トレイアイコンを表示する前に確かめる）
        keyboardHook = new KeyboardHook(HandleInput);

        notifyIcon.ContextMenuStrip = CreateMenu();
        notifyIcon.MouseClick += (_, eventArgs) =>
        {
            if (eventArgs.Button == MouseButtons.Left)
            {
                TogglePunctuationStyle(bounceFocus: false);
            }
        };
        notifyIcon.Visible = true;
        UpdateDisplay();

        pollTimer.Tick += (_, _) => UpdateDisplay();
        pollTimer.Start();

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            pollTimer.Dispose();
            keyboardHook.Dispose();
            focusBouncer.Dispose();
            triggerKeyRecorder?.Dispose();
            notifyIcon.Visible = false;
            notifyIcon.ContextMenuStrip?.Dispose();
            notifyIcon.Dispose();
            currentIcon?.Dispose();
        }
        base.Dispose(disposing);
    }

    // MARK: 入力の処理

    private bool HandleInput(KeyboardInput input)
    {
        if (triggerKeyRecorder is { } recorder)
        {
            return recorder.HandleInput(input);
        }

        var decision = detector.Handle(input);
        if (decision.ShouldToggle)
        {
            // フックの処理が遅いと Windows にフックを外されるので、メッセージループに戻ってから切り替える
            uiContext.Post(_ => TogglePunctuationStyle(bounceFocus: true), null);
        }
        return decision.ShouldSuppress;
    }

    /// <param name="bounceFocus">
    /// 入力中のアプリに反映させるため、フォーカスを一瞬移すか。
    /// タスクトレイやメニューから切り替えた場合は、すでにフォーカスが入力欄から外れているので不要。
    /// </param>
    private void TogglePunctuationStyle(bool bounceFocus)
    {
        MicrosoftImeSettings.PunctuationStyle = MicrosoftImeSettings.PunctuationStyle.Toggled();
        UpdateDisplay();

        if (bounceFocus)
        {
            focusBouncer.Bounce();
        }
    }

    // MARK: メニューとアイコン

    private ContextMenuStrip CreateMenu()
    {
        toggleMenuItem.Click += (_, _) => TogglePunctuationStyle(bounceFocus: false);
        launchAtLoginMenuItem.Click += (_, _) => ToggleLaunchAtLogin();

        var menu = new ContextMenuStrip();
        menu.Items.Add(toggleMenuItem);
        menu.Items.Add(usageMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("切り替えキーを変更...", null, (_, _) => ShowTriggerKeyRecorder());
        menu.Items.Add(launchAtLoginMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("バージョン情報", null, (_, _) => ShowAbout());
        menu.Items.Add("終了", null, (_, _) => ExitThread());
        menu.Opening += (_, _) =>
        {
            launchAtLoginMenuItem.Checked = AppSettings.LaunchAtLogin;
            UpdateDisplay();
        };
        return menu;
    }

    private void UpdateDisplay()
    {
        var style = MicrosoftImeSettings.PunctuationStyle;
        var triggerKey = detector.TriggerKey;

        toggleMenuItem.Text = style == PunctuationStyle.CommaPeriod
            ? $"「、。」に切り替え（{triggerKey.DisplayName}）"
            : $"「，．」に切り替え（{triggerKey.DisplayName}）";
        usageMenuItem.Text = triggerKey.UsageDescription;
        notifyIcon.Text = $"PunctuationToggle: {style.Symbols()}";

        if (style == displayedStyle)
        {
            return;
        }
        displayedStyle = style;

        var previousIcon = currentIcon;
        currentIcon = TrayIconRenderer.Render(style.Symbols(), AppSettings.IsTaskbarLight);
        notifyIcon.Icon = currentIcon;
        previousIcon?.Dispose();
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs eventArgs)
    {
        // タスクバーのライト・ダークが切り替わったら、文字の色を合わせて描き直す
        if (eventArgs.Category == UserPreferenceCategory.General)
        {
            displayedStyle = null;
            UpdateDisplay();
        }
    }

    private void ShowTriggerKeyRecorder()
    {
        if (triggerKeyRecorder is { } existingRecorder)
        {
            existingRecorder.Activate();
            return;
        }

        var recorder = new TriggerKeyRecorderForm(detector.TriggerKey);
        recorder.Finished += newKey =>
        {
            triggerKeyRecorder = null;
            if (newKey is not null)
            {
                AppSettings.TriggerKey = newKey;
                detector.TriggerKey = newKey;
            }
            UpdateDisplay();
        };
        triggerKeyRecorder = recorder;
        recorder.Show();
        recorder.Activate();
    }

    private static void ToggleLaunchAtLogin()
    {
        try
        {
            AppSettings.LaunchAtLogin = !AppSettings.LaunchAtLogin;
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            MessageBox.Show(
                $"「ログイン時に起動」を変更できませんでした。{Environment.NewLine}{exception.Message}",
                "PunctuationToggle",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private static void ShowAbout()
    {
        MessageBox.Show(
            $"PunctuationToggle {Application.ProductVersion.Split('+')[0]}{Environment.NewLine}{RepositoryUrl}",
            "バージョン情報",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
