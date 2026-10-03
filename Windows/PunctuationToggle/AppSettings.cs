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

    /// <summary>切り替えキー。保存されていない場合や、保存された値が使えない場合は初期設定のキー。</summary>
    public static TriggerKey TriggerKey
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(SettingsKeyPath);
            return key?.GetValue(TriggerKeyValueName) is int virtualKey
                ? TriggerKey.FromVirtualKey(virtualKey) ?? TriggerKey.Default
                : TriggerKey.Default;
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(SettingsKeyPath);
            key.SetValue(TriggerKeyValueName, value.VirtualKey, RegistryValueKind.DWord);
        }
    }

    /// <summary>ログイン時に起動するか（HKCU の Run キーに登録する）。</summary>
    public static bool LaunchAtLogin
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(RunValueName) is string;
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (value)
            {
                key.SetValue(RunValueName, $"\"{Application.ExecutablePath}\"");
            }
            else
            {
                key.DeleteValue(RunValueName, throwOnMissingValue: false);
            }
        }
    }

    /// <summary>タスクバーがライトテーマか。</summary>
    public static bool IsTaskbarLight
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("SystemUsesLightTheme") is int value && value != 0;
        }
    }
}
