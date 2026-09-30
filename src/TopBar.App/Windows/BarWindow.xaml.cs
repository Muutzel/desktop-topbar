using Microsoft.Extensions.Logging;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using TopBar.App.Controls;
using TopBar.App.Services;
using TopBar.App.ViewModels;
using TopBar.Core.Services;
using TopBar.Platform;
using TopBar.Platform.AppBar;
using WinRT;
using WinRT.Interop;

namespace TopBar.App.Views;

/// <summary>One bar on one monitor: borderless, acrylic, registered as top AppBar.</summary>
public sealed partial class BarWindow : Window
{
    private readonly ISettingsService _settings;
    private readonly BarViewModels _viewModels;
    private readonly ILogger _log;
    private readonly string _monitorDevice;
    private readonly nint _hwnd;
    private readonly AppBarController _appBar;
    private readonly Windows.UI.ViewManagement.UISettings _uiSettings = new();
    private DesktopAcrylicController? _acrylic;
    private SystemBackdropConfiguration? _backdropConfig;
    private bool _allowClose;

    public string MonitorDevice => _monitorDevice;

    public BarWindow(string monitorDevice, ISettingsService settings, BarViewModels viewModels, ILogger log)
    {
        _monitorDevice = monitorDevice;
        _settings = settings;
        _viewModels = viewModels;
        _log = log;

        InitializeComponent();
        Title = "TopBar";
        _hwnd = WindowNative.GetWindowHandle(this);

        ConfigureWindow();
        BuildContent();
        SetupBackdrop();
        ApplyAppearance();

        _appBar = new AppBarController(_hwnd, () => MonitorEnumerator.FindByDeviceName(_monitorDevice), () => _settings.Current.General.BarHeight, _log);
        _appBar.FullscreenChanged += OnFullscreenChanged;
        _appBar.DisplayChanged += () => DisplayChanged?.Invoke();
        _appBar.Register();

        _settings.Changed += OnSettingsChanged;
        _uiSettings.ColorValuesChanged += (_, _) => DispatcherQueue.TryEnqueue(UpdateBackdropTheme);
        Root.ActualThemeChanged += (_, _) => UpdateBackdropTheme();
        AppWindow.Closing += (_, e) => { if (!_allowClose) e.Cancel = true; }; // Alt+F4 must not close the bar
        Closed += OnClosed;

        Activate();
    }

    /// <summary>Display configuration changed; the BarManager re-evaluates the monitor list.</summary>
    public event Action? DisplayChanged;

    private void ConfigureWindow()
    {
        var presenter = OverlappedPresenter.Create();
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = false;
        presenter.IsMinimizable = false;
        presenter.IsMaximizable = false;
        presenter.IsAlwaysOnTop = true;
        AppWindow.SetPresenter(presenter);
        AppWindow.IsShownInSwitchers = false;
        var style = Win32Window.StripFrame(_hwnd);
        var (border, corner) = Win32Window.RemoveBorderAndCorners(_hwnd);
        _log.LogInformation("Bar frame: style=0x{Style:X8} dwmBorder=0x{Border:X8} dwmCorner=0x{Corner:X8}", style, border, corner);
        Win32Window.MakeToolWindow(_hwnd); // not in taskbar / Alt+Tab; deliberately NOT WS_EX_NOACTIVATE (breaks flyouts)
    }

    private void BuildContent()
    {
        // Left section: virtual desktops, then the Pomodoro timer to their right (ThreeSectionPanel takes exactly three children).
        var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Stretch };
        left.Children.Add(new DesktopIndicator(_viewModels.Desktops, _settings));
        left.Children.Add(new PomodoroControl(_viewModels.Pomodoro));
        Sections.Children.Add(left);
        Sections.Children.Add(new CenterInfo(_viewModels.Clock, _viewModels.Weather, _settings));
        Sections.Children.Add(new SystemStatsControl(_viewModels.Stats));
    }

    private void SetupBackdrop()
    {
        try
        {
            if (!DesktopAcrylicController.IsSupported())
            {
                _log.LogWarning("Bar: acrylic backdrop not supported");
                return;
            }
            // IsInputActive stays true permanently: the bar rarely has focus and would otherwise render a flat fallback color.
            _backdropConfig = new SystemBackdropConfiguration { IsInputActive = true };
            _acrylic = new DesktopAcrylicController();
            _acrylic.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
            _acrylic.SetSystemBackdropConfiguration(_backdropConfig);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Bar: backdrop setup failed");
        }
    }

    private void OnSettingsChanged(object? sender, Core.Settings.AppSettings e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ApplyAppearance();
            _appBar.UpdatePosition(); // bar height may have changed
        });
    }

    private void ApplyAppearance()
    {
        var g = _settings.Current.General;
        Root.RequestedTheme = g.Theme switch { "Light" => ElementTheme.Light, "Dark" => ElementTheme.Dark, _ => ElementTheme.Default };
        Root.FontSize = g.FontSize;
        if (_acrylic is not null) _acrylic.TintOpacity = (float)g.BackgroundOpacity;
        UpdateBackdropTheme();
    }

    private void UpdateBackdropTheme()
    {
        if (_backdropConfig is null) return;
        _backdropConfig.Theme = Root.ActualTheme == ElementTheme.Dark ? SystemBackdropTheme.Dark : SystemBackdropTheme.Light;
        _backdropConfig.IsInputActive = true;
    }

    private void OnFullscreenChanged(bool fullscreenAppActive)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (fullscreenAppActive) AppWindow.Hide();
                else AppWindow.Show(false); // AppBar stays registered while hidden
            }
            catch (Exception ex) { _log.LogError(ex, "Bar: toggling visibility failed"); }
        });
    }

    /// <summary>Recomputes size and position (monitor/DPI/height changes).</summary>
    public void UpdatePosition() => _appBar.UpdatePosition();

    /// <summary>Closes the window for real (releases the reserved screen space first).</summary>
    public void CloseForReal()
    {
        _allowClose = true;
        try { Close(); }
        catch (Exception ex) { _log.LogWarning(ex, "Bar: close failed"); _appBar.Dispose(); }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _settings.Changed -= OnSettingsChanged;
        _appBar.Dispose(); // ABM_REMOVE
        _acrylic?.Dispose();
        _acrylic = null;
        _backdropConfig = null;
    }

    private void Root_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var flyout = new MenuFlyout();

        var settings = new MenuFlyoutItem { Text = Loc.Get("Menu_Settings"), Icon = SegoeIcon("") };
        settings.Click += (_, _) => AppController.OpenSettings();
        var reload = new MenuFlyoutItem { Text = Loc.Get("Menu_Reload"), Icon = SegoeIcon("") };
        reload.Click += (_, _) => AppController.Reload();
        var exit = new MenuFlyoutItem { Text = Loc.Get("Menu_Exit"), Icon = SegoeIcon("") };
        exit.Click += (_, _) => AppController.Exit();

        flyout.Items.Add(settings);
        flyout.Items.Add(reload);
        flyout.Items.Add(new MenuFlyoutSeparator());
        flyout.Items.Add(exit);

        flyout.ShowAt(Root, new FlyoutShowOptions { Position = e.GetPosition(Root) });
        e.Handled = true;
    }

    private static FontIcon SegoeIcon(string glyph) =>
        new() { Glyph = glyph, FontFamily = (Microsoft.UI.Xaml.Media.FontFamily)Application.Current.Resources["SegoeIconsFont"] };
}
