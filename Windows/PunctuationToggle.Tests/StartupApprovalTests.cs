namespace PunctuationToggle.Tests;

public class StartupApprovalTests
{
    [Fact]
    public void 値が無ければ有効()
    {
        Assert.False(StartupApproval.IsDisabled(null));
        Assert.False(StartupApproval.IsDisabled([]));
    }

    [Theory]
    // 設定アプリやタスク マネージャーで有効にした値（先頭のバイトが偶数）
    [InlineData(new byte[] { 0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, false)]
    [InlineData(new byte[] { 0x06, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, false)]
    // 無効にした値（先頭のバイトが奇数。2 バイト目以降は無効にした日時）
    [InlineData(new byte[] { 0x03, 0, 0, 0, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x01 }, true)]
    [InlineData(new byte[] { 0x07, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, true)]
    public void 先頭のバイトの最下位ビットで判定する(byte[] value, bool expected)
    {
        Assert.Equal(expected, StartupApproval.IsDisabled(value));
    }
}
