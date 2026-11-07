using System.ComponentModel;
using System.Runtime.InteropServices;

// P/Invoke の DLL（user32.dll など）を、exe と同じフォルダー（単一ファイルの exe では展開先）より先に探さず、
// System32 からだけ読み込む。すべてのキー入力を監視するプロセスに、置かれた同名の DLL を読み込ませないため（CA5392）
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace PunctuationToggle;

internal static class Program
{
    private const string MutexName = @"Local\PunctuationToggle";

    // 2つ目に起動したプロセスが、すでに動いているプロセスに起動されたことを知らせるイベント
    private const string AlreadyRunningEventName = @"Local\PunctuationToggle.AlreadyRunning";

    private const string IssuesUrl = TrayApplication.RepositoryUrl + "/issues";

    [STAThread]
    private static void Main()
    {
        // 二重に起動すると切り替えキーで2回切り替わってしまうので防ぐ
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            // タスクトレイのアイコンは隠れていることが多く、何も起きないと起動に失敗したように見えるため、
            // すでに動いているプロセスに知らせ、そちらで通知を出してもらう（アイコンの場所も示せる）
            if (EventWaitHandle.TryOpenExisting(AlreadyRunningEventName, out var alreadyRunningEvent))
            {
                using (alreadyRunningEvent)
                {
                    alreadyRunningEvent.Set();
                }
            }
            return;
        }
        using var alreadyRunning = new EventWaitHandle(false, EventResetMode.AutoReset, AlreadyRunningEventName);

        ApplicationConfiguration.Initialize();

        // 想定外の例外が起きたら、知らせて終了する。キー入力を監視するアプリなので、
        // Windows Forms の既定のダイアログの「続行」で、状態が分からないまま監視を続けさせない
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Exception? fatalException = null;
        TrayApplication? application = null;
        Application.ThreadException += (_, eventArgs) =>
        {
            // メッセージループを終わらせ、フックを外してから（下の using の後で）知らせる
            fatalException ??= eventArgs.Exception;
            application?.ExitThread();
        };
        // UI スレッド以外の例外では、この後プロセスが終了する（フックも OS が外す）ので、知らせるだけ
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            ShowFatalError(eventArgs.ExceptionObject as Exception);

        try
        {
            application = new TrayApplication();
        }
        catch (Win32Exception exception)
        {
            MessageBox.Show(
                $"キーボードの監視を開始できませんでした。{Environment.NewLine}{exception.Message}",
                "PunctuationToggle",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }
        // トレイアイコンの作成などで想定外の例外が起きた場合も、ほかの想定外の例外と同じく知らせて終了する
#pragma warning disable CA1031 // 上の理由で、すべての例外を捕まえる
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ShowFatalError(exception);
            return;
        }

        using (application)
        {
            var uiContext = SynchronizationContext.Current;
            var alreadyRunningWait = ThreadPool.RegisterWaitForSingleObject(
                alreadyRunning,
                (_, _) => uiContext?.Post(_ => application.ShowAlreadyRunningNotice(), null),
                state: null,
                Timeout.Infinite,
                executeOnlyOnce: false);
            try
            {
                Application.Run(application);
            }
            finally
            {
                alreadyRunningWait.Unregister(waitObject: null);
            }
        }

        if (fatalException is not null)
        {
            ShowFatalError(fatalException);
        }
    }

    private static void ShowFatalError(Exception? exception)
    {
        var detail = "";
        for (var current = exception; current is not null; current = current.InnerException)
        {
            detail += $"{Environment.NewLine}{current.GetType().Name}: {current.Message}";
        }
        if (detail.Length > 0)
        {
            detail = Environment.NewLine + detail;
        }
        MessageBox.Show(
            $"予期しないエラーが起きたため、PunctuationToggle を終了します。{detail}{Environment.NewLine}{Environment.NewLine}" +
            $"もう一度起動して使えます。繰り返し起きる場合は、次のページからお知らせください。{Environment.NewLine}{IssuesUrl}",
            "PunctuationToggle",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
