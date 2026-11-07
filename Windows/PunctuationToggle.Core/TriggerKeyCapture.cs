namespace PunctuationToggle;

public enum CaptureOutcome
{
    /// <summary>まだキーが押されていない。</summary>
    Waiting,

    /// <summary>切り替えキーとして使えるキーが押された。</summary>
    Captured,

    /// <summary>切り替えキーとして使えないキーが押された（読み取りは続ける）。</summary>
    Rejected,

    /// <summary>Esc が押された。</summary>
    Cancelled,
}

/// <summary><see cref="TriggerKeyCapture.Handle"/> の結果。</summary>
/// <param name="Outcome">読み取りの状態。</param>
/// <param name="ShouldSuppress">この入力をほかのアプリに渡さないか。</param>
/// <param name="CapturedKey"><see cref="CaptureOutcome.Captured"/> のとき、読み取ったキー。</param>
/// <param name="RejectedVirtualKey"><see cref="CaptureOutcome.Rejected"/> のとき、押されたキーの仮想キーコード。</param>
public readonly record struct CaptureResult(
    CaptureOutcome Outcome,
    bool ShouldSuppress,
    TriggerKey? CapturedKey = null,
    int RejectedVirtualKey = 0);

/// <summary>
/// 切り替えキーの設定画面で、次に押されたキーを読み取る。
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>修飾キーは、単独で押して離したときに読み取る（Ctrl＋C のような組み合わせは無視する）。</item>
/// <item>それ以外のキーは、押したときに読み取る。押したキーの入力はほかのアプリに渡さない。
/// ただし、修飾キーと一緒に押したキー（Alt＋F4 など）は、OS やアプリのショートカットなので読み取らずに渡す。</item>
/// <item>Esc でキャンセルする。</item>
/// <item>Tab・Enter・スペースは読み取らず、設定画面のボタンの操作に使えるよう渡す。</item>
/// <item>設定画面が前面にない間の入力は <see cref="HandleWhileInactive"/> に渡し、読み取らずにほかのアプリへ渡す。</item>
/// </list>
/// </remarks>
public sealed class TriggerKeyCapture
{
    // 単独で押されている修飾キー
    private int? pendingModifier;
    // 押し下げを渡さなかったキー（離したときの入力も渡さない）
    private int? suppressedKey;

    public CaptureResult Handle(KeyboardInput input)
    {
        var virtualKey = input.VirtualKey;

        switch (input.Kind)
        {
            case KeyboardInputKind.KeyDown when VirtualKeys.IsModifier(virtualKey):
                if (!input.IsRepeat)
                {
                    var isAlone = pendingModifier is null && !input.OtherModifiersHeld;
                    pendingModifier = isAlone ? virtualKey : null;
                }
                return new CaptureResult(CaptureOutcome.Waiting, ShouldSuppress: false);

            case KeyboardInputKind.KeyUp when VirtualKeys.IsModifier(virtualKey):
                if (pendingModifier != virtualKey)
                {
                    return new CaptureResult(CaptureOutcome.Waiting, ShouldSuppress: false);
                }
                pendingModifier = null;
                return ResultFor(virtualKey, shouldSuppress: false);

            // 設定画面のボタンをキーボードで操作するキー。どれも切り替えキーにできないので、読み取らずに画面へ渡す
            case KeyboardInputKind.KeyDown when VirtualKeys.IsDialogNavigation(virtualKey):
                pendingModifier = null;
                return new CaptureResult(CaptureOutcome.Waiting, ShouldSuppress: false);

            // 修飾キーと一緒に押したキーは、設定画面を閉じる Alt＋F4 などのショートカットなので、読み取らずに渡す
            // （読み取ると、F4 が切り替えキーとして保存され、Alt＋F4 も効かなくなる）
            case KeyboardInputKind.KeyDown when input.OtherModifiersHeld:
                pendingModifier = null;
                return new CaptureResult(CaptureOutcome.Waiting, ShouldSuppress: false);

            case KeyboardInputKind.KeyDown:
                pendingModifier = null;
                suppressedKey = virtualKey;
                return virtualKey == VirtualKeys.Escape
                    ? new CaptureResult(CaptureOutcome.Cancelled, ShouldSuppress: true)
                    : ResultFor(virtualKey, shouldSuppress: true);

            case KeyboardInputKind.KeyUp:
                var shouldSuppress = suppressedKey == virtualKey;
                if (shouldSuppress)
                {
                    suppressedKey = null;
                }
                return new CaptureResult(CaptureOutcome.Waiting, shouldSuppress);

            case KeyboardInputKind.Mouse:
            default:
                pendingModifier = null;
                return new CaptureResult(CaptureOutcome.Waiting, ShouldSuppress: false);
        }
    }

    /// <summary>
    /// 設定画面が前面にない（ほかのアプリを操作している）ときの入力を処理する。
    /// キーは読み取らず、入力もほかのアプリに渡す。
    /// ただし、前面にあったときに押し下げを渡さなかったキーは、離したときの入力も渡さない。
    /// </summary>
    public CaptureResult HandleWhileInactive(KeyboardInput input)
    {
        pendingModifier = null;
        if (input.Kind != KeyboardInputKind.KeyUp || suppressedKey != input.VirtualKey)
        {
            return new CaptureResult(CaptureOutcome.Waiting, ShouldSuppress: false);
        }
        suppressedKey = null;
        return new CaptureResult(CaptureOutcome.Waiting, ShouldSuppress: true);
    }

    private static CaptureResult ResultFor(int virtualKey, bool shouldSuppress) =>
        TriggerKey.FromVirtualKey(virtualKey) is { } key
            ? new CaptureResult(CaptureOutcome.Captured, shouldSuppress, CapturedKey: key)
            : new CaptureResult(CaptureOutcome.Rejected, shouldSuppress, RejectedVirtualKey: virtualKey);
}
