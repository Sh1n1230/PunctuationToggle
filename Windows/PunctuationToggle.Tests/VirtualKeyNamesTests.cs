namespace PunctuationToggle.Tests;

public class VirtualKeyNamesTests
{
    [Theory]
    [InlineData(0x41, "A")]
    [InlineData(0x5A, "Z")]
    [InlineData(0x30, "0")]
    [InlineData(0x39, "9")]
    [InlineData(0x20, "スペース")]
    [InlineData(0x0D, "Enter")]
    [InlineData(0x14, "Caps Lock")]
    [InlineData(0x08, "Backspace")]
    [InlineData(0x60, "テンキーの 0")]
    [InlineData(0xF3, "半角/全角")]
    [InlineData(0xF4, "半角/全角")]
    public void よく押されるキーの名前(int virtualKey, string expected)
    {
        Assert.Equal(expected, VirtualKeyNames.For(virtualKey));
    }

    [Theory]
    [InlineData(0xBC)] // VK_OEM_COMMA（アプリ本体で Keys の名前を使う）
    [InlineData(0x00)]
    public void 表に無いキーはnull(int virtualKey)
    {
        Assert.Null(VirtualKeyNames.For(virtualKey));
    }
}
