using Microsoft.Win32;

namespace PunctuationToggle;

/// <summary>
/// Microsoft IME の「句読点」設定を読み書きする。
/// IME の設定画面と同じく HKCU\...\MSIME の option1 のビット16〜17 に保存されている。
/// IME は入力欄にフォーカスが入ったときに設定を読み直すため、
/// 書き換えただけでは反映されない（<see cref="FocusBouncer"/> で反映させる）。
/// </summary>
static class MicrosoftImePunctuation
{
    const string KeyPath = @"Software\Microsoft\IME\15.0\IMEJP\MSIME";
    const string ValueName = "option1";

    const int Shift = 16;
    const int Mask = 0b11 << Shift;

    /// 設定値（option1 のビット16〜17）
    ///   0: ，．  1: 、。  2: 、．  3: ，。
    public const int JapaneseStyle = 1;
    public const int FullWidthStyle = 0;

    // option1 が無い環境（IME の設定を一度も変更していない）での既定値
    const int DefaultOption1 = 0x00150200;

    public static int Current => (ReadOption1() & Mask) >> Shift;

    public static void Set(int style)
    {
        var option1 = (ReadOption1() & ~Mask) | (style << Shift);
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
        key.SetValue(ValueName, option1, RegistryValueKind.DWord);
    }

    static int ReadOption1()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        return key?.GetValue(ValueName) is int value ? value : DefaultOption1;
    }
}
