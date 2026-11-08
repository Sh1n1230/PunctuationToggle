using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace PunctuationToggle;

/// <summary>句読点の2文字をそのまま描いたタスクトレイ用のアイコンを作る。</summary>
internal static partial class TrayIconRenderer
{
    // 文字の輪郭を作るときのフォントサイズ。輪郭は後でアイコンの大きさに合わせて拡大・縮小するので、精度が足りればよい
    private const float OutlineFontSize = 100;

    // アイコンの縁と文字の間に空ける幅（アイコンの幅に対する割合）
    private const float PaddingRatio = 0.06f;

    // 並べる文字の間に空ける幅（文字の輪郭全体の高さに対する割合）
    private const float CharacterGapRatio = 0.5f;

    // GetSystemMetricsForDpi に渡す、小さいアイコン（タスクトレイのアイコン）の幅
    private const int SM_CXSMICON = 49;

    // MonitorFromPoint で、指定した点がどのモニターにも無いときにメインモニターを返させる
    private const uint MONITOR_DEFAULTTOPRIMARY = 1;

    // GetDpiForMonitor で、表示倍率を反映した DPI を求める
    private const int MDT_EFFECTIVE_DPI = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr hIcon);

    [LibraryImport("user32.dll")]
    private static partial IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    // Windows 8.1 以降
    [LibraryImport("shcore.dll")]
    private static partial int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    // Windows 10 1607 以降（アプリが必要とする最低の版を決めている API。PunctuationToggle.csproj の SupportedOSPlatformVersion）
    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetricsForDpi(int nIndex, uint dpi);

    /// <summary>
    /// タスクトレイに表示されるアイコンの大きさ（ピクセル）。表示倍率 100% で 16、150% で 24 になる。
    /// </summary>
    /// <remarks>
    /// タスクトレイはメインモニターのタスクバーにあるため、メインモニターの表示倍率で求める。
    /// アプリは PerMonitorV2 で動くので、SystemInformation.SmallIconSize（ログオン時の倍率の値）ではなく、今の倍率で求める。
    /// </remarks>
    public static int CurrentIconSize
    {
        get
        {
            var monitor = MonitorFromPoint(new POINT(), MONITOR_DEFAULTTOPRIMARY);
            if (GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out var dpi, out _) != 0)
            {
                return SystemInformation.SmallIconSize.Width;
            }
            var size = GetSystemMetricsForDpi(SM_CXSMICON, dpi);
            return size > 0 ? size : SystemInformation.SmallIconSize.Width;
        }
    }

    /// <param name="text">描く文字。</param>
    /// <param name="textColor">文字の色（タスクバーの背景と見分けられる色。<see cref="TextColorForTaskbar"/>）。</param>
    /// <param name="iconSize">
    /// アイコンの幅と高さ（ピクセル）。表示される大きさで描くと、Windows による縮小で句読点の点がにじまない。
    /// </param>
    public static Icon Render(string text, Color textColor, int iconSize)
    {
        // 全角の句読点は字面（1文字分の正方形）の左下に小さく描かれるため、字面のまま並べると
        // 2文字がアイコンからはみ出したり、間が空きすぎて点が小さくなったりする。
        // 文字の輪郭そのものの範囲を測って間を詰めて並べ、拡大してアイコンの中央に置く
        using var outline = CreateTightOutline(text);
        var outlineBounds = outline.GetBounds();
        var layout = MeasureLayout(outlineBounds, iconSize * (1 - (2 * PaddingRatio)));

        using (var transform = new Matrix())
        {
            // 輪郭の中心を原点に移し、拡大・縮小してから、アイコンの中心に移す（後に書いたものから順に適用される）
            transform.Translate(iconSize / 2f, iconSize / 2f);
            transform.Scale(layout.Scale, layout.Scale);
            transform.Translate(-(outlineBounds.X + (outlineBounds.Width / 2)), -layout.CenterY);
            outline.Transform(transform);
        }

        using var bitmap = new Bitmap(iconSize, iconSize);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            using var brush = new SolidBrush(textColor);
            graphics.FillPath(brush, outline);
        }

        // GetHicon で作ったハンドルは Icon が解放しないので、複製してから自分で解放する
        var iconHandle = bitmap.GetHicon();
        try
        {
            using var temporaryIcon = Icon.FromHandle(iconHandle);
            return (Icon)temporaryIcon.Clone();
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
    }

    /// <summary>タスクバーに表示するアイコンの文字の色。</summary>
    /// <param name="isHighContrast">コントラスト テーマ（ハイコントラスト）を使っているか。</param>
    /// <param name="isTaskbarLight">タスクバーがライトテーマか（コントラスト テーマでないときに使う）。</param>
    /// <remarks>
    /// コントラスト テーマでは、タスクバーの背景と文字はテーマの配色（「砂漠」は明るい背景、「水生」「夕暮れ」は暗い背景など）になり、
    /// ライト・ダークの設定（SystemUsesLightTheme）とは一致しないため、テーマの文字色で描く。
    /// </remarks>
    public static Color TextColorForTaskbar(bool isHighContrast, bool isTaskbarLight) =>
        isHighContrast ? SystemColors.WindowText
        : isTaskbarLight ? Color.Black
        : Color.White;

    /// <summary>輪郭を描くときの倍率と、アイコンの縦の中心に置く輪郭上の位置。</summary>
    private readonly record struct Layout(float Scale, float CenterY);

    /// <param name="outlineBounds">描く文字の輪郭（<see cref="CreateTightOutline"/>）の範囲。</param>
    /// <param name="available">アイコンの中で文字を描ける幅と高さ（ピクセル）。</param>
    /// <remarks>
    /// 句読点の組み合わせ（「、。」「，．」など）ごとにアイコンいっぱいに拡大すると、字形の小さい「，．」が
    /// 「、。」の約1.5倍の倍率で描かれ、切り替えたときに大きさが変わって見える（#322）。
    /// そのため、輪郭の範囲の面積が、どの組み合わせでも同じになる倍率で描く。基準の面積は、すべての組み合わせを
    /// 同じ倍率でアイコンに収めたときの、最も広い組み合わせ（「、。」）の面積。面積をそろえても収まらない文字（「？」など）は、
    /// 収まる大きさに縮める。縦の位置も、すべての組み合わせを重ねた範囲の中央をアイコンの中央に置き、句読点の高さをそろえる。
    /// </remarks>
    private static Layout MeasureLayout(RectangleF outlineBounds, float available)
    {
        var referenceBounds = Enum.GetValues<PunctuationStyle>()
            .Select(style =>
            {
                using var referenceOutline = CreateTightOutline(style.Symbols());
                return referenceOutline.GetBounds();
            })
            .ToList();
        var top = Math.Min(referenceBounds.Min(bounds => bounds.Top), outlineBounds.Top);
        var bottom = Math.Max(referenceBounds.Max(bounds => bounds.Bottom), outlineBounds.Bottom);

        var commonScale = Math.Min(
            available / referenceBounds.Max(bounds => bounds.Width),
            available / (referenceBounds.Max(bounds => bounds.Bottom) - referenceBounds.Min(bounds => bounds.Top)));
        var referenceArea = referenceBounds.Max(bounds => bounds.Width * bounds.Height) * commonScale * commonScale;
        var areaScale = MathF.Sqrt(referenceArea / (outlineBounds.Width * outlineBounds.Height));
        var fittingScale = Math.Min(available / outlineBounds.Width, available / (bottom - top));
        return new Layout(Math.Min(areaScale, fittingScale), (top + bottom) / 2);
    }

    /// <summary>
    /// 文字ごとの輪郭を、縦の位置（ベースラインからの高さ）は保ったまま、横の間を詰めて並べた輪郭を作る。
    /// </summary>
    private static GraphicsPath CreateTightOutline(string text)
    {
        using var font = new Font("Yu Gothic UI", OutlineFontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        var characterOutlines = new List<GraphicsPath>();
        try
        {
            foreach (var character in text)
            {
                var characterOutline = new GraphicsPath();
                characterOutlines.Add(characterOutline);
                characterOutline.AddString(
                    character.ToString(), font.FontFamily, (int)font.Style, font.Size, PointF.Empty,
                    StringFormat.GenericTypographic);
            }

            var bounds = characterOutlines.Select(characterOutline => characterOutline.GetBounds()).ToList();
            var height = bounds.Max(bound => bound.Bottom) - bounds.Min(bound => bound.Top);
            var gap = height * CharacterGapRatio;

            var outline = new GraphicsPath();
            var left = 0f;
            for (var index = 0; index < characterOutlines.Count; index++)
            {
                using var shift = new Matrix();
                shift.Translate(left - bounds[index].Left, 0);
                characterOutlines[index].Transform(shift);
                outline.AddPath(characterOutlines[index], connect: false);
                left += bounds[index].Width + gap;
            }
            return outline;
        }
        finally
        {
            foreach (var characterOutline in characterOutlines)
            {
                characterOutline.Dispose();
            }
        }
    }
}
