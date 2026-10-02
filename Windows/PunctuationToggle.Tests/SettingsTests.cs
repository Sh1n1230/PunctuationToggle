namespace PunctuationToggle.Tests;

public class MicrosoftImeOption1Tests
{
    [Fact]
    public void 既定値の句読点は読点と句点()
    {
        Assert.Equal(PunctuationStyle.ToutenKuten, MicrosoftImeOption1.GetPunctuationStyle(MicrosoftImeOption1.DefaultValue));
    }

    [Theory]
    [InlineData(0x0000_0000, PunctuationStyle.CommaPeriod)]
    [InlineData(0x0001_0000, PunctuationStyle.ToutenKuten)]
    [InlineData(0x0002_0000, PunctuationStyle.ToutenPeriod)]
    [InlineData(0x0003_0000, PunctuationStyle.CommaKuten)]
    [InlineData(unchecked((int)0xFFFC_FFFF), PunctuationStyle.CommaPeriod)]
    public void ビット16から17を句読点として読む(int option1, PunctuationStyle expected)
    {
        Assert.Equal(expected, MicrosoftImeOption1.GetPunctuationStyle(option1));
    }

    [Theory]
    [MemberData(nameof(AllStyles))]
    public void 書き込んだ句読点を読み戻せてほかのビットは変わらない(PunctuationStyle style)
    {
        int[] originals = [MicrosoftImeOption1.DefaultValue, 0, -1, 0x1234_5678];
        foreach (var original in originals)
        {
            var updated = MicrosoftImeOption1.WithPunctuationStyle(original, style);
            Assert.Equal(style, MicrosoftImeOption1.GetPunctuationStyle(updated));
            Assert.Equal(original & ~0x0003_0000, updated & ~0x0003_0000);
        }
    }

    public static TheoryData<PunctuationStyle> AllStyles => new(Enum.GetValues<PunctuationStyle>());
}

public class PunctuationStyleTests
{
    [Theory]
    [InlineData(PunctuationStyle.ToutenKuten, "、。")]
    [InlineData(PunctuationStyle.CommaKuten, "，。")]
    [InlineData(PunctuationStyle.ToutenPeriod, "、．")]
    [InlineData(PunctuationStyle.CommaPeriod, "，．")]
    public void 表示する文字(PunctuationStyle style, string expected)
    {
        Assert.Equal(expected, style.Symbols());
    }

    [Theory]
    [InlineData(PunctuationStyle.ToutenKuten, PunctuationStyle.CommaPeriod)]
    [InlineData(PunctuationStyle.CommaPeriod, PunctuationStyle.ToutenKuten)]
    [InlineData(PunctuationStyle.CommaKuten, PunctuationStyle.CommaPeriod)]
    [InlineData(PunctuationStyle.ToutenPeriod, PunctuationStyle.CommaPeriod)]
    public void 切り替え先(PunctuationStyle style, PunctuationStyle expected)
    {
        Assert.Equal(expected, style.Toggled());
    }
}

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

    [Fact]
    public void 操作の説明()
    {
        Assert.Equal("右Ctrlを単独で押すと切り替え", TriggerKey.RightControl.UsageDescription);
        Assert.Equal("無変換を押すと切り替え", TriggerKey.FromVirtualKey(VirtualKeys.NonConvert)!.UsageDescription);
    }
}
