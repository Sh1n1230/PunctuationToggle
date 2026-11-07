using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;

namespace PunctuationToggle;

/// <summary>
/// タスクトレイに現在の句読点を表示し、切り替えキーで切り替える。
/// </summary>
internal sealed class TrayApplication : ApplicationContext
{
    internal const string RepositoryUrl = "https://github.com/Sh1n1230/PunctuationToggle";

    // 最新のリリースのページ。アプリ自身は通信せず、利用者がメニューを選んだときだけブラウザーで開く
    private const string LatestReleaseUrl = RepositoryUrl + "/releases/latest";

    // IME の設定画面側で変更された場合も表示を追従させるため、設定を読み直す間隔。
    // 設定の変更を監視できている間（MicrosoftImeSettings.WatchChanges）は、監視の取りこぼしと表示倍率の変更に
    // 備えて読み直すだけなので、何もしない間にプロセスが起きる回数を減らすよう長くする
    private const int PollIntervalWithoutWatcherMilliseconds = 2000;
    private const int PollIntervalWithWatcherMilliseconds = 10000;

    // 起動したときから句読点の設定を読み込めず、今の句読点が分からないときにアイコンに描く文字
    private const string UnknownStyleSymbol = "？";

    // 通知の表示時間。Windows 10 以降は OS の設定が優先され、この値は使われないが、引数として必要
    private const int BalloonTipTimeoutMilliseconds = 10000;

    private readonly NotifyIcon notifyIcon = new();
    private readonly ToolStripMenuItem toggleMenuItem = new();
    private readonly ToolStripMenuItem usageMenuItem = new() { Enabled = false };
    private readonly ToolStripMenuItem launchAtLoginMenuItem = new("ログイン時に起動(&L)");
    // 判定はフックの専用のスレッドで、切り替えキーの変更は UI スレッドで行うので、detectorLock の中で触る
    private readonly TriggerKeyDetector detector = new(AppSettings.TriggerKey);
    private readonly Lock detectorLock = new();
    private readonly FocusBouncer focusBouncer = new();
    private readonly System.Windows.Forms.Timer pollTimer = new() { Interval = PollIntervalWithoutWatcherMilliseconds };
    private readonly SynchronizationContext uiContext;
    private readonly KeyboardHook keyboardHook;

    private RegistryChangeWatcher? imeSettingsWatcher;

    // Dispose を始めたか。スレッドプールからの通知が、破棄した後のトレイアイコンなどに触らないようにする
    private volatile bool isDisposed;
    private bool isImeSettingsUnreadable;

    // 句読点の切り替え（書き込み）に失敗し続けているか。失敗の知らせを、続いている間は1回だけにする
    private bool isToggleFailing;
    // UI スレッドで設定し、フックの専用のスレッドで読む
    private volatile TriggerKeyRecorderForm? triggerKeyRecorder;
    private Icon? currentIcon;
    // 最後に読み込めた句読点（読み込めない間は、この句読点のまま表示を続ける）
    private PunctuationStyle? lastReadStyle;

    // アイコンに描いている文字（描き直すかを決めるため）。null にすると次の UpdateDisplay で描き直す
    private string? displayedSymbols;
    private int displayedIconSize;

