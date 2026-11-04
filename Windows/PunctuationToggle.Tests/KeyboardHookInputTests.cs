namespace PunctuationToggle.Tests;

public class KeyboardHookInputTests
{
    private const int NoFlags = 0;

    // KBDLLHOOKSTRUCT の flags の LLKHF_INJECTED（どのプロセスから注入されたかを問わず、注入された入力）
    private const int InjectedFlag = 0x10;

    // WM_CHAR は低レベルフックには届かないが、想定外のメッセージの例として使う
    private const int CharMessage = 0x0102;

    private static Func<int, bool> KeysDown(params int[] virtualKeys) => virtualKeys.Contains;

    [Theory]
    [InlineData(KeyboardHookInput.KeyDownMessage)]
    [InlineData(KeyboardHookInput.SystemKeyDownMessage)]
    public void 押し下げのメッセージはKeyDownになる(int message)
    {
        var input = KeyboardHookInput.FromHookMessage(message, VirtualKeys.F1, NoFlags, KeysDown());
        Assert.Equal(KeyboardInput.KeyDown(VirtualKeys.F1), input);
    }

    [Theory]
    [InlineData(KeyboardHookInput.KeyUpMessage)]
    [InlineData(KeyboardHookInput.SystemKeyUpMessage)]
    public void 離したメッセージはKeyUpになる(int message)
    {
        var input = KeyboardHookInput.FromHookMessage(message, VirtualKeys.F1, NoFlags, KeysDown(VirtualKeys.F1));
        Assert.Equal(KeyboardInput.KeyUp(VirtualKeys.F1), input);
    }

    [Fact]
    public void すでに押されているキーの押し下げはリピートになる()
    {
        var input = KeyboardHookInput.FromHookMessage(
            KeyboardHookInput.KeyDownMessage, VirtualKeys.RightControl, NoFlags, KeysDown(VirtualKeys.RightControl));
        Assert.Equal(KeyboardInput.KeyDown(VirtualKeys.RightControl, isRepeat: true), input);
    }

    [Fact]
    public void 右Ctrlだけが押されているときの右Ctrlの押し下げはほかの修飾キーなし()
    {
        var input = KeyboardHookInput.FromHookMessage(
            KeyboardHookInput.KeyDownMessage, VirtualKeys.RightControl, NoFlags, KeysDown(VirtualKeys.RightControl));
        Assert.False(input?.OtherModifiersHeld);
    }

    [Fact]
    public void 左Shiftが押されているときはほかの修飾キーあり()
    {
        var input = KeyboardHookInput.FromHookMessage(
            KeyboardHookInput.KeyDownMessage, VirtualKeys.RightControl, NoFlags, KeysDown(VirtualKeys.LeftShift));
        Assert.Equal(KeyboardInput.KeyDown(VirtualKeys.RightControl, otherModifiersHeld: true), input);
    }

    [Fact]
    public void 修飾キー以外が押されていてもほかの修飾キーには数えない()
    {
        var input = KeyboardHookInput.FromHookMessage(
            KeyboardHookInput.KeyDownMessage, VirtualKeys.RightControl, NoFlags, KeysDown(0x41)); // A
        Assert.False(input?.OtherModifiersHeld);
    }

    [Fact]
    public void ほかのメッセージは判定に使わない()
    {
        Assert.Null(KeyboardHookInput.FromHookMessage(CharMessage, 0x41, NoFlags, KeysDown()));
    }

    [Fact]
    public void 整合性レベルが低いプロセスが注入した入力は判定に使わない()
    {
        // LLKHF_LOWER_IL_INJECTED が立つ入力は LLKHF_INJECTED も立つ
        const int lowerIntegrityLevelInjected = KeyboardHookInput.LowerIntegrityLevelInjectedFlag | InjectedFlag;
        Assert.Null(KeyboardHookInput.FromHookMessage(
            KeyboardHookInput.KeyDownMessage, VirtualKeys.RightControl, lowerIntegrityLevelInjected, KeysDown()));
        Assert.Null(KeyboardHookInput.FromHookMessage(
            KeyboardHookInput.KeyUpMessage, VirtualKeys.RightControl, lowerIntegrityLevelInjected, KeysDown()));
    }

    [Fact]
    public void 同じ整合性レベルのプロセスが注入した入力は判定に使う()
    {
        // PowerToys の Keyboard Manager や AutoHotkey でキーを割り当て直した入力
        Assert.Equal(
            KeyboardInput.KeyDown(VirtualKeys.RightControl),
            KeyboardHookInput.FromHookMessage(
                KeyboardHookInput.KeyDownMessage, VirtualKeys.RightControl, InjectedFlag, KeysDown()));
    }
}
