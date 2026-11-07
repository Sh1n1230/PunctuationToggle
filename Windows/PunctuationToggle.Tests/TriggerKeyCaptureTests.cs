namespace PunctuationToggle.Tests;

// 両 OS に共通の読み取りは、testdata/trigger_key_vectors.json（TriggerKeyVectorTests）で確かめる。
// ここには Windows 版だけの扱いのテストを置く。

public class TriggerKeyCaptureTests
{
    private readonly TriggerKeyCapture capture = new();

    [Theory]
    [InlineData(VirtualKeys.Tab)]
    [InlineData(VirtualKeys.Enter)]
    [InlineData(VirtualKeys.Space)]
    public void ボタンの操作に使うキーは読み取らずに画面へ渡す(int virtualKey)
    {
        Assert.Equal(
            new CaptureResult(CaptureOutcome.Waiting, ShouldSuppress: false),
            capture.Handle(KeyboardInput.KeyDown(virtualKey)));
        Assert.Equal(
            new CaptureResult(CaptureOutcome.Waiting, ShouldSuppress: false),
            capture.Handle(KeyboardInput.KeyUp(virtualKey)));
    }

    [Fact]
    public void ShiftとTabでフォーカスを戻しても_Shiftを読み取らない()
    {
        capture.Handle(KeyboardInput.KeyDown(VirtualKeys.LeftShift));
        capture.Handle(KeyboardInput.KeyDown(VirtualKeys.Tab, otherModifiersHeld: true));
        capture.Handle(KeyboardInput.KeyUp(VirtualKeys.Tab));
        Assert.Equal(CaptureOutcome.Waiting, capture.Handle(KeyboardInput.KeyUp(VirtualKeys.LeftShift)).Outcome);
    }

    [Fact]
    public void Tabの後に押したキーは読み取る()
    {
        capture.Handle(KeyboardInput.KeyDown(VirtualKeys.Tab));
        capture.Handle(KeyboardInput.KeyUp(VirtualKeys.Tab));
        var result = capture.Handle(KeyboardInput.KeyDown(VirtualKeys.NonConvert));
        Assert.Equal(CaptureOutcome.Captured, result.Outcome);
        Assert.Equal(VirtualKeys.NonConvert, result.CapturedKey?.VirtualKey);
    }
}
