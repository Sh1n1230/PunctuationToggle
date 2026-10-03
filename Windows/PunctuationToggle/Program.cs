using System.ComponentModel;

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
