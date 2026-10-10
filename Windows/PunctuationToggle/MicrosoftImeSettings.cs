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

    /// <exception cref="InvalidDataException">
    /// 設定時、option1 が DWORD 以外の形式で保存されている場合。
    /// 既定値から書き込むと IME のほかの設定を変えてしまうおそれがあるため、書き換えない。
    /// </exception>
    public static PunctuationStyle PunctuationStyle
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return MicrosoftImeOption1.GetPunctuationStyle(
                key?.GetValue(Option1ValueName) is int option1 ? option1 : MicrosoftImeOption1.DefaultValue);
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
            var option1 = key.GetValue(Option1ValueName) switch
            {
                // IME の設定を一度も変更していない環境では値が無く、IME は既定値で動いている
                null => MicrosoftImeOption1.DefaultValue,
                int dword => dword,
                _ => throw new InvalidDataException(
                    $"Microsoft IME の設定値 {Option1ValueName} が想定と異なる形式（{key.GetValueKind(Option1ValueName)}）で保存されています。"),
            };
            key.SetValue(Option1ValueName, MicrosoftImeOption1.WithPunctuationStyle(option1, value), RegistryValueKind.DWord);
        }
    }
}
