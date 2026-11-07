using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace PunctuationToggle;

/// <summary>
/// HKCU のレジストリキーの値が変わったときに知らせる（RegNotifyChangeKeyValue）。
/// 一定の間隔で読み直す代わりに使い、何も変わらない間にプロセスが起きないようにする。
/// </summary>
/// <remarks>
/// 通知はスレッドプールのスレッドで呼ばれる。UI を更新する場合は、呼ばれた側で UI スレッドに移ること。
/// <see cref="Dispose"/> は実行中の通知が終わるのを待つので、<see cref="Dispose"/> から戻った後に通知が呼ばれることはない。
/// 通知のスレッドの中から <see cref="Dispose"/> を呼ぶと終わらなくなるため、呼ぶ場合はほかのスレッドに移ってから呼ぶこと。
/// https://learn.microsoft.com/windows/win32/api/winreg/nf-winreg-regnotifychangekeyvalue
/// </remarks>
internal sealed partial class RegistryChangeWatcher : IDisposable
{
    // 値の追加・削除・変更を通知させる
    private const uint REG_NOTIFY_CHANGE_LAST_SET = 0x00000004;

    // 登録したスレッドが終わっても通知を続ける（Windows 8 以降。それより前の Windows では登録に失敗する）。
    // 通知を受けたスレッドプールのスレッドで登録し直すため、このフラグが無いとそのスレッドの終了で通知が止まる
    private const uint REG_NOTIFY_THREAD_AGNOSTIC = 0x10000000;

    private const int ERROR_SUCCESS = 0;

    [LibraryImport("advapi32.dll")]
    private static partial int RegNotifyChangeKeyValue(
        SafeRegistryHandle hKey,
        [MarshalAs(UnmanagedType.Bool)] bool bWatchSubtree,
        uint dwNotifyFilter,
        SafeWaitHandle hEvent,
        [MarshalAs(UnmanagedType.Bool)] bool fAsynchronous);

    private readonly RegistryKey key;
    private readonly Action onChanged;
    private readonly Action onStopped;
    private readonly AutoResetEvent changedEvent = new(initialState: false);
    private readonly RegisteredWaitHandle waitHandle;

    private RegistryChangeWatcher(RegistryKey key, Action onChanged, Action onStopped)
    {
        this.key = key;
        this.onChanged = onChanged;
        this.onStopped = onStopped;
        waitHandle = ThreadPool.RegisterWaitForSingleObject(
            changedEvent, (_, _) => HandleChanged(), state: null, Timeout.Infinite, executeOnlyOnce: false);
    }

    /// <summary>
    /// 監視を始める。キーが無い場合や、読み込めない・監視を登録できない場合は null（呼び出し元は読み直す方法に戻す）。
    /// </summary>
    /// <param name="keyPath">HKCU からのキーのパス。</param>
    /// <param name="onChanged">値が変わったときに、スレッドプールのスレッドで呼ばれる。</param>
    /// <param name="onStopped">
    /// 通知を登録し直せず、監視が止まったときに1回だけ、スレッドプールのスレッドで呼ばれる（キーが削除された場合など）。
    /// 呼ばれた後は通知されないので、呼び出し元は <see cref="Dispose"/> し、読み直す方法に戻す。
    /// </param>
    public static RegistryChangeWatcher? TryStart(string keyPath, Action onChanged, Action onStopped)
    {
        RegistryKey? key;
        try
        {
            key = Registry.CurrentUser.OpenSubKey(keyPath);
        }
        catch (Exception exception) when (RegistryErrors.IsAccessFailure(exception))
        {
            return null;
        }
        if (key is null)
        {
            return null;
        }

        var watcher = new RegistryChangeWatcher(key, onChanged, onStopped);
        if (!watcher.Register())
        {
            watcher.Dispose();
            return null;
        }
        return watcher;
    }

    public void Dispose()
    {
        // 実行中の通知が終わるのを待ってから、キーとイベントを閉じる（終了の処理と通知が重なっても、
        // 破棄した後のものに触らないように）
        using (var unregistered = new ManualResetEvent(initialState: false))
        {
            if (waitHandle.Unregister(unregistered))
            {
                unregistered.WaitOne();
            }
        }
        // キーを閉じると、登録した通知も取り消される
        key.Dispose();
        changedEvent.Dispose();
    }

    /// <summary>次の変更を1回だけ通知させる（通知のたびに登録し直す必要がある）。</summary>
    private bool Register() =>
        RegNotifyChangeKeyValue(
            key.Handle, bWatchSubtree: false, REG_NOTIFY_CHANGE_LAST_SET | REG_NOTIFY_THREAD_AGNOSTIC,
            changedEvent.SafeWaitHandle, fAsynchronous: true) == ERROR_SUCCESS;

    private void HandleChanged()
    {
        // 先に登録し直してから知らせる。知らせた先で値を読むので、その間の変更も取りこぼさない
        bool isRegistered;
        try
        {
            isRegistered = Register();
        }
        // Dispose でキーを閉じた後に呼ばれた場合
        catch (ObjectDisposedException)
        {
            return;
        }
        onChanged();
        // キーが削除された場合など。開いたままのハンドルは削除されたキーを指すので、キーが作り直されても通知されない
        if (!isRegistered)
        {
            onStopped();
        }
    }
}
