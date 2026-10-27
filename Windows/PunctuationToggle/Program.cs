using System.ComponentModel;
using System.Runtime.InteropServices;

// P/Invoke の DLL（user32.dll など）を、exe と同じフォルダー（単一ファイルの exe では展開先）より先に探さず、
// System32 からだけ読み込む。すべてのキー入力を監視するプロセスに、置かれた同名の DLL を読み込ませないため（CA5392）
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace PunctuationToggle;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // 二重に起動すると切り替えキーで2回切り替わってしまうので防ぐ
        using var mutex = new Mutex(initiallyOwned: true, @"Local\PunctuationToggle", out var isFirstInstance);
        if (!isFirstInstance)
        {
            return;
        }

        ApplicationConfiguration.Initialize();

        TrayApplication application;
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

        using (application)
        {
            Application.Run(application);
        }
    }
}
