using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace PunctuationToggle;

/// <summary>句読点の2文字をそのまま描いたタスクトレイ用のアイコンを作る。</summary>
internal static class TrayIconRenderer
{
    // 文字の大きさと位置は、32×32 ピクセルで描くときの値を基準にして、アイコンの大きさに比例させる
    private const int BaseIconSize = 32;
    private const float BaseFontSize = 20;

    // 全角の句読点は字面の左下に寄っているので、少し右上にずらして中央に見せる
    private const float BaseTextOffsetX = 6;
    private const float BaseTextOffsetY = -4;

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
        var scale = (float)iconSize / BaseIconSize;
        var textBounds = new RectangleF(BaseTextOffsetX * scale, BaseTextOffsetY * scale, iconSize, iconSize);

        using var bitmap = new Bitmap(iconSize, iconSize);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            graphics.Clear(Color.Transparent);

            using var font = new Font("Yu Gothic UI", BaseFontSize * scale, FontStyle.Bold, GraphicsUnit.Pixel);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            using var brush = new SolidBrush(isTaskbarLight ? Color.Black : Color.White);
            graphics.DrawString(text, font, brush, textBounds, format);
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
}
