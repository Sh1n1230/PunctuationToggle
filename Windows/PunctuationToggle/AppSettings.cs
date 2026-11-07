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

    // Windows の設定の「スタートアップ アプリ」で自動起動を止めた状態の保存先（非公開。StartupApproval を参照）
    private const string StartupApprovedKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    /// <summary>
    /// 切り替えキー。保存されていない場合や、保存された値が使えない場合、読み込めない場合は初期設定のキー。
    /// </summary>
    public static TriggerKey TriggerKey
    {
        get => ReadTriggerKey(SettingsKeyPath);
        set => WriteTriggerKey(SettingsKeyPath, value);
    }

    /// <summary>
    /// ログイン時に起動するか（HKCU の Run キーに登録する）。
    /// 別の場所の exe が登録されている場合や、読み込めない場合は false（オンにし直すと、今の場所の exe で登録し直す）。
    /// </summary>
    public static bool LaunchAtLogin
    {
        get => IsLaunchAtLoginRegistered(RunKeyPath, Application.ExecutablePath);
        set => SetLaunchAtLogin(RunKeyPath, Application.ExecutablePath, value);
    }

    /// <summary>
    /// 「ログイン時に起動」の状態。Run キーに登録されていても、Windows の設定の「スタートアップ アプリ」などで
    /// 無効にされていれば <see cref="LaunchAtLoginState.DisabledByWindows"/>。
    /// </summary>
    public static LaunchAtLoginState LaunchAtLoginState =>
        ReadLaunchAtLoginState(RunKeyPath, StartupApprovedKeyPath, Application.ExecutablePath);

    // 以下の keyPath を受け取るものは、テスト（PunctuationToggle.WindowsTests）で実際の設定や Run キーを変えずに確かめるため

    internal static TriggerKey ReadTriggerKey(string settingsKeyPath)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(settingsKeyPath);
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

    internal static void WriteTriggerKey(string settingsKeyPath, TriggerKey value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(settingsKeyPath);
        key.SetValue(TriggerKeyValueName, value.VirtualKey, RegistryValueKind.DWord);
    }

    internal static bool IsLaunchAtLoginRegistered(string runKeyPath, string executablePath)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(runKeyPath);
            return key?.GetValue(RunValueName) is string command
                && LaunchAtLoginCommand.Launches(command, executablePath);
        }
        // メニューを開くたびに読むため、読み込めなくても異常終了させない
        catch (Exception exception) when (RegistryErrors.IsAccessFailure(exception))
        {
            return false;
        }
    }

    internal static LaunchAtLoginState ReadLaunchAtLoginState(
        string runKeyPath, string startupApprovedKeyPath, string executablePath)
    {
        if (!IsLaunchAtLoginRegistered(runKeyPath, executablePath))
        {
            return LaunchAtLoginState.Off;
        }
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(startupApprovedKeyPath);
            return StartupApproval.IsDisabled(key?.GetValue(RunValueName) as byte[])
                ? LaunchAtLoginState.DisabledByWindows
                : LaunchAtLoginState.On;
        }
        // 非公開の値で、読めなくても「ログイン時に起動」の表示だけの問題なので、止められていないとみなす
        catch (Exception exception) when (RegistryErrors.IsAccessFailure(exception))
        {
            return LaunchAtLoginState.On;
        }
    }

    internal static void SetLaunchAtLogin(string runKeyPath, string executablePath, bool value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(runKeyPath);
        if (value)
        {
            key.SetValue(RunValueName, LaunchAtLoginCommand.For(executablePath));
        }
        else
        {
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
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
