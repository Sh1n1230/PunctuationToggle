using Microsoft.Win32;

namespace PunctuationToggle.WindowsTests;

public sealed class AppSettingsTests : IDisposable
{
    private const string ExecutablePath = @"C:\Users\Test\AppData\Local\Programs\PunctuationToggle\PunctuationToggle.exe";

    private readonly TestRegistryKey testKey = new();

    public void Dispose() => testKey.Dispose();

    [Fact]
    public void 保存されていない切り替えキーは初期設定のキー()
    {
        Assert.Equal(TriggerKey.Default, AppSettings.ReadTriggerKey(testKey.Path));
    }

    [Fact]
    public void 保存した切り替えキーを読み込める()
    {
        var key = TriggerKey.FromVirtualKey(VirtualKeys.NonConvert)!;
        AppSettings.WriteTriggerKey(testKey.Path, key);
        Assert.Equal(key, AppSettings.ReadTriggerKey(testKey.Path));
    }

    [Fact]
    public void 使えない値が保存されていれば初期設定のキー()
    {
        using (var key = Registry.CurrentUser.CreateSubKey(testKey.Path))
        {
            key.SetValue("TriggerKey", (int)'A', RegistryValueKind.DWord);
        }
        Assert.Equal(TriggerKey.Default, AppSettings.ReadTriggerKey(testKey.Path));
    }

    [Fact]
    public void ログイン時に起動の登録と解除()
    {
        Assert.False(AppSettings.IsLaunchAtLoginRegistered(testKey.Path, ExecutablePath));

        AppSettings.SetLaunchAtLogin(testKey.Path, ExecutablePath, value: true);
        Assert.True(AppSettings.IsLaunchAtLoginRegistered(testKey.Path, ExecutablePath));
        // 別の場所の exe が登録されている場合はオフとみなす
        Assert.False(AppSettings.IsLaunchAtLoginRegistered(testKey.Path, @"D:\PunctuationToggle.exe"));

        AppSettings.SetLaunchAtLogin(testKey.Path, ExecutablePath, value: false);
        Assert.False(AppSettings.IsLaunchAtLoginRegistered(testKey.Path, ExecutablePath));
        // 登録が無いときに解除しても失敗しない
        AppSettings.SetLaunchAtLogin(testKey.Path, ExecutablePath, value: false);
    }
}

public sealed class MicrosoftImeSettingsTests : IDisposable
{
    private readonly TestRegistryKey testKey = new();

    public void Dispose() => testKey.Dispose();

    [Fact]
    public void 句読点のビットだけを書き換え_ほかのビットは保つ()
    {
        // 句読点（ビット16〜17）以外のビットも立てておく
        const int otherBits = 0x0000_0200 | 0x0040_0000;
        const int toutenKuten = 1 << 16;
        using (var key = Registry.CurrentUser.CreateSubKey(testKey.Path))
        {
            key.SetValue("option1", otherBits | toutenKuten, RegistryValueKind.DWord);
        }

        MicrosoftImeSettings.WritePunctuationStyle(testKey.Path, PunctuationStyle.CommaPeriod);

        using (var key = Registry.CurrentUser.OpenSubKey(testKey.Path)!)
        {
            Assert.Equal(otherBits, (int)key.GetValue("option1")!);
        }
        Assert.Equal(PunctuationStyle.CommaPeriod, MicrosoftImeSettings.TryReadPunctuationStyle(testKey.Path));
    }

    [Fact]
    public void DWORD以外の形式で保存されていれば書き換えない()
    {
        using (var key = Registry.CurrentUser.CreateSubKey(testKey.Path))
        {
            key.SetValue("option1", "文字列", RegistryValueKind.String);
        }

        Assert.Throws<InvalidDataException>(
            () => MicrosoftImeSettings.WritePunctuationStyle(testKey.Path, PunctuationStyle.CommaPeriod));
        using var stored = Registry.CurrentUser.OpenSubKey(testKey.Path)!;
        Assert.Equal("文字列", stored.GetValue("option1"));
    }

    [Fact]
    public void キーが無ければ既定値の句読点として読む()
    {
        Assert.Equal(PunctuationStyle.ToutenKuten, MicrosoftImeSettings.TryReadPunctuationStyle(testKey.Path));
    }
}
