using System.Runtime.InteropServices;

namespace PunctuationToggle;

/// <summary>
/// 切り替えキーを設定するウィンドウ。
/// </summary>
/// <remarks>
/// 開いている間は <see cref="HandleInput"/> に入力を渡してもらい、次に押されたキーを新しい切り替えキーとして読み取る。
/// キー入力は <see cref="KeyboardHook"/> で受け取る（修飾キーの単独押しや Windows キーはウィンドウのイベントでは扱いにくいため）。
/// ほかのアプリでの入力を読み取ったり握りつぶしたりしないよう、このウィンドウが前面にある間だけ読み取り、
/// ほかのウィンドウに切り替えたら設定を中止して閉じる（開いている間は切り替えキーが効かないため、残さない）。
/// <see cref="HandleInput"/> はフックの専用のスレッドで呼ばれる。<see cref="TriggerKeyCapture"/> はそのスレッドだけで使い、
/// 画面の更新は BeginInvoke で UI スレッドに移して行う。
/// </remarks>
internal sealed partial class TriggerKeyRecorderForm : Form
{
    private const string InstructionText =
        "修飾キー（Shift・Ctrl・Alt・Windows）は単独で押して離してください。" +
        "ほかに F1〜F24、変換、無変換、アプリケーション、Pause、Scroll Lock が使えます。Esc でキャンセルします。";

    private const string InactiveMessage = "このウィンドウをクリックしてから、キーを押してください。";

    // 案内を折り返す幅（96 DPI でのピクセル。表示倍率に合わせて拡大される）
    private const int MessageWidth = 380;

    // 案内の文字色。ウィンドウの背景（SystemColors.Control。Windows 11 の既定で #F0F0F0）に対して、
    // WCAG 2.2 の達成基準 1.4.3 が通常の大きさの文字に求める 4.5:1 以上のコントラスト比にする。
    // #9C4A00 は 5.43:1（以前の Color.DarkOrange は 2.05:1 で足りなかった）、Firebrick（#B22222）は 5.86:1
    private static readonly Color InactiveMessageColor = Color.FromArgb(0x9C, 0x4A, 0x00);
    private static readonly Color RejectedMessageColor = Color.Firebrick;

    private readonly TriggerKeyCapture capture = new();
    // 本文はアプリの既定のフォント（PunctuationToggle.csproj の ApplicationDefaultFont）、見出しはその太字
    private readonly Font titleFont;
    private readonly Label statusLabel;

    // UI スレッドで設定し、フックの専用のスレッドで読む
    private volatile bool isFinished;
    private nint windowHandle;

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    /// <summary>読み取りが終わったときに1度だけ発生する。キャンセルされた場合は null。</summary>
    public event Action<TriggerKey?>? Finished;

    public TriggerKeyRecorderForm(TriggerKey currentKey)
    {
        // 余白や折り返しの幅（Padding・Margin・MaximumSize）は 96 DPI（表示倍率 100%）のピクセルで書き、
        // 表示倍率に合わせて Windows Forms に拡大させる（フォントはポイントで指定しているので倍率に合わせて大きくなる）。
        // PerMonitorV2 なので、倍率の違うモニターに動かしたときも拡大し直される
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "切り替えキーの設定";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        titleFont = new Font(Font, FontStyle.Bold);

        var titleLabel = new Label
        {
            Text = "新しい切り替えキーを押してください",
            Font = titleFont,
            AutoSize = true,
        };
        var currentKeyLabel = new Label { Text = $"現在の切り替えキー: {currentKey.DisplayName}", AutoSize = true };
        var instructionLabel = new Label
        {
            Text = InstructionText,
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            MaximumSize = new Size(MessageWidth, 0),
        };
        // 使えないキーを押したときなどの短い知らせ。変わらない案内（instructionLabel）とは分け、
        // 知らせが変わったときに、短い知らせだけをナレーターなどのスクリーンリーダーに読み上げさせる。
        // LiveSetting を設定したラベルは、Text を変えるとライブ リージョンの変化を UI オートメーションに通知する
        statusLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(MessageWidth, 0),
            LiveSetting = System.Windows.Forms.Automation.AutomationLiveSetting.Polite,
        };

        var resetButton = new Button { Text = $"初期設定（{TriggerKey.Default.DisplayName}）に戻す", AutoSize = true };
        resetButton.Click += (_, _) => Finish(TriggerKey.Default);
        var cancelButton = new Button { Text = "キャンセル", AutoSize = true };
        cancelButton.Click += (_, _) => Finish(null);

