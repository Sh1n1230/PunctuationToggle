using Microsoft.Win32;

namespace PunctuationToggle.WindowsTests;

public sealed class RegistryChangeWatcherTests : IDisposable
{
    // 通知はスレッドプールで非同期に届くので、届くまで待つ時間と、届かないことを確かめるために待つ時間
    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan NoNotificationWait = TimeSpan.FromMilliseconds(500);

    private readonly TestRegistryKey testKey = new();
    private readonly SemaphoreSlim changed = new(0);
    private readonly ManualResetEventSlim stopped = new();

    public RegistryChangeWatcherTests()
    {
        Registry.CurrentUser.CreateSubKey(testKey.Path).Dispose();
    }

    public void Dispose()
    {
        testKey.Dispose();
        changed.Dispose();
        stopped.Dispose();
    }

    [Fact]
    public void キーが無ければ監視しない()
    {
        Assert.Null(RegistryChangeWatcher.TryStart(testKey.Path + @"\無いキー", () => { }, () => { }));
    }

    [Fact]
    public void 値が変わるたびに知らせる()
    {
        using var watcher = Start();

        SetValue(1);
        Assert.True(changed.Wait(NotificationTimeout));
        // 通知のたびに登録し直すので、2回目の変更も知らされる
        SetValue(2);
        Assert.True(changed.Wait(NotificationTimeout));
        Assert.False(stopped.IsSet);
    }

    [Fact]
    public void キーが削除されたら監視が止まったことを知らせる()
    {
        using var watcher = Start();

        Registry.CurrentUser.DeleteSubKeyTree(testKey.Path);

        Assert.True(stopped.Wait(NotificationTimeout));
    }

    [Fact]
    public void Disposeの後は知らせない()
    {
        var watcher = Start();
        watcher.Dispose();

        SetValue(1);

        Assert.False(changed.Wait(NoNotificationWait));
    }

    private RegistryChangeWatcher Start()
    {
        var watcher = RegistryChangeWatcher.TryStart(testKey.Path, () => changed.Release(), stopped.Set);
        Assert.NotNull(watcher);
        return watcher;
    }

    private void SetValue(int value)
    {
        using var key = Registry.CurrentUser.OpenSubKey(testKey.Path, writable: true)!;
        key.SetValue("value", value, RegistryValueKind.DWord);
    }
}
