using TopBar.Core.Settings;

namespace TopBar.Core.Pomodoro;

public enum PomodoroPhase { Work, ShortBreak, LongBreak }

public enum PomodoroRunState { Idle, Running, Paused }

/// <summary>
/// Pomodoro state machine without any timer or UI dependency: the caller passes the current time into every call.
/// Cycle: Work 1 → short break → … → Work N → long break → back to Idle. When a phase runs out the next one is prepared but
/// <b>not</b> started (state Paused, full duration): the user starts it manually.
/// </summary>
public sealed class PomodoroEngine
{
    private int _workMinutes;
    private int _shortBreakMinutes;
    private int _sessions;
    private int _longBreakMinutes;
    private TimeSpan _remaining;      // valid while Idle/Paused
    private DateTimeOffset _endsAt;   // valid while Running

    public PomodoroEngine(PomodoroSettings settings)
    {
        CopySettings(settings);
        Reset();
    }

    public PomodoroPhase Phase { get; private set; }
    public PomodoroRunState State { get; private set; }
    /// <summary>1-based number of the current work unit (also during the break that follows it).</summary>
    public int Session { get; private set; }
    public int SessionCount => _sessions;

    public TimeSpan Remaining(DateTimeOffset now) =>
        State == PomodoroRunState.Running ? Max(TimeSpan.Zero, _endsAt - now) : _remaining;

    public void Start(DateTimeOffset now)
    {
        if (State == PomodoroRunState.Running) return;
        _endsAt = now + _remaining;
        State = PomodoroRunState.Running;
    }

    public void Pause(DateTimeOffset now)
    {
        if (State != PomodoroRunState.Running) return;
        _remaining = Remaining(now);
        State = PomodoroRunState.Paused;
    }

    public void Toggle(DateTimeOffset now)
    {
        if (State == PomodoroRunState.Running) Pause(now);
        else Start(now);
    }

    /// <summary>Back to the start of the cycle (work unit 1, stopped).</summary>
    public void Reset()
    {
        Phase = PomodoroPhase.Work;
        Session = 1;
        State = PomodoroRunState.Idle;
        _remaining = DurationOf(Phase);
    }

    /// <summary>Advances to the next phase when the current one has run out. Returns true if the phase changed (or the cycle ended).</summary>
    public bool Update(DateTimeOffset now)
    {
        if (State != PomodoroRunState.Running || now < _endsAt) return false;
        Advance(now, autoStart: false);
        return true;
    }

    /// <summary>Manual skip: ends the current phase now and starts the next one running (also from pause). No effect while idle.</summary>
    public bool Skip(DateTimeOffset now)
    {
        if (State == PomodoroRunState.Idle) return false;
        Advance(now, autoStart: true);
        return true;
    }

    private void Advance(DateTimeOffset now, bool autoStart)
    {
        switch (Phase)
        {
            case PomodoroPhase.Work:
                Phase = Session >= _sessions ? PomodoroPhase.LongBreak : PomodoroPhase.ShortBreak;
                break;
            case PomodoroPhase.ShortBreak:
                // Normally Session < _sessions here; after lowering the unit count mid-cycle the long break comes next.
                if (Session >= _sessions) Phase = PomodoroPhase.LongBreak;
                else { Phase = PomodoroPhase.Work; Session++; }
                break;
            default: // long break over: cycle finished
                Reset();
                return;
        }

        _remaining = DurationOf(Phase);
        if (autoStart)
        {
            _endsAt = now + _remaining;
            State = PomodoroRunState.Running;
        }
        else
        {
            State = PomodoroRunState.Paused; // waiting for the user to start it
        }
    }

    /// <summary>Takes over changed settings. Idle: new duration. Running/paused: the current phase keeps its time but never exceeds the new duration.</summary>
    public void ApplySettings(PomodoroSettings settings, DateTimeOffset now)
    {
        var remaining = Remaining(now);
        CopySettings(settings);
        Session = Math.Min(Session, _sessions);

        if (State == PomodoroRunState.Idle)
        {
            _remaining = DurationOf(Phase);
            return;
        }

        var limit = DurationOf(Phase);
        if (remaining <= limit) return;
        if (State == PomodoroRunState.Running) _endsAt = now + limit;
        else _remaining = limit;
    }

    private void CopySettings(PomodoroSettings s)
    {
        _workMinutes = Math.Max(1, s.WorkMinutes);
        _shortBreakMinutes = Math.Max(1, s.ShortBreakMinutes);
        _sessions = Math.Max(1, s.Sessions);
        _longBreakMinutes = Math.Max(1, s.LongBreakMinutes);
    }

    private TimeSpan DurationOf(PomodoroPhase phase) => TimeSpan.FromMinutes(phase switch
    {
        PomodoroPhase.Work => _workMinutes,
        PomodoroPhase.ShortBreak => _shortBreakMinutes,
        _ => _longBreakMinutes,
    });

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}

public static class PomodoroFormat
{
    /// <summary>"mm:ss", seconds rounded up so the display never shows 00:00 while time is left.</summary>
    public static string Timer(TimeSpan remaining)
    {
        var seconds = (int)Math.Ceiling(Math.Max(0, remaining.TotalSeconds));
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }
}
