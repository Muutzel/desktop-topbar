using Microsoft.UI.Xaml;
using TopBar.App.Services;

namespace TopBar.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            AppController.LogFatal(e.ExceptionObject as Exception, "AppDomain.UnhandledException");
            Platform.AppBar.AppBarController.RemoveAll();
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var autostart = Environment.GetCommandLineArgs().Any(a => string.Equals(a, "--autostart", StringComparison.OrdinalIgnoreCase));
        AppController.Start(autostart);
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // A faulty module must never take the bar down: log, release AppBar space as a precaution only if we cannot continue.
        AppController.LogFatal(e.Exception, "Application.UnhandledException");
        e.Handled = true;
    }
}
