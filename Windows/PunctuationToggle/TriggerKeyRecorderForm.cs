namespace PunctuationToggle;

/// <summary>
/// 切り替えキーを設定するウィンドウ。
/// </summary>
/// <remarks>
/// 開いている間は <see cref="HandleInput"/> に入力を渡してもらい、次に押されたキーを新しい切り替えキーとして読み取る。
/// キー入力は <see cref="KeyboardHook"/> で受け取る（修飾キーの単独押しや Windows キーはウィンドウのイベントでは扱いにくいため）。
/// ほかのアプリでの入力を読み取ったり握りつぶしたりしないよう、このウィンドウが前面にある間だけ読み取り、
/// ほかのウィンドウに切り替えたら設定を中止して閉じる（開いている間は切り替えキーが効かないため、残さない）。
/// </remarks>
internal sealed class TriggerKeyRecorderForm : Form
{
    private const string InstructionText =
        "修飾キー（Shift・Ctrl・Alt・Windows）は単独で押して離してください。" +
        "ほかに F1〜F24、変換、無変換、アプリケーション、Pause、Scroll Lock が使えます。Esc でキャンセルします。";

    private readonly TriggerKeyCapture capture = new();
    private readonly Font bodyFont = new("Yu Gothic UI", 9F);
    private readonly Font titleFont = new("Yu Gothic UI", 9F, FontStyle.Bold);
    private readonly Label statusLabel;
    private bool isFinished;

    /// <summary>読み取りが終わったときに1度だけ発生する。キャンセルされた場合は null。</summary>
    public event Action<TriggerKey?>? Finished;

    public TriggerKeyRecorderForm(TriggerKey currentKey)
    {
        Text = "切り替えキーの設定";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = bodyFont;

        var titleLabel = new Label
        {
            Text = "新しい切り替えキーを押してください",
            Font = titleFont,
            AutoSize = true,
        };
        var currentKeyLabel = new Label { Text = $"現在の切り替えキー: {currentKey.DisplayName}", AutoSize = true };
        statusLabel = new Label
        {
            Text = InstructionText,
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            MaximumSize = new Size(380, 0),
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
        layout.Controls.AddRange([titleLabel, currentKeyLabel, statusLabel, buttonRow]);
        Controls.Add(layout);

        FormClosed += (_, _) => Finish(null);
        Deactivate += (_, _) => Finish(null);
    }

    /// <summary>入力を1つ処理する。true を返した入力はほかのアプリに渡さない。</summary>
    public bool HandleInput(KeyboardInput input)
    {
        if (isFinished)
        {
            return false;
        }

        // フックはこのウィンドウと同じ UI スレッドで呼ばれるので、ActiveForm をそのまま調べられる
        var result = ActiveForm == this ? capture.Handle(input) : capture.HandleWhileInactive(input);
        switch (result.Outcome)
        {
            case CaptureOutcome.Captured:
                // キーボードフックの処理を早く終えるため、ウィンドウを閉じるのはフックを抜けてから
                BeginInvoke(() => Finish(result.CapturedKey));
                break;
            case CaptureOutcome.Rejected:
                ShowRejectedMessage(result.RejectedVirtualKey);
                break;
            case CaptureOutcome.Cancelled:
                BeginInvoke(() => Finish(null));
                break;
            case CaptureOutcome.Waiting:
            default:
                break;
        }
        return result.ShouldSuppress;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            bodyFont.Dispose();
            titleFont.Dispose();
        }
        base.Dispose(disposing);
    }

    private void ShowRejectedMessage(int virtualKey)
    {
        statusLabel.Text = $"そのキー（仮想キーコード 0x{virtualKey:X2}）は使えません。{Environment.NewLine}{InstructionText}";
        statusLabel.ForeColor = Color.Firebrick;
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
