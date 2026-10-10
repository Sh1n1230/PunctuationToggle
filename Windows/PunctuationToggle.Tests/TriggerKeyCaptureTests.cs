namespace PunctuationToggle.Tests;

public class TriggerKeyCaptureTests
{
    private const int LetterA = 0x41;

    private readonly TriggerKeyCapture capture = new();

    [Fact]
    public void 修飾キーは単独で押して離したときに読み取る()
    {
        Assert.Equal(
            new CaptureResult(CaptureOutcome.Waiting, ShouldSuppress: false),
            capture.Handle(KeyboardInput.KeyDown(VirtualKeys.RightAlt)));
        capture.Handle(KeyboardInput.KeyDown(VirtualKeys.RightAlt, isRepeat: true));

        var result = capture.Handle(KeyboardInput.KeyUp(VirtualKeys.RightAlt));
        Assert.Equal(CaptureOutcome.Captured, result.Outcome);
        Assert.Equal(VirtualKeys.RightAlt, result.CapturedKey?.VirtualKey);
        Assert.False(result.ShouldSuppress);
    }

    [Fact]
    public void 修飾キーの組み合わせは読み取らない()
    {
        capture.Handle(KeyboardInput.KeyDown(VirtualKeys.LeftShift));
        capture.Handle(KeyboardInput.KeyDown(VirtualKeys.RightControl, otherModifiersHeld: true));
        Assert.Equal(CaptureOutcome.Waiting, capture.Handle(KeyboardInput.KeyUp(VirtualKeys.RightControl)).Outcome);
        Assert.Equal(CaptureOutcome.Waiting, capture.Handle(KeyboardInput.KeyUp(VirtualKeys.LeftShift)).Outcome);
    }

    [Fact]
    public void 修飾キーを押したままクリックした場合は読み取らない()
    {
        capture.Handle(KeyboardInput.KeyDown(VirtualKeys.RightControl));
        capture.Handle(KeyboardInput.Mouse);
        Assert.Equal(CaptureOutcome.Waiting, capture.Handle(KeyboardInput.KeyUp(VirtualKeys.RightControl)).Outcome);
    }

    [Fact]
    public void 修飾キー以外は押したときに読み取り押し下げと離したときの入力は渡さない()
    {
        var result = capture.Handle(KeyboardInput.KeyDown(VirtualKeys.Convert));
        Assert.Equal(CaptureOutcome.Captured, result.Outcome);
        Assert.Equal(VirtualKeys.Convert, result.CapturedKey?.VirtualKey);
        Assert.True(result.ShouldSuppress);
        Assert.True(capture.Handle(KeyboardInput.KeyUp(VirtualKeys.Convert)).ShouldSuppress);
    }

    [Fact]
    public void 文字キーは使えないキーとして扱い入力は渡さない()
    {
        var result = capture.Handle(KeyboardInput.KeyDown(LetterA));
        Assert.Equal(new CaptureResult(CaptureOutcome.Rejected, ShouldSuppress: true, RejectedVirtualKey: LetterA), result);
        Assert.True(capture.Handle(KeyboardInput.KeyUp(LetterA)).ShouldSuppress);
    }

    [Fact]
    public void Escでキャンセルする()
    {
        Assert.Equal(
            new CaptureResult(CaptureOutcome.Cancelled, ShouldSuppress: true),
            capture.Handle(KeyboardInput.KeyDown(VirtualKeys.Escape)));
    }

    [Fact]
    public void 押し下げを渡したキーの離したときの入力は渡す()
    {
        Assert.False(capture.Handle(KeyboardInput.KeyUp(LetterA)).ShouldSuppress);
    }

    [Fact]
    public void 設定画面が前面にない間はキーを読み取らず入力も渡す()
    {
        var waiting = new CaptureResult(CaptureOutcome.Waiting, ShouldSuppress: false);
        Assert.Equal(waiting, capture.HandleWhileInactive(KeyboardInput.KeyDown(LetterA)));
        Assert.Equal(waiting, capture.HandleWhileInactive(KeyboardInput.KeyUp(LetterA)));
        Assert.Equal(waiting, capture.HandleWhileInactive(KeyboardInput.KeyDown(VirtualKeys.Convert)));
    }

    [Fact]
    public void 設定画面が前面にない間に押し始めた修飾キーは前面に戻ってから離しても読み取らない()
    {
        capture.HandleWhileInactive(KeyboardInput.KeyDown(VirtualKeys.RightControl));
        Assert.Equal(CaptureOutcome.Waiting, capture.Handle(KeyboardInput.KeyUp(VirtualKeys.RightControl)).Outcome);
    }

    [Fact]
    public void 前面にあるときに渡さなかったキーは前面でなくなってから離しても渡さない()
    {
        capture.Handle(KeyboardInput.KeyDown(LetterA));
        Assert.True(capture.HandleWhileInactive(KeyboardInput.KeyUp(LetterA)).ShouldSuppress);
    }
}
