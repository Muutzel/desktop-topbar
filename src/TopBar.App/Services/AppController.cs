using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Serilog;
using TopBar.App.ViewModels;
using TopBar.App.Views;
using TopBar.Core.Services;
using TopBar.Core.Settings;
using TopBar.Platform;
using TopBar.Platform.AppBar;
using TopBar.Platform.Services;
using TopBar.Platform.Stats;
using TopBar.Platform.VirtualDesktops;

namespace TopBar.App.Services;

/// <summary>Composition root and lifecycle: startup order (spec 12.3), reload, exit, single settings window.</summary>
internal static class AppController
{
    private static IHost? _host;
    private static Microsoft.Extensions.Logging.ILogger _log = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    private static BarManager? _bars;
    private static BarViewModels? _viewModels;
    private static SettingsWindow? _settingsWindow;
    private static bool _exiting;

    public static IServiceProvider Services => _host!.Services;
    public static DispatcherUiThread UiThread { get; private set; } = null!;

    public static void Start(bool autostartLaunch)
    {
        try
        {
            UiThread = new DispatcherUiThread(DispatcherQueue.GetForCurrentThread());
            InitLogging();

            var firstRun = !File.Exists(AppPaths.SettingsFile);
            _host = BuildHost();
            _log = _host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("TopBar");
            _log.LogInformation("Starting TopBar {Version} (autostart launch: {Autostart}, first run: {First})",
                typeof(AppController).Assembly.GetName().Version, autostartLaunch, firstRun);

            var settings = _host.Services.GetRequiredService<ISettingsService>();
            ReconcileAutostart(settings);

            AppNotifier.Initialize();
            StartServices();
            _viewModels = new BarViewModels(
                _host.Services.GetRequiredService<IClockService>(),
                _host.Services.GetRequiredService<IWeatherService>(),
                _host.Services.GetRequiredService<ISystemStatsService>(),
                _host.Services.GetRequiredService<IVirtualDesktopService>(),
                settings, UiThread);

            _bars = new BarManager(settings, _viewModels, _log);
            _bars.Start();

            if (firstRun && !autostartLaunch) OpenSettings();
        }
        catch (Exception ex)
        {
            LogFatal(ex, "Startup");
            AppBarController.RemoveAll();
            Application.Current.Exit();
        }
    }

    private static void InitLogging()
    {
        Directory.CreateDirectory(AppPaths.LogDir);
        var config = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .WriteTo.File(Path.Combine(AppPaths.LogDir, "topbar-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, shared: true);
        Log.Logger = config.CreateLogger();
    }

    private static IHost BuildHost()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Services.AddLogging(b => b.ClearProviders().AddSerilog(Log.Logger, dispose: false));

        builder.Services.AddSingleton<IUiThread>(_ => UiThread);
        builder.Services.AddSingleton<ISettingsService>(sp =>
            new SettingsService(AppPaths.SettingsFile, sp.GetRequiredService<ILogger<SettingsService>>()));
        builder.Services.AddSingleton<IAutostartService, AutostartService>();

        AddHosted<ClockService, IClockService>(builder.Services);
        AddHosted<WeatherService, IWeatherService>(builder.Services);
        AddHosted<SystemStatsService, ISystemStatsService>(builder.Services);
        AddHosted<VirtualDesktopService, IVirtualDesktopService>(builder.Services);

        return builder.Build();
    }

    private static void AddHosted<TImpl, TService>(IServiceCollection services)
        where TImpl : class, TService, IHostedService
        where TService : class
    {
        services.AddSingleton<TImpl>();
        services.AddSingleton<TService>(sp => sp.GetRequiredService<TImpl>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<TImpl>());
    }

    private static void ReconcileAutostart(ISettingsService settings)
    {
        var autostart = _host!.Services.GetRequiredService<IAutostartService>();
        // The setting is authoritative; on first run the default (true) enables autostart.
        if (autostart.IsEnabled != settings.Current.General.Autostart) autostart.SetEnabled(settings.Current.General.Autostart);
        autostart.RepairPathIfNeeded();
        settings.Changed += (_, s) =>
        {
            if (autostart.IsEnabled != s.General.Autostart) autostart.SetEnabled(s.General.Autostart);
        };
    }

    private static void StartServices()
    {
        foreach (var service in _host!.Services.GetServices<IHostedService>())
        {
            try { service.StartAsync(CancellationToken.None).GetAwaiter().GetResult(); }
            catch (Exception ex) { _log.LogError(ex, "Service {Service} failed to start", service.GetType().Name); }
        }
    }

    private static void StopServices()
    {
        foreach (var service in _host!.Services.GetServices<IHostedService>().Reverse())
        {
            try { service.StopAsync(CancellationToken.None).GetAwaiter().GetResult(); }
            catch (Exception ex) { _log.LogError(ex, "Service {Service} failed to stop", service.GetType().Name); }
        }
    }

    /// <summary>Context menu "Neu laden": close all bars, restart services, recreate bars.</summary>
    public static void Reload()
    {
        if (_exiting || _bars is null) return;
        UiThread.Post(() =>
        {
            try
            {
                _log.LogInformation("Reloading");
                _bars.CloseAll();
                StopServices();
                StartServices();
                _viewModels?.RefreshAll();
                _bars.Start();
            }
            catch (Exception ex) { LogFatal(ex, "Reload"); }
        });
    }

    /// <summary>Clean exit: release the reserved space first, then stop services and quit.</summary>
    public static void Exit()
    {
        if (_exiting) return;
        _exiting = true;
        try
        {
            _log.LogInformation("Exiting");
            _bars?.CloseAll();
            StopServices();
            AppNotifier.Shutdown();
        }
        catch (Exception ex) { LogFatal(ex, "Exit"); }
        finally
        {
            AppBarController.RemoveAll();
            Log.CloseAndFlush();
            Application.Current.Exit();
        }
    }

    /// <summary>Opens (or focuses) the single settings window. <paramref name="page"/> is a page tag such as "weather".</summary>
    public static void OpenSettings(string? page = null)
    {
        UiThread.Post(() =>
        {
            try
            {
                if (_settingsWindow is null)
                {
                    _settingsWindow = new SettingsWindow();
                    _settingsWindow.Closed += (_, _) => _settingsWindow = null;
                }
                _settingsWindow.Show(page);
            }
            catch (Exception ex) { LogFatal(ex, "OpenSettings"); }
        });
    }

    /// <summary>Called on the first instance when a second start was redirected to it.</summary>
    public static void OnRedirectedActivation(bool autostart)
    {
        if (autostart || UiThread is null) return;
        OpenSettings();
    }

    public static void LogFatal(Exception? ex, string source)
    {
        try { Log.Error(ex, "Unhandled exception in {Source}", source); }
        catch { /* logging must never throw */ }
    }

    public static void LogWarning(Exception ex, string message)
    {
        try { Log.Warning(ex, "{Message}", message); }
        catch { /* ignore */ }
    }
}
