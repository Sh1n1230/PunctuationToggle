using Microsoft.Win32;

namespace PunctuationToggle;

/// <summary>
/// Microsoft IME の「句読点」設定をレジストリで読み書きする。
/// </summary>
/// <remarks>
/// IME の設定画面と同じく HKCU\Software\Microsoft\IME\15.0\IMEJP\MSIME の option1 に保存する
/// （ビットの意味は <see cref="MicrosoftImeOption1"/> を参照）。
/// IME は入力欄にフォーカスが入ったときに設定を読み直すため、
/// 書き換えただけでは入力中のアプリに反映されない（<see cref="FocusBouncer"/> で反映させる）。
/// この値は非公開のため、Microsoft IME のアップデートで変わる可能性がある。
/// </remarks>
internal static class MicrosoftImeSettings
{
    private const string KeyPath = @"Software\Microsoft\IME\15.0\IMEJP\MSIME";
    private const string Option1ValueName = "option1";

    public static PunctuationStyle PunctuationStyle
    {
        get => MicrosoftImeOption1.GetPunctuationStyle(ReadOption1());
        set
        {
            var option1 = MicrosoftImeOption1.WithPunctuationStyle(ReadOption1(), value);
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
            key.SetValue(Option1ValueName, option1, RegistryValueKind.DWord);
        }
    }

    private static int ReadOption1()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        return key?.GetValue(Option1ValueName) is int option1 ? option1 : MicrosoftImeOption1.DefaultValue;
    }
}
