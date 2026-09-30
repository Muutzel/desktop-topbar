using Microsoft.UI.Xaml;
using TopBar.App.Controls;
using TopBar.App.ViewModels;
using TopBar.Core.Services;
using TopBar.Core.Settings;

namespace TopBar.App.Services;

/// <summary>Creates the control for a bar element id. Every call creates a new instance (one per bar window).</summary>
internal static class BarElementFactory
{
    /// <summary>Horizontal margin of elements that have no inner padding of their own; with the stat/gear buttons (6 px padding) this gives ≈ 12 px between neighbours.</summary>
    private static readonly Thickness TextMargin = new(6, 0, 6, 0);

    public static FrameworkElement? Create(string id, BarViewModels vm, ISettingsService settings)
    {
        FrameworkElement? element = id switch
        {
            BarElementIds.Desktops => new DesktopIndicator(vm.Desktops, settings) { Margin = TextMargin },
            BarElementIds.Pomodoro => new PomodoroControl(vm.Pomodoro) { Margin = TextMargin },
            BarElementIds.Weather => new WeatherControl(vm.Weather) { Margin = TextMargin },
            BarElementIds.Clock => new ClockControl(vm.Clock, settings, ClockPart.Time) { Margin = TextMargin },
            BarElementIds.Date => new ClockControl(vm.Clock, settings, ClockPart.Date) { Margin = TextMargin },
            BarElementIds.Cpu => new StatControl(vm.Stats, StatKind.Cpu),
            BarElementIds.Gpu => new StatControl(vm.Stats, StatKind.Gpu),
            BarElementIds.Ram => new StatControl(vm.Stats, StatKind.Ram),
            BarElementIds.Network => new StatControl(vm.Stats, StatKind.Network),
            BarElementIds.SettingsButton => new SettingsButtonControl(),
            _ => null,
        };
        return element;
    }
}
