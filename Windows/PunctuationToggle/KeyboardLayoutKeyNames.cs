using System.Runtime.InteropServices;

namespace PunctuationToggle;

/// <summary>
/// 今のキーボード配列で、キーに印字された文字に近い名前を求める（GetKeyNameText）。
/// </summary>
/// <remarks>
/// System.Windows.Forms.Keys の名前（Oemcomma など）は US 配列を前提にした英語の識別子で、日本語キーボードの
/// 「、」のキーが「Oemcomma」と示されるなど分かりにくいため、記号のキーはこちらの名前を使う。
/// https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-getkeynametextw
/// </remarks>
internal static partial class KeyboardLayoutKeyNames
{
    // 仮想キーコードから、拡張キーの印（上位バイトの 0xE0）付きのスキャンコードを求める
    private const uint MAPVK_VK_TO_VSC_EX = 4;
    private const int ExtendedScanCodePrefix = 0xE0;

    // GetKeyNameText の lParam: ビット16〜23 がスキャンコード、ビット24 が拡張キーの印
    private const int ScanCodeShift = 16;
    private const int ExtendedKeyBit = 1 << 24;

    private const int MaxNameLength = 64;

    [LibraryImport("user32.dll", EntryPoint = "MapVirtualKeyW")]
    private static partial uint MapVirtualKey(uint uCode, uint uMapType);

    [LibraryImport("user32.dll", EntryPoint = "GetKeyNameTextW")]
    private static unsafe partial int GetKeyNameText(int lParam, char* lpString, int cchSize);

    /// <summary>キーの名前。求められない場合は null。</summary>
    public static unsafe string? For(int virtualKey)
    {
        var scanCode = MapVirtualKey((uint)virtualKey, MAPVK_VK_TO_VSC_EX);
        if (scanCode == 0)
        {
            return null;
        }
        var lParam = (int)((scanCode & 0xFF) << ScanCodeShift);
        if ((scanCode >> 8) == ExtendedScanCodePrefix)
        {
            lParam |= ExtendedKeyBit;
        }

        var buffer = stackalloc char[MaxNameLength];
        var length = GetKeyNameText(lParam, buffer, MaxNameLength);
        return length > 0 ? new string(buffer, 0, length) : null;
    }
}
