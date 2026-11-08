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
        using var icon = TrayIconRenderer.Render(text, Color.Black, iconSize);
        Assert.Equal(iconSize, icon.Width);
        Assert.Equal(iconSize, icon.Height);

        // 文字を描いた（透明でない画素がある）
        Assert.NotNull(MeasureInkBounds(icon));
    }

    // 面積を比べるので、画素の端数の影響が小さい大きさで描く
    private const int ComparisonIconSize = 256;

    // 面積の比の許容範囲（画素の端数とアンチエイリアスによる誤差の分）
    private const double AreaRatioTolerance = 0.05;

    [Fact]
    public void 句読点の組み合わせが違っても文字の範囲の面積がそろう()
    {
        var areas = Enum.GetValues<PunctuationStyle>()
            .Select(style =>
            {
                using var icon = TrayIconRenderer.Render(style.Symbols(), Color.Black, ComparisonIconSize);
                var inkBounds = MeasureInkBounds(icon)!.Value;
                return (double)inkBounds.Width * inkBounds.Height;
            })
            .ToList();
        Assert.InRange(areas.Min() / areas.Max(), 1 - AreaRatioTolerance, 1);
    }

    /// <summary>透明でない画素を囲む範囲。透明でない画素が無ければ null。</summary>
    private static Rectangle? MeasureInkBounds(Icon icon)
    {
        using var bitmap = icon.ToBitmap();
        int left = bitmap.Width, top = bitmap.Height, right = -1, bottom = -1;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).A == 0)
                {
                    continue;
                }
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }
        return right < 0 ? null : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    [Fact]
    public void コントラストテーマではテーマの文字色で描く()
    {
        Assert.Equal(SystemColors.WindowText, TrayIconRenderer.TextColorForTaskbar(isHighContrast: true, isTaskbarLight: true));
        Assert.Equal(SystemColors.WindowText, TrayIconRenderer.TextColorForTaskbar(isHighContrast: true, isTaskbarLight: false));
    }

    [Fact]
    public void 通常はタスクバーのライトとダークに合わせる()
    {
        Assert.Equal(Color.Black, TrayIconRenderer.TextColorForTaskbar(isHighContrast: false, isTaskbarLight: true));
        Assert.Equal(Color.White, TrayIconRenderer.TextColorForTaskbar(isHighContrast: false, isTaskbarLight: false));
    }
}
