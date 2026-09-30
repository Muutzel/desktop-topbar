using Microsoft.UI.Dispatching;
using TopBar.App.Services;
using TopBar.Core.Pomodoro;
using TopBar.Core.Services;
using TopBar.Platform;

namespace TopBar.App.ViewModels;

/// <summary>Drives the <see cref="PomodoroEngine"/>: a 250 ms UI timer that only runs while the timer runs, settings changes, toast at phase end.</summary>
public sealed class PomodoroViewModel
{
    private readonly PomodoroEngine _engine;
    private readonly ISettingsService _settings;
    private readonly DispatcherQueueTimer _timer;
    private (string Text, PomodoroPhase Phase, PomodoroRunState State, int Session, int Count) _last;

    public string Text { get; private set; } = "";
    public PomodoroPhase Phase => _engine.Phase;
    public PomodoroRunState State => _engine.State;
    public int Session => _engine.Session;
    public int SessionCount => _engine.SessionCount;

    /// <summary>Raised on the UI thread whenever something visible changed.</summary>
    public event Action? Updated;

    // Must be created on the UI thread (the DispatcherQueue of the current thread drives the timer).
    internal PomodoroViewModel(ISettingsService settings, IUiThread ui)
    {
        _settings = settings;
        _engine = new PomodoroEngine(settings.Current.Pomodoro);
        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(250);
        _timer.IsRepeating = true;
        _timer.Tick += (_, _) => Tick();
        settings.Changed += (_, _) => ui.Post(() =>
        {
            _engine.ApplySettings(_settings.Current.Pomodoro, Now);
            Update();
        });
        Update();
    }

    private static DateTimeOffset Now => DateTimeOffset.UtcNow;

    public void Toggle()
    {
        _engine.Toggle(Now);
        Update();
    }

    /// <summary>Ends the current phase early; the next one (e.g. the break) starts immediately.</summary>
    public void Skip()
    {
        _engine.Skip(Now);
        Update();
    }

    public void Reset()
    {
        _engine.Reset();
        Update();
    }

    private void Tick()
    {
        var finished = _engine.Phase;
        var session = _engine.Session;
        if (_engine.Update(Now)) Notify(finished, session);
        Update();
    }

    /// <summary>Toast when a phase has run out; the next phase waits for a manual start.</summary>
    private void Notify(PomodoroPhase finished, int session)
    {
        var count = _engine.SessionCount;
        var (title, text) = finished switch
        {
            PomodoroPhase.Work => (
                string.Format(Loc.Get("Pomodoro_Toast_WorkDone_Title"), session, count),
                Loc.Get(_engine.Phase == PomodoroPhase.LongBreak ? "Pomodoro_Toast_WorkDone_Long" : "Pomodoro_Toast_WorkDone_Short")),
            PomodoroPhase.ShortBreak => (
                Loc.Get("Pomodoro_Toast_BreakDone_Title"),
                string.Format(Loc.Get("Pomodoro_Toast_BreakDone_Text"), _engine.Session, count)),
            _ => (
                Loc.Get("Pomodoro_Toast_CycleDone_Title"),
                string.Format(Loc.Get("Pomodoro_Toast_CycleDone_Text"), count)),
        };
        AppNotifier.Show(title, text);
    }

    public void Update()
    {
        Text = PomodoroFormat.Timer(_engine.Remaining(Now));

        if (_engine.State == PomodoroRunState.Running) { if (!_timer.IsRunning) _timer.Start(); }
        else _timer.Stop();

        var current = (Text, _engine.Phase, _engine.State, _engine.Session, _engine.SessionCount);
        if (current == _last) return;
        _last = current;
        Updated?.Invoke();
    }
}
