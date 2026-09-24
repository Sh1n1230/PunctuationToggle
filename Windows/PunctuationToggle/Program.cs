namespace PunctuationToggle;

static class Program
{
    [STAThread]
    static void Main()
    {
        // 二重起動すると右Ctrlで2回切り替わってしまうので防ぐ
        using var mutex = new Mutex(true, @"Local\PunctuationToggle", out var createdNew);
        if (!createdNew)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        using var app = new TrayApp();
        Application.Run(app);
    }
}
