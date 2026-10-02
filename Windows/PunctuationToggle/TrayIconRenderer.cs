using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace PunctuationToggle;

/// <summary>句読点の2文字をそのまま描いたタスクトレイ用のアイコンを作る。</summary>
internal static class TrayIconRenderer
{
    private const int IconSize = 32;
    private const float FontSize = 20;

    // 全角の句読点は字面の左下に寄っているので、少し右上にずらして中央に見せる
    private static readonly RectangleF TextBounds = new(6, -4, IconSize, IconSize);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    /// <param name="text">描く文字。</param>
    /// <param name="isTaskbarLight">タスクバーがライトテーマなら黒、ダークテーマなら白で描く。</param>
    public static Icon Render(string text, bool isTaskbarLight)
    {
        using var bitmap = new Bitmap(IconSize, IconSize);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            graphics.Clear(Color.Transparent);

            using var font = new Font("Yu Gothic UI", FontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            using var brush = new SolidBrush(isTaskbarLight ? Color.Black : Color.White);
            graphics.DrawString(text, font, brush, TextBounds, format);
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
