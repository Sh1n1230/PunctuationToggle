using Microsoft.Win32;

namespace PunctuationToggle.WindowsTests;

/// <summary>
/// テストごとに作り、終わったら消す、HKCU\Software\PunctuationToggle.Tests\&lt;一意な名前&gt; のキー。
/// 実際のアプリの設定・IME の設定・Run キーには書き込まないよう、テストはこのキーの下だけを使う。
/// </summary>
internal sealed class TestRegistryKey : IDisposable
{
    private const string RootPath = @"Software\PunctuationToggle.Tests";

    public TestRegistryKey()
    {
        Path = $@"{RootPath}\{Guid.NewGuid():N}";
    }

    /// <summary>HKCU からのパス（キーはまだ作っていない）。</summary>
    public string Path { get; }

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(Path, throwOnMissingSubKey: false);
        // ほかのテストのキーが残っていなければ、親のキーも消す
        using var root = Registry.CurrentUser.OpenSubKey(RootPath);
        if (root is { SubKeyCount: 0, ValueCount: 0 })
        {
            Registry.CurrentUser.DeleteSubKey(RootPath, throwOnMissingSubKey: false);
        }
    }
}
