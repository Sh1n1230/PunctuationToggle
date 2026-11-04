namespace PunctuationToggle.Tests;

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
    [InlineData(PunctuationStyle.ToutenKuten, "読点と句点（、。）")]
    [InlineData(PunctuationStyle.CommaKuten, "コンマと句点（，。）")]
    [InlineData(PunctuationStyle.ToutenPeriod, "読点とピリオド（、．）")]
    [InlineData(PunctuationStyle.CommaPeriod, "コンマとピリオド（，．）")]
    public void 読み上げる名前(PunctuationStyle style, string expected)
    {
        Assert.Equal(expected, style.SpokenName());
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
