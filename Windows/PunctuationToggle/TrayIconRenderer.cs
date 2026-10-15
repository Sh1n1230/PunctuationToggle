using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace PunctuationToggle;

/// <summary>句読点の2文字をそのまま描いたタスクトレイ用のアイコンを作る。</summary>
internal static class TrayIconRenderer
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

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetricsForDpi(int nIndex, uint dpi);

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
    /// <param name="isTaskbarLight">タスクバーがライトテーマなら黒、ダークテーマなら白で描く。</param>
    /// <param name="iconSize">
    /// アイコンの幅と高さ（ピクセル）。表示される大きさで描くと、Windows による縮小で句読点の点がにじまない。
    /// </param>
    public static Icon Render(string text, bool isTaskbarLight, int iconSize)
    {
        // 全角の句読点は字面（1文字分の正方形）の左下に小さく描かれるため、字面のまま並べると
        // 2文字がアイコンからはみ出したり、間が空きすぎて点が小さくなったりする。
        // 文字の輪郭そのものの範囲を測って間を詰めて並べ、アイコンいっぱいに拡大して中央に置く
        using var outline = CreateTightOutline(text);
        var outlineBounds = outline.GetBounds();

        var available = iconSize * (1 - (2 * PaddingRatio));
        var scale = Math.Min(available / outlineBounds.Width, available / outlineBounds.Height);
        using (var transform = new Matrix())
        {
            // 輪郭の中心を原点に移し、拡大・縮小してから、アイコンの中心に移す（後に書いたものから順に適用される）
            transform.Translate(iconSize / 2f, iconSize / 2f);
            transform.Scale(scale, scale);
            transform.Translate(
                -(outlineBounds.X + (outlineBounds.Width / 2)),
                -(outlineBounds.Y + (outlineBounds.Height / 2)));
            outline.Transform(transform);
        }

        using var bitmap = new Bitmap(iconSize, iconSize);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            using var brush = new SolidBrush(isTaskbarLight ? Color.Black : Color.White);
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
