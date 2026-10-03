namespace PunctuationToggle;

/// <summary>1つの入力に対して、句読点を切り替えるか、入力をほかのアプリに渡さないかの判断。</summary>
public readonly record struct InputDecision(bool ShouldToggle = false, bool ShouldSuppress = false)
{
    public static InputDecision PassThrough => default;
}

/// <summary>
/// 入力を順に受け取り、切り替えキーが押されたことを検出する。
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>修飾キーの場合: 単独で押して離したときだけ切り替える。押している間にほかのキー・修飾キー・
/// マウスが使われた場合（Ctrl＋C など）は切り替えない。入力はすべてそのまま渡す。</item>
/// <item>それ以外のキーの場合: 修飾キーなしで押されたときに切り替え、そのキーの入力は渡さない。
/// 修飾キーと一緒に押された場合（Ctrl＋F13 など）はショートカットとしてそのまま渡す。</item>
/// </list>
/// </remarks>
public sealed class TriggerKeyDetector(TriggerKey triggerKey)
{
    // 修飾キーを押してから離すまでの間に、ほかの入力があったか
    private bool isTriggerModifierHeld;
    private bool wasUsedWithOtherInput;

    // 修飾キー以外の切り替えキーを押し下げ中で、離すまで入力を渡さないか
    private bool isSuppressingTriggerKey;

    public TriggerKey TriggerKey
    {
        get => triggerKey;
        set
        {
            triggerKey = value;
            Reset();
        }
    }

    public InputDecision Handle(KeyboardInput input) =>
        triggerKey.IsModifier ? HandleWithModifierTrigger(input) : HandleWithRegularTrigger(input);

    public void Reset()
    {
        isTriggerModifierHeld = false;
        wasUsedWithOtherInput = false;
        isSuppressingTriggerKey = false;
    }

    private InputDecision HandleWithModifierTrigger(KeyboardInput input)
    {
        var isTriggerKey = input.VirtualKey == triggerKey.VirtualKey;

        switch (input.Kind)
        {
            case KeyboardInputKind.KeyDown when isTriggerKey:
                // 押しっぱなしにすると修飾キーもリピートするので、最初の押し下げだけを見る
                if (!input.IsRepeat)
                {
                    isTriggerModifierHeld = true;
                    // Shift を押したまま右Ctrl を押した場合なども、単独押しとはみなさない
                    wasUsedWithOtherInput = input.OtherModifiersHeld;
                }
                break;

            case KeyboardInputKind.KeyUp when isTriggerKey:
                if (isTriggerModifierHeld)
                {
                    isTriggerModifierHeld = false;
                    return new InputDecision(ShouldToggle: !wasUsedWithOtherInput);
                }
                break;

            case KeyboardInputKind.KeyDown:
            case KeyboardInputKind.Mouse:
                if (isTriggerModifierHeld)
                {
                    wasUsedWithOtherInput = true;
                }
                break;

            case KeyboardInputKind.KeyUp:
            default:
                break;
        }
        return InputDecision.PassThrough;
    }

    private InputDecision HandleWithRegularTrigger(KeyboardInput input)
    {
        if (input.VirtualKey != triggerKey.VirtualKey)
        {
            return InputDecision.PassThrough;
        }

        switch (input.Kind)
        {
            case KeyboardInputKind.KeyDown:
                if (isSuppressingTriggerKey)
                {
                    // 押しっぱなしによるリピート
                    return new InputDecision(ShouldSuppress: true);
                }
                if (input.OtherModifiersHeld)
                {
                    return InputDecision.PassThrough;
                }
                isSuppressingTriggerKey = true;
                return new InputDecision(ShouldToggle: !input.IsRepeat, ShouldSuppress: true);

            case KeyboardInputKind.KeyUp when isSuppressingTriggerKey:
                isSuppressingTriggerKey = false;
                return new InputDecision(ShouldSuppress: true);

            default:
                return InputDecision.PassThrough;
        }
    }
}
