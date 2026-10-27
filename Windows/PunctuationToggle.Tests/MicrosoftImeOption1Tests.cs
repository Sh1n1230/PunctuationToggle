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
