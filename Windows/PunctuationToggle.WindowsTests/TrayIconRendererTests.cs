namespace PunctuationToggle.WindowsTests;

public class TrayIconRendererTests
{
    [Theory]
    [InlineData("、。", 16)]
    [InlineData("，．", 24)]
    // 起動したときから句読点の設定を読み込めない場合の、1文字のアイコン
    [InlineData("？", 32)]
    public void 指定した大きさのアイコンを描く(string text, int iconSize)
    {
        using var icon = TrayIconRenderer.Render(text, isTaskbarLight: true, iconSize);
        Assert.Equal(iconSize, icon.Width);
        Assert.Equal(iconSize, icon.Height);

        // 文字を描いた（透明でない画素がある）
        using var bitmap = icon.ToBitmap();
        var hasInk = false;
        for (var y = 0; y < bitmap.Height && !hasInk; y++)
        {
            for (var x = 0; x < bitmap.Width && !hasInk; x++)
            {
                hasInk = bitmap.GetPixel(x, y).A > 0;
            }
        }
        Assert.True(hasInk);
    }
}
