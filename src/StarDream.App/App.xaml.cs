using Microsoft.UI.Xaml;

namespace StarDream.App;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        RequestedTheme = ApplicationTheme.Dark;
        UnhandledException += OnUnhandledException;
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        try
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "stardream-crash.txt"),
                e.Exception?.ToString() ?? e.Message);
        }
        catch (System.IO.IOException)
        {
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
