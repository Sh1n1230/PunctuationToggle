using Microsoft.Win32;

namespace PunctuationToggle;

/// <summary>
/// このアプリの設定をレジストリ（HKCU）に保存する。
/// </summary>
internal static class AppSettings
{
    private const string SettingsKeyPath = @"Software\PunctuationToggle";
    private const string TriggerKeyValueName = "TriggerKey";

    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "PunctuationToggle";

    /// <summary>
    /// 切り替えキー。保存されていない場合や、保存された値が使えない場合、読み込めない場合は初期設定のキー。
    /// </summary>
    public static TriggerKey TriggerKey
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(SettingsKeyPath);
                return key?.GetValue(TriggerKeyValueName) is int virtualKey
                    ? TriggerKey.FromVirtualKey(virtualKey) ?? TriggerKey.Default
                    : TriggerKey.Default;
            }
            // 起動時に読むため、読み込めなくても異常終了させず、保存されていない場合と同じく初期設定のキーで動かす
            catch (Exception exception) when (RegistryErrors.IsAccessFailure(exception))
            {
                return TriggerKey.Default;
            }
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(SettingsKeyPath);
            key.SetValue(TriggerKeyValueName, value.VirtualKey, RegistryValueKind.DWord);
        }
    }

    /// <summary>
    /// ログイン時に起動するか（HKCU の Run キーに登録する）。
    /// 別の場所の exe が登録されている場合や、読み込めない場合は false（オンにし直すと、今の場所の exe で登録し直す）。
    /// </summary>
    public static bool LaunchAtLogin
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
                return key?.GetValue(RunValueName) is string command
                    && LaunchAtLoginCommand.Launches(command, Application.ExecutablePath);
            }
            // メニューを開くたびに読むため、読み込めなくても異常終了させない
            catch (Exception exception) when (RegistryErrors.IsAccessFailure(exception))
            {
                return false;
            }
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (value)
            {
                key.SetValue(RunValueName, LaunchAtLoginCommand.For(Application.ExecutablePath));
            }
            else
            {
                key.DeleteValue(RunValueName, throwOnMissingValue: false);
            }
        }
    }

    /// <summary>タスクバーがライトテーマか。値が無い場合や読み込めない場合は false（ダークテーマとみなす）。</summary>
    public static bool IsTaskbarLight
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("SystemUsesLightTheme") is int value && value != 0;
            }
            // アイコンを描くたびに読むため、読み込めなくても異常終了させない
            catch (Exception exception) when (RegistryErrors.IsAccessFailure(exception))
            {
                return false;
            }
        }
    }
}
