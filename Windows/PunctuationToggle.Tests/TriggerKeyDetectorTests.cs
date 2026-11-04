namespace PunctuationToggle.Tests;

// 両 OS に共通の判定は、testdata/trigger_key_vectors.json（TriggerKeyVectorTests）で確かめる。
// ここには Windows 版だけのキーや、入力の列では表せない操作（切り替えキーの変更・Reset）のテストを置く。

public class ModifierTriggerTests
{
    private readonly TriggerKeyDetector detector = new(TriggerKey.RightControl);

    [Fact]
    public void Windowsキーも単独押しで切り替える()
    {
        detector.TriggerKey = TriggerKey.FromVirtualKey(VirtualKeys.LeftWindows)!;
        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.LeftWindows));
        Assert.Equal(new InputDecision(ShouldToggle: true), detector.Handle(KeyboardInput.KeyUp(VirtualKeys.LeftWindows)));
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
    public void 無変換キーも押したときに切り替える()
    {
        Assert.Equal(
            new InputDecision(ShouldToggle: true, ShouldSuppress: true),
            detector.Handle(KeyboardInput.KeyDown(VirtualKeys.NonConvert)));
    }

    [Fact]
    public void 切り替えキーを変えると押し下げ中の状態は破棄される()
    {
        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.NonConvert));
        detector.TriggerKey = TriggerKey.FromVirtualKey(F13)!;
        // 古いキーを離した入力は、もう切り替えキーの入力ではないのでそのまま渡す
        Assert.Equal(InputDecision.PassThrough, detector.Handle(KeyboardInput.KeyUp(VirtualKeys.NonConvert)));
        Assert.True(detector.Handle(KeyboardInput.KeyDown(F13)).ShouldToggle);
    }

    [Fact]
    public void Resetで押し下げ中の状態を破棄する()
    {
        detector.Handle(KeyboardInput.KeyDown(VirtualKeys.NonConvert));
        detector.Reset();
        Assert.Equal(InputDecision.PassThrough, detector.Handle(KeyboardInput.KeyUp(VirtualKeys.NonConvert)));
    }
}
