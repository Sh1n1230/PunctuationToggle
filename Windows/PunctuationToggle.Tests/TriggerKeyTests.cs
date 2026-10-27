namespace PunctuationToggle.Tests;

public class TriggerKeyTests
{
    [Fact]
    public void 初期設定は右Ctrl()
    {
        Assert.Equal(VirtualKeys.RightControl, TriggerKey.Default.VirtualKey);
        Assert.Equal("右Ctrl", TriggerKey.Default.DisplayName);
        Assert.True(TriggerKey.Default.IsModifier);
    }

    [Theory]
    [InlineData(0x41)] // A
    [InlineData(0x20)] // Space
    [InlineData(0x0D)] // Enter
    [InlineData(VirtualKeys.Escape)]
    [InlineData(0x14)] // Caps Lock
    public void 文字キーやEscは切り替えキーにできない(int virtualKey)
    {
        Assert.Null(TriggerKey.FromVirtualKey(virtualKey));
    }

    [Fact]
    public void ファンクションキーはF1からF24の名前になる()
    {
        var names = Enumerable.Range(VirtualKeys.F1, VirtualKeys.FunctionKeyCount)
            .Select(virtualKey => TriggerKey.FromVirtualKey(virtualKey)?.DisplayName);
        Assert.Equal(Enumerable.Range(1, 24).Select(number => $"F{number}"), names);
    }

    [Fact]
    public void 修飾キーかどうかが仮想キーの一覧と一致する()
    {
        foreach (var key in TriggerKey.SupportedKeys.Values)
        {
            Assert.Equal(VirtualKeys.IsModifier(key.VirtualKey), key.IsModifier);
        }
        Assert.All(VirtualKeys.Modifiers, modifier => Assert.NotNull(TriggerKey.FromVirtualKey(modifier)));
    }

    [Theory]
    [InlineData(VirtualKeys.Convert)]
    [InlineData(VirtualKeys.NonConvert)]
    [InlineData(VirtualKeys.Applications)]
    [InlineData(VirtualKeys.Pause)]
    [InlineData(VirtualKeys.ScrollLock)]
    [InlineData(VirtualKeys.F1)]
    [InlineData(VirtualKeys.F1 + VirtualKeys.FunctionKeyCount - 1)]
    public void 修飾キー以外のキー(int virtualKey)
    {
        var key = TriggerKey.FromVirtualKey(virtualKey);
        Assert.NotNull(key);
        Assert.False(key.IsModifier);
        Assert.Equal(virtualKey, key.VirtualKey);
    }

    // README の「切り替えキーの変更」の表と、設定画面の案内（TriggerKeyRecorderForm）に書いた名前
    [Theory]
    [InlineData(VirtualKeys.Convert, "変換")]
    [InlineData(VirtualKeys.NonConvert, "無変換")]
    [InlineData(VirtualKeys.Applications, "アプリケーション")]
    [InlineData(VirtualKeys.Pause, "Pause")]
    [InlineData(VirtualKeys.ScrollLock, "Scroll Lock")]
    public void 変換や無変換などのキーの表示名(int virtualKey, string expectedName)
    {
        var key = TriggerKey.FromVirtualKey(virtualKey);
        Assert.NotNull(key);
        Assert.Equal(expectedName, key.DisplayName);
        Assert.False(key.IsModifier);
    }

    [Fact]
    public void 選べるキーは修飾キー8個とそれ以外の5個とF1からF24()
    {
        Assert.Equal(VirtualKeys.Modifiers.Count + 5 + VirtualKeys.FunctionKeyCount, TriggerKey.SupportedKeys.Count);
        // README の表と食い違わないよう、数そのものでも確かめる（8 + 5 + 24）
        Assert.Equal(37, TriggerKey.SupportedKeys.Count);
    }

    [Theory]
    [InlineData(0x10)] // VK_SHIFT（左右を区別しない）
    [InlineData(0x11)] // VK_CONTROL
    [InlineData(0x12)] // VK_MENU
    public void 左右を区別しない修飾キーは選べない(int virtualKey)
    {
        // 低レベルフックは左右を区別した仮想キーコードを渡すので、区別しないコードでは判定できない
        Assert.Null(TriggerKey.FromVirtualKey(virtualKey));
        Assert.False(VirtualKeys.IsModifier(virtualKey));
    }

    [Fact]
    public void 同じ仮想キーコードのキーは等しい()
    {
        Assert.Equal(TriggerKey.RightControl, TriggerKey.FromVirtualKey(VirtualKeys.RightControl));
        Assert.NotEqual(TriggerKey.RightControl, TriggerKey.FromVirtualKey(VirtualKeys.LeftControl));
    }

    [Fact]
    public void 操作の説明()
    {
        Assert.Equal("右Ctrlを単独で押すと切り替え", TriggerKey.RightControl.UsageDescription);
        Assert.Equal("無変換を押すと切り替え", TriggerKey.FromVirtualKey(VirtualKeys.NonConvert)!.UsageDescription);
    }
}
