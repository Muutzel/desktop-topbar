using Microsoft.UI.Xaml.Controls;
using TopBar.App.Services;
using TopBar.Core.Settings;

namespace TopBar.App.SettingsPages;

public sealed partial class PomodoroPage : Page
{
    private readonly PageState _s = new();

    public PomodoroPage()
    {
        InitializeComponent();
        Loaded += (_, _) => _s.Load(LoadValues);
        Unloaded += (_, _) => _s.Flush();
    }

    private void LoadValues()
    {
        var p = _s.Settings.Current.Pomodoro;
        WorkBox.Value = p.WorkMinutes;
        ShortBreakBox.Value = p.ShortBreakMinutes;
        SessionsBox.Value = p.Sessions;
        LongBreakBox.Value = p.LongBreakMinutes;
    }

    // NumberBox raises ValueChanged on commit (Enter, focus loss, spinner), not per keystroke: saving immediately is fine.
    private void WorkBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) =>
        Save(args, 1, 99, (x, v) => x.Pomodoro.WorkMinutes = v);

    private void ShortBreakBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) =>
        Save(args, 1, 99, (x, v) => x.Pomodoro.ShortBreakMinutes = v);

    private void SessionsBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) =>
        Save(args, 1, 12, (x, v) => x.Pomodoro.Sessions = v);

    private void LongBreakBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) =>
        Save(args, 1, 99, (x, v) => x.Pomodoro.LongBreakMinutes = v);

    private void Save(NumberBoxValueChangedEventArgs args, int min, int max, Action<AppSettings, int> apply)
    {
        if (double.IsNaN(args.NewValue)) return;
        var value = (int)Math.Clamp(Math.Round(args.NewValue), min, max);
        _s.Save(x => apply(x, value));
    }
}