    public TrayApplication()
    {
        // FocusBouncer のフォームを作った時点で、UI スレッドの SynchronizationContext が設定されている
        uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        // 登録できなかった場合はここで例外になる（トレイアイコンを表示する前に確かめる）
        keyboardHook = new KeyboardHook(HandleInput, HandleInputFailed);

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
        NotifyIfMouseHookMissing();

        StartWatchingImeSettings();
        pollTimer.Tick += (_, _) =>
        {
            // IME の設定のキーが後から作られた場合（初めて切り替えたときなど）に、監視に切り替える
            if (imeSettingsWatcher is null)
            {
                StartWatchingImeSettings();
            }
            UpdateDisplay();
        };
        pollTimer.Start();

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            isDisposed = true;
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            pollTimer.Dispose();
            imeSettingsWatcher?.Dispose();
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

    /// <summary>もう一度起動されたときに、すでに起動していることと、操作する場所を知らせる。</summary>
    public void ShowAlreadyRunningNotice() =>
        notifyIcon.ShowBalloonTip(
            BalloonTipTimeoutMilliseconds,
            "PunctuationToggle",
            "PunctuationToggle はすでに起動しています。タスクトレイ（通知領域）のこのアイコンから操作できます。",
            ToolTipIcon.Info);

    // ---- 入力の処理 ----

    /// <summary>入力を1つ処理する。フックの専用のスレッドで呼ばれる。</summary>
    private HookAction HandleInput(KeyboardInput input)
    {
        if (triggerKeyRecorder is { } recorder)
        {
            return recorder.HandleInput(input) ? HookAction.Suppress : HookAction.PassThrough;
        }

        InputDecision decision;
        lock (detectorLock)
        {
            decision = detector.Handle(input);
        }
        if (decision.ShouldToggle)
        {
            // フックの処理が遅いと Windows にフックを外されるので、切り替えは UI スレッドで行う
            uiContext.Post(_ => TogglePunctuationStyle(bounceFocus: true), null);
        }
        return decision switch
        {
            { ShouldMaskMenu: true } => HookAction.ReplayKeyUpWithMenuMask,
            { ShouldSuppress: true } => HookAction.Suppress,
            _ => HookAction.PassThrough,
        };
    }

    /// <summary>
    /// 入力の処理で想定外の例外が起きたとき（フックの専用のスレッドで呼ばれる）。入力はすでにほかのアプリに渡している。
    /// 判定の途中の状態が分からないまま監視を続けないよう、UI スレッドで投げ直し、想定外の例外として扱う（Program を参照）。
    /// </summary>
    private void HandleInputFailed(Exception exception) =>
        uiContext.Post(_ => throw new InvalidOperationException("キー入力の処理中にエラーが起きました。", exception), null);

    private TriggerKey CurrentTriggerKey
    {
        get
        {
            lock (detectorLock)
            {
                return detector.TriggerKey;
            }
        }
        set
        {
            lock (detectorLock)
            {
                detector.TriggerKey = value;
            }
        }
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
            NotifyToggleFailed(exception);
            return;
        }
        isToggleFailing = false;
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
            // 切り替えキーが効かないと気づいた利用者はメニューを開くので、そのときにフックを登録し直す
            ReinstallKeyboardHook();
            launchAtLoginMenuItem.Checked = AppSettings.LaunchAtLogin;
            UpdateDisplay();
        };
        return menu;
    }

    private void UpdateDisplay()
    {
        var readStyle = MicrosoftImeSettings.TryReadPunctuationStyle();
        NotifyIfImeSettingsUnreadable(readStyle is null);
        // 読み込めない間は、前回読み込めた句読点のまま表示を続ける。起動したときから読み込めない場合は、
        // 既定値（「、。」）を今の設定のように見せず、分からないことを示す
        lastReadStyle = readStyle ?? lastReadStyle;
        var style = lastReadStyle;
        var triggerKey = CurrentTriggerKey;

        toggleMenuItem.Text = style switch
        {
            PunctuationStyle.CommaPeriod => $"「、。」に切り替え(&T)（{triggerKey.DisplayName}）",
            null => $"句読点を切り替え(&T)（{triggerKey.DisplayName}）",
            _ => $"「，．」に切り替え(&T)（{triggerKey.DisplayName}）",
        };
        usageMenuItem.Text = triggerKey.UsageDescription;
        // タスクトレイのアイコンの名前としてナレーターが読み上げるため、記号だけでなく句読点の呼び名も入れる
        notifyIcon.Text = isImeSettingsUnreadable || style is null
            ? "PunctuationToggle 句読点の設定を読み込めません"
            : $"PunctuationToggle 句読点: {style.Value.SpokenName()}";

        // 表示倍率の変更は、トレイ用のウィンドウが無く WM_DPICHANGED を受け取れないため、
        // 画面の設定の変更（DisplaySettingsChanged）と、設定を読み直すたびに確かめる
        var symbols = style?.Symbols() ?? UnknownStyleSymbol;
        var iconSize = TrayIconRenderer.CurrentIconSize;
        if (symbols == displayedSymbols && iconSize == displayedIconSize)
        {
            return;
        }
        displayedSymbols = symbols;
        displayedIconSize = iconSize;

        var previousIcon = currentIcon;
        currentIcon = TrayIconRenderer.Render(symbols, AppSettings.IsTaskbarLight, iconSize);
        notifyIcon.Icon = currentIcon;
        previousIcon?.Dispose();
    }

    /// <summary>
    /// 句読点の切り替えに失敗したことを、通知で知らせる。失敗が続いている間は、切り替えキーを何度押しても1回だけにする。
    /// </summary>
    /// <remarks>
    /// ダイアログ（MessageBox）は、切り替えキーで切り替えたときに前面に出ず（前面はほかのアプリ）、
    /// 表示中も切り替えキーで次のダイアログが重なって開くため、使わない。
    /// </remarks>
    private void NotifyToggleFailed(Exception exception)
    {
        if (isToggleFailing)
        {
            return;
        }
        isToggleFailing = true;
        notifyIcon.ShowBalloonTip(
            BalloonTipTimeoutMilliseconds,
            "PunctuationToggle",
            $"句読点の設定を変更できませんでした。{exception.Message}",
            ToolTipIcon.Warning);
    }

    /// <summary>
    /// 句読点の設定を読み込めなくなったときに、1回だけ通知で知らせる。
    /// 表示を更新するたびに読むため、ダイアログを繰り返し出さないよう、読み込めるようになるまでは知らせ直さない。
    /// </summary>
    private void NotifyIfImeSettingsUnreadable(bool isUnreadable)
    {
        if (isUnreadable && !isImeSettingsUnreadable)
        {
            notifyIcon.ShowBalloonTip(
                BalloonTipTimeoutMilliseconds,
                "PunctuationToggle",
                "Microsoft IME の句読点の設定を読み込めません。レジストリのアクセス権が変更されていないか確認してください。",
                ToolTipIcon.Warning);
        }
        isImeSettingsUnreadable = isUnreadable;
    }

    private void StartWatchingImeSettings()
    {
        // 通知はスレッドプールで呼ばれるので、表示の更新は UI スレッドに移ってから行う
        RegistryChangeWatcher? watcher = null;
        watcher = MicrosoftImeSettings.WatchChanges(
            onChanged: () => PostUnlessDisposed(UpdateDisplay),
            // 通知は TryStart から戻った後に UI スレッドで処理されるので、watcher は代入済み
            onStopped: () => PostUnlessDisposed(() => StopWatchingImeSettings(watcher)));
        imeSettingsWatcher = watcher;
        pollTimer.Interval = imeSettingsWatcher is null
            ? PollIntervalWithoutWatcherMilliseconds
            : PollIntervalWithWatcherMilliseconds;
    }

    /// <summary>
    /// 監視が止まったとき（IME の設定のキーが削除された場合など）に、短い間隔の読み直しに戻す。
    /// 次の読み直しで、キーがあれば監視をやり直す。
    /// </summary>
    private void StopWatchingImeSettings(RegistryChangeWatcher? stoppedWatcher)
    {
        if (stoppedWatcher is null)
        {
            return;
        }
        if (imeSettingsWatcher == stoppedWatcher)
        {
            imeSettingsWatcher = null;
            pollTimer.Interval = PollIntervalWithoutWatcherMilliseconds;
        }
        stoppedWatcher.Dispose();
    }

    /// <summary>ほかのスレッドから、UI スレッドで処理を行う。Dispose を始めた後は何もしない。</summary>
    private void PostUnlessDisposed(Action action)
    {
        if (isDisposed)
        {
            return;
        }
        uiContext.Post(_ =>
        {
            if (!isDisposed)
            {
                action();
            }
        }, null);
    }

    /// <summary>
    /// フックを登録し直し、判定の途中の状態を消す。フックが Windows に外されていた場合（外されたことは
    /// アプリに知らされない）に、切り替えキーを使えるように戻す。古いフックを外してから登録するので、二重にはならない。
    /// </summary>
    private void ReinstallKeyboardHook()
    {
        keyboardHook.Reinstall();
        lock (detectorLock)
        {
            detector.Reset();
        }
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs eventArgs)
    {
        // 画面のロックの間は入力がフックに届かず、ロックの解除の後にフックが外されていることもある
        if (eventArgs.Reason == SessionSwitchReason.SessionUnlock)
        {
            ReinstallKeyboardHook();
        }
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs eventArgs)
    {
        if (eventArgs.Mode == PowerModes.Resume)
        {
            ReinstallKeyboardHook();
        }
    }

    /// <summary>マウスのフックを登録できなかった場合に、起動時に1回だけ知らせる。</summary>
    private void NotifyIfMouseHookMissing()
    {
        if (keyboardHook.MouseHookErrorCode is not { } errorCode)
        {
            return;
        }
        notifyIcon.ShowBalloonTip(
            BalloonTipTimeoutMilliseconds,
            "PunctuationToggle",
            "マウスの操作を監視できないため、切り替えキー（修飾キー）を押したままクリックしても、句読点が切り替わることがあります。" +
            $"（{new Win32Exception(errorCode).Message}）",
            ToolTipIcon.Warning);
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs eventArgs)
    {
        // 表示倍率を変えたら、その倍率の大きさでアイコンを描き直す（UpdateDisplay で大きさの変化を確かめる）
        UpdateDisplay();
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs eventArgs)
    {
        // タスクバーのライト・ダークが切り替わったら、文字の色を合わせて描き直す
        if (eventArgs.Category == UserPreferenceCategory.General)
        {
            displayedSymbols = null;
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

        var recorder = new TriggerKeyRecorderForm(CurrentTriggerKey);
        recorder.Finished += newKey =>
        {
            triggerKeyRecorder = null;
            if (newKey is not null)
            {
                // 保存できなかったキーを今回の起動中だけ使うと、再起動で元に戻って分かりにくいため、変更しない。
                try
                {
                    AppSettings.TriggerKey = newKey;
                    CurrentTriggerKey = newKey;
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
        // フックから入力を渡すのは、ウィンドウのハンドルを作ってから（HandleInput が BeginInvoke を使う）
        recorder.Show();
        triggerKeyRecorder = recorder;
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
