namespace PunctuationToggle.Tests;

public class ModifierTriggerTests
{
    private const int LetterC = 0x43;

    private readonly TriggerKeyDetector detector = new(TriggerKey.RightControl);

    [Fact]
    public void 単独で押して離すと切り替える()
    {
        Assert.Equal(InputDecision.PassThrough, detector.Handle(KeyboardInput.KeyDown(VirtualKeys.RightControl)));
        Assert.Equal(new InputDecision(ShouldToggle: true), detector.Handle(KeyboardInput.KeyUp(VirtualKeys.RightControl)));
    }

    [Fact]
    public void 押しっぱなしのリピートがあっても単独押しとして切り替える()
    {
        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.RightControl));
        foreach (var _ in Enumerable.Range(0, 5))
        {
            detector.Handle(KeyboardInput.KeyDown(VirtualKeys.RightControl, isRepeat: true));
        }
        Assert.True(detector.Handle(KeyboardInput.KeyUp(VirtualKeys.RightControl)).ShouldToggle);
    }

    [Fact]
    public void ショートカットでは切り替えない()
    {
        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.RightControl));
        detector.Handle(KeyboardInput.KeyDown(LetterC, otherModifiersHeld: true));
        detector.Handle(KeyboardInput.KeyUp(LetterC));
        Assert.False(detector.Handle(KeyboardInput.KeyUp(VirtualKeys.RightControl)).ShouldToggle);

        // 次の単独押しは切り替える
        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.RightControl));
        Assert.True(detector.Handle(KeyboardInput.KeyUp(VirtualKeys.RightControl)).ShouldToggle);
    }

    [Fact]
    public void 押している間にほかの修飾キーを押したら切り替えない()
    {
        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.RightControl));
        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.LeftShift, otherModifiersHeld: true));
        detector.Handle(KeyboardInput.KeyUp(VirtualKeys.LeftShift));
        Assert.False(detector.Handle(KeyboardInput.KeyUp(VirtualKeys.RightControl)).ShouldToggle);
    }

    [Fact]
    public void ほかの修飾キーを押したまま押した場合は切り替えない()
    {
        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.LeftShift));
        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.RightControl, otherModifiersHeld: true));
        Assert.False(detector.Handle(KeyboardInput.KeyUp(VirtualKeys.RightControl)).ShouldToggle);
    }

    [Fact]
    public void Ctrlクリックでは切り替えない()
    {
        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.RightControl));
        detector.Handle(KeyboardInput.Mouse);
        Assert.False(detector.Handle(KeyboardInput.KeyUp(VirtualKeys.RightControl)).ShouldToggle);
    }

    [Fact]
    public void 左Ctrlの単独押しでは切り替えない()
    {
        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.LeftControl));
        Assert.False(detector.Handle(KeyboardInput.KeyUp(VirtualKeys.LeftControl)).ShouldToggle);
    }

    [Fact]
    public void 押していないのに離された入力だけが来ても切り替えない()
    {
        Assert.False(detector.Handle(KeyboardInput.KeyUp(VirtualKeys.RightControl)).ShouldToggle);
    }

    [Fact]
    public void 離した入力を取りこぼしても次の押し下げからやり直す()
    {
        // ロック画面への切り替えなどで、離した入力が届かなかった場合
        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.RightControl));
        detector.Handle(KeyboardInput.KeyDown(LetterC, otherModifiersHeld: true));

        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.RightControl));
        Assert.True(detector.Handle(KeyboardInput.KeyUp(VirtualKeys.RightControl)).ShouldToggle);
    }

    [Fact]
    public void 修飾キーの入力は常にそのまま渡す()
    {
        KeyboardInput[] inputs =
        [
            KeyboardInput.KeyDown(VirtualKeys.RightControl),
            KeyboardInput.KeyDown(LetterC, otherModifiersHeld: true),
            KeyboardInput.KeyUp(LetterC),
            KeyboardInput.Mouse,
            KeyboardInput.KeyUp(VirtualKeys.RightControl),
            KeyboardInput.KeyDown(VirtualKeys.RightControl),
            KeyboardInput.KeyUp(VirtualKeys.RightControl),
        ];
        Assert.All(inputs, input => Assert.False(detector.Handle(input).ShouldSuppress));
    }

    [Fact]
    public void 切り替えキーを変えると押し下げ中の状態は破棄される()
    {
        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.RightControl));
        detector.TriggerKey = TriggerKey.FromVirtualKey(VirtualKeys.RightAlt)!;
        Assert.False(detector.Handle(KeyboardInput.KeyUp(VirtualKeys.RightControl)).ShouldToggle);

        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.RightAlt));
        Assert.True(detector.Handle(KeyboardInput.KeyUp(VirtualKeys.RightAlt)).ShouldToggle);
    }
}

public class RegularTriggerTests
{
    private const int F13 = VirtualKeys.F1 + 12;

    private readonly TriggerKeyDetector detector = new(TriggerKey.FromVirtualKey(VirtualKeys.NonConvert)!);

    [Fact]
    public void 押したときに切り替え入力は渡さない()
    {
        Assert.Equal(
            new InputDecision(ShouldToggle: true, ShouldSuppress: true),
            detector.Handle(KeyboardInput.KeyDown(VirtualKeys.NonConvert)));
        Assert.Equal(
            new InputDecision(ShouldSuppress: true),
            detector.Handle(KeyboardInput.KeyUp(VirtualKeys.NonConvert)));
    }

    [Fact]
    public void 押しっぱなしのリピートでは切り替えないが入力は渡さない()
    {
        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.NonConvert));
        foreach (var _ in Enumerable.Range(0, 5))
        {
            Assert.Equal(
                new InputDecision(ShouldSuppress: true),
                detector.Handle(KeyboardInput.KeyDown(VirtualKeys.NonConvert, isRepeat: true)));
        }
        Assert.True(detector.Handle(KeyboardInput.KeyUp(VirtualKeys.NonConvert)).ShouldSuppress);
    }

    [Fact]
    public void 修飾キーと一緒に押された場合はそのまま渡す()
    {
        Assert.Equal(
            InputDecision.PassThrough,
            detector.Handle(KeyboardInput.KeyDown(VirtualKeys.NonConvert, otherModifiersHeld: true)));
        Assert.Equal(InputDecision.PassThrough, detector.Handle(KeyboardInput.KeyUp(VirtualKeys.NonConvert)));
    }

    [Fact]
    public void ほかのキーには影響しない()
    {
        Assert.Equal(InputDecision.PassThrough, detector.Handle(KeyboardInput.KeyDown(F13)));
        Assert.Equal(InputDecision.PassThrough, detector.Handle(KeyboardInput.KeyUp(F13)));
        Assert.Equal(InputDecision.PassThrough, detector.Handle(KeyboardInput.KeyDown(VirtualKeys.RightControl)));
        Assert.Equal(InputDecision.PassThrough, detector.Handle(KeyboardInput.KeyUp(VirtualKeys.RightControl)));
        Assert.Equal(InputDecision.PassThrough, detector.Handle(KeyboardInput.Mouse));
    }

    [Fact]
    public void 起動前から押されていたキーのリピートでは切り替えない()
    {
        Assert.Equal(
            new InputDecision(ShouldSuppress: true),
            detector.Handle(KeyboardInput.KeyDown(VirtualKeys.NonConvert, isRepeat: true)));
    }
}
