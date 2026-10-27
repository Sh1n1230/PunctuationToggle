using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;

namespace PunctuationToggle;

/// <summary>
/// タスクトレイに現在の句読点を表示し、切り替えキーで切り替える。
/// </summary>
internal sealed class TrayApplication : ApplicationContext
{
    private const string RepositoryUrl = "https://github.com/Sh1n1230/PunctuationToggle";

    // 最新のリリースのページ。アプリ自身は通信せず、利用者がメニューを選んだときだけブラウザーで開く
    private const string LatestReleaseUrl = RepositoryUrl + "/releases/latest";

    // IME の設定画面側で変更された場合も表示を追従させるための、設定を読み直す間隔
    private const int PollIntervalMilliseconds = 2000;

    private readonly NotifyIcon notifyIcon = new();
    private readonly ToolStripMenuItem toggleMenuItem = new();
    private readonly ToolStripMenuItem usageMenuItem = new() { Enabled = false };
    private readonly ToolStripMenuItem launchAtLoginMenuItem = new("ログイン時に起動(&L)");
    private readonly TriggerKeyDetector detector = new(AppSettings.TriggerKey);
    private readonly FocusBouncer focusBouncer = new();
    private readonly System.Windows.Forms.Timer pollTimer = new() { Interval = PollIntervalMilliseconds };
    private readonly SynchronizationContext uiContext;
    private readonly KeyboardHook keyboardHook;

    private TriggerKeyRecorderForm? triggerKeyRecorder;
    private Icon? currentIcon;
    private PunctuationStyle? displayedStyle;
    private int displayedIconSize;

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

    // ---- 入力の処理 ----

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
        try
        {
            MicrosoftImeSettings.PunctuationStyle = MicrosoftImeSettings.PunctuationStyle.Toggled();
        }
        // レジストリのアクセス権が変えられている場合なども、アプリを終了させずに知らせる
        catch (Exception exception) when (exception is InvalidDataException || RegistryErrors.IsAccessFailure(exception))
        {
            MessageBox.Show(
                $"句読点の設定を変更できませんでした。{Environment.NewLine}{exception.Message}",
                "PunctuationToggle",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }
        UpdateDisplay();

        if (bounceFocus)
        {
            focusBouncer.Bounce();
        }
    }

    // ---- メニューとアイコン ----

    private ContextMenuStrip CreateMenu()
    {
        toggleMenuItem.Click += (_, _) => TogglePunctuationStyle(bounceFocus: false);
        launchAtLoginMenuItem.Click += (_, _) => ToggleLaunchAtLogin();

        // キーボードだけでも選べるよう、日本語の Windows アプリの慣習どおり項目名の後ろに (&X) の形でアクセスキーを付ける。
        // 操作の説明（usageMenuItem）は選べない項目なので付けない
        var menu = new ContextMenuStrip();
        menu.Items.Add(toggleMenuItem);
        menu.Items.Add(usageMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("切り替えキーを変更(&K)…", null, (_, _) => ShowTriggerKeyRecorder());
        menu.Items.Add(launchAtLoginMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("最新版を確認(&U)…", null, (_, _) => OpenLatestRelease());
        menu.Items.Add("バージョン情報(&A)", null, (_, _) => ShowAbout());
        menu.Items.Add("終了(&X)", null, (_, _) => ExitThread());
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
            ? $"「、。」に切り替え(&T)（{triggerKey.DisplayName}）"
            : $"「，．」に切り替え(&T)（{triggerKey.DisplayName}）";
        usageMenuItem.Text = triggerKey.UsageDescription;
        notifyIcon.Text = $"PunctuationToggle: {style.Symbols()}";

        // 表示倍率の変更は、トレイ用のウィンドウが無く WM_DPICHANGED を受け取れないため、設定を読み直すたびに確かめる
        var iconSize = TrayIconRenderer.CurrentIconSize;
        if (style == displayedStyle && iconSize == displayedIconSize)
        {
            return;
        }
        displayedStyle = style;
        displayedIconSize = iconSize;

        var previousIcon = currentIcon;
        currentIcon = TrayIconRenderer.Render(style.Symbols(), AppSettings.IsTaskbarLight, iconSize);
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
                // 保存できなかったキーを今回の起動中だけ使うと、再起動で元に戻って分かりにくいため、変更しない。
                try
                {
                    AppSettings.TriggerKey = newKey;
                    detector.TriggerKey = newKey;
                }
                catch (Exception exception) when (RegistryErrors.IsAccessFailure(exception))
                {
                    MessageBox.Show(
                        $"切り替えキーを保存できませんでした。{Environment.NewLine}{exception.Message}",
                        "PunctuationToggle",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
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
        catch (Exception exception) when (RegistryErrors.IsAccessFailure(exception))
        {
            MessageBox.Show(
                $"「ログイン時に起動」を変更できませんでした。{Environment.NewLine}{exception.Message}",
                "PunctuationToggle",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private static void OpenLatestRelease()
    {
        try
        {
            Process.Start(new ProcessStartInfo(LatestReleaseUrl) { UseShellExecute = true });
        }
        // 既定のブラウザーが設定されていない場合など
        catch (Win32Exception exception)
        {
            MessageBox.Show(
                $"ブラウザーを開けませんでした。次のページで最新版を確認してください。{Environment.NewLine}{LatestReleaseUrl}{Environment.NewLine}{exception.Message}",
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