        var buttonRow = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
        buttonRow.Controls.AddRange([resetButton, cancelButton]);

        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Padding = new Padding(16),
            WrapContents = false,
        };
        layout.Controls.AddRange([titleLabel, currentKeyLabel, instructionLabel, statusLabel, buttonRow]);
        Controls.Add(layout);

        // Tab・Enter・スペースはボタンの操作に使えるが、開いた直後にボタンにフォーカスがあると、
        // Enter やスペースを押しただけで「初期設定に戻す」が押されてしまうため、Tab で選ぶまではどのボタンも選ばない
        Shown += (_, _) => ActiveControl = null;
        FormClosed += (_, _) => Finish(null);
        Deactivate += (_, _) => Finish(null);

        ResumeLayout(performLayout: false);
        PerformLayout();
    }

    /// <summary>入力を1つ処理する。true を返した入力はほかのアプリに渡さない。フックの専用のスレッドで呼ばれる。</summary>
    public bool HandleInput(KeyboardInput input)
    {
        if (isFinished)
        {
            return false;
        }

        // ActiveForm は UI スレッドでしか正しく読めないため、前面のウィンドウがこのウィンドウかで調べる
        var handle = Volatile.Read(ref windowHandle);
        var isActive = handle != 0 && GetForegroundWindow() == handle;
        var result = isActive ? capture.Handle(input) : capture.HandleWhileInactive(input);
        if (!isActive && input.Kind == KeyboardInputKind.KeyDown)
        {
            // Windows がこのウィンドウを前面にしなかった場合に、何も起きないように見えないよう案内する
            PostToUi(ShowInactiveMessage);
        }
        switch (result.Outcome)
        {
            case CaptureOutcome.Captured:
                PostToUi(() => Finish(result.CapturedKey));
                break;
            case CaptureOutcome.Rejected:
                PostToUi(() => ShowRejectedMessage(result.RejectedVirtualKey));
                break;
            case CaptureOutcome.Cancelled:
                PostToUi(() => Finish(null));
                break;
            case CaptureOutcome.Waiting:
            default:
                break;
        }
        return result.ShouldSuppress;
    }

    /// <summary>画面の更新を UI スレッドに移す。ウィンドウがすでに閉じられていれば何もしない。</summary>
    private void PostToUi(Action action)
    {
        try
        {
            BeginInvoke(action);
        }
        // 閉じる処理（Finish）とフックからの入力が重なり、ハンドルが破棄された後に呼ばれた場合
        catch (InvalidOperationException)
        {
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Volatile.Write(ref windowHandle, Handle);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        Volatile.Write(ref windowHandle, 0);
        base.OnHandleDestroyed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            titleFont.Dispose();
        }
        base.Dispose(disposing);
    }

    private void ShowInactiveMessage()
    {
        // 前面にない間は、ほかのアプリで文字を入力していることがある。1文字ごとに同じ知らせを設定し直して
        // スクリーンリーダーが繰り返し読み上げないよう、すでに表示していれば何もしない
        if (statusLabel.Text == InactiveMessage)
        {
            return;
        }
        statusLabel.Text = InactiveMessage;
        statusLabel.ForeColor = StatusColor(InactiveMessageColor);
    }

    private void ShowRejectedMessage(int virtualKey)
    {
        statusLabel.Text = $"そのキー（{KeyName(virtualKey)}）は使えません。";
        statusLabel.ForeColor = StatusColor(RejectedMessageColor);
    }

    /// <summary>
    /// 案内の文字色。ハイコントラスト（コントラスト テーマ）では、テーマの文字色に従う。
    /// 色だけで状態を伝えないよう、文言も変えている。
    /// </summary>
    private static Color StatusColor(Color normalColor) =>
        SystemInformation.HighContrast ? SystemColors.ControlText : normalColor;

    /// <summary>利用者に示すキーの名前。名前を求められないキーは仮想キーコードで示す。</summary>
    private static string KeyName(int virtualKey)
    {
        if (VirtualKeyNames.For(virtualKey) is { } name)
        {
            return name;
        }
        var key = (Keys)virtualKey;
        return Enum.IsDefined(key) ? key.ToString() : $"仮想キーコード 0x{virtualKey:X2}";
    }

    private void Finish(TriggerKey? key)
    {
        if (isFinished)
        {
            return;
        }
        isFinished = true;
        Finished?.Invoke(key);
        Close();
    }
}
