using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TopBar.App.Services;
using TopBar.App.ViewModels;
using TopBar.Core.Pomodoro;

namespace TopBar.App.Controls;

/// <summary>Pomodoro timer next to the virtual desktops: [play/pause] 24:59 1/4 [stop]. Click = start/pause, stop = reset.</summary>
public sealed partial class PomodoroControl : UserControl
{
    // Segoe UI Emoji rendered monochrome (IsColorFontEnabled=False), so the glyphs take the text color like the other bar icons.
    private const int GlyphWork = 0x1F345;  // tomato
    private const int GlyphBreak = 0x2615;  // hot beverage

    private readonly PomodoroViewModel _vm;

    public PomodoroControl(PomodoroViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        ToolTipService.SetToolTip(ResetButton, Loc.Get("Pomodoro_Reset"));
        ToolTipService.SetToolTip(SkipButton, Loc.Get("Pomodoro_Skip"));
        Loaded += (_, _) => { _vm.Updated += Refresh; Refresh(); };
        Unloaded += (_, _) => _vm.Updated -= Refresh;
        ActualThemeChanged += (_, _) => Refresh();
    }

    private void Refresh()
    {
        var state = _vm.State;
        var phase = _vm.Phase;
        var secondary = (Brush)Resources["SecondaryTextBrush"];

        // Tomato while working, coffee cup during breaks; dimmed unless the timer is running.
        StateIcon.Text = char.ConvertFromUtf32(phase == PomodoroPhase.Work ? GlyphWork : GlyphBreak);
        StateIcon.Opacity = state == PomodoroRunState.Running ? 1.0 : 0.45;

        TimeText.Text = _vm.Text;
        TimeText.Opacity = state == PomodoroRunState.Paused ? 0.6 : 1.0; // paused: dimmed
        TimeText.MinWidth = MeasureWidth(TimeText, "88:88");             // fixed width: no jumping

        var phaseName = phase switch
        {
            PomodoroPhase.Work => Loc.Get("Pomodoro_Phase_Work"),
            PomodoroPhase.ShortBreak => Loc.Get("Pomodoro_Phase_Break"),
            _ => Loc.Get("Pomodoro_Phase_LongBreak"),
        };
        var session = $"{_vm.Session}/{_vm.SessionCount}";
        InfoText.Text = phase == PomodoroPhase.Work ? session : phaseName;
        InfoText.Foreground = secondary;

        var active = state == PomodoroRunState.Idle ? Visibility.Collapsed : Visibility.Visible;
        ResetButton.Visibility = active;
        SkipButton.Visibility = active;

        var action = state switch
        {
            PomodoroRunState.Running => Loc.Get("Pomodoro_Tip_Pause"),
            PomodoroRunState.Paused => Loc.Get("Pomodoro_Tip_Resume"),
            _ => Loc.Get("Pomodoro_Tip_Start"),
        };
        ToolTipService.SetToolTip(MainButton, $"{phaseName} {session} · {action}");
    }

    private static double MeasureWidth(TextBlock reference, string sample)
    {
        var probe = new TextBlock { Text = sample, FontFamily = reference.FontFamily, FontSize = reference.FontSize };
        probe.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        return Math.Ceiling(probe.DesiredSize.Width);
    }

    private void MainButton_Click(object sender, RoutedEventArgs e) => _vm.Toggle();
    private void SkipButton_Click(object sender, RoutedEventArgs e) => _vm.Skip();
    private void ResetButton_Click(object sender, RoutedEventArgs e) => _vm.Reset();
}
