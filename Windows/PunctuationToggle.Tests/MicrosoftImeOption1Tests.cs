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

    [Fact]
    public void 保存された値が無ければ既定値から書き換える()
    {
        Assert.Equal(MicrosoftImeOption1.DefaultValue, MicrosoftImeOption1.FromStoredValue(null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0x0011_0200)]
    public void DWORDで保存された値はそのまま使う(int storedValue)
    {
        Assert.Equal(storedValue, MicrosoftImeOption1.FromStoredValue(storedValue));
    }

    [Theory]
    [MemberData(nameof(NonDwordValues))]
    public void DWORD以外の形式で保存された値は書き換えに使わない(object storedValue)
    {
        var exception = Assert.Throws<InvalidDataException>(() => MicrosoftImeOption1.FromStoredValue(storedValue));
        Assert.Contains(storedValue.GetType().Name, exception.Message, StringComparison.Ordinal);
    }

    // レジストリの REG_SZ・REG_QWORD・REG_BINARY・REG_MULTI_SZ を RegistryKey.GetValue で読んだときの型
    public static TheoryData<object> NonDwordValues => new("1376768", 0x0015_0200L, BinaryValue, MultiStringValue);

    private static readonly byte[] BinaryValue = [0x00, 0x02, 0x15, 0x00];
    private static readonly string[] MultiStringValue = ["1376768"];

    public static TheoryData<PunctuationStyle> AllStyles => new(Enum.GetValues<PunctuationStyle>());
}
