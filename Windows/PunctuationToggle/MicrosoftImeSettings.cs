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
        // 読み込めない場合は、値が無い場合と同じく既定値で動いているとみなす。
        // IME も同じユーザーとして読むので、IME も既定値で動いていると考えられる。
        // 書き込み（set）は自分で option1 を読み直し、読み込めなければ例外にするので、ほかのビットを既定値で上書きすることはない
        get => TryReadPunctuationStyle()
            ?? MicrosoftImeOption1.GetPunctuationStyle(MicrosoftImeOption1.DefaultValue);
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
            var option1 = MicrosoftImeOption1.FromStoredValue(key.GetValue(Option1ValueName));
            key.SetValue(Option1ValueName, MicrosoftImeOption1.WithPunctuationStyle(option1, value), RegistryValueKind.DWord);
        }
    }

    /// <summary>
    /// 句読点の設定を読む。レジストリを読み込めない場合（アクセス権が変更されている場合など）は null。
    /// </summary>
    public static PunctuationStyle? TryReadPunctuationStyle()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return MicrosoftImeOption1.GetPunctuationStyle(
                MicrosoftImeOption1.FromStoredValue(key?.GetValue(Option1ValueName)));
        }
        // 表示に使うだけなので、想定外の形式でも既定値とみなす（書き込みでは書き換えない）
        catch (InvalidDataException)
        {
            return MicrosoftImeOption1.GetPunctuationStyle(MicrosoftImeOption1.DefaultValue);
        }
        catch (Exception exception) when (RegistryErrors.IsAccessFailure(exception))
        {
            return null;
        }
    }

    /// <summary>
    /// 句読点の設定（option1）が変わったときに知らせる。IME の設定画面で変えた場合も含む。
    /// キーがまだ無い（IME の設定を一度も変えていない）場合などは監視できず、null を返す。
    /// </summary>
    /// <param name="onChanged">スレッドプールのスレッドで呼ばれる。</param>
    public static RegistryChangeWatcher? WatchChanges(Action onChanged) =>
        RegistryChangeWatcher.TryStart(KeyPath, onChanged);
}
