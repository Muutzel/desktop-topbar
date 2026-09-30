using TopBar.Core.Pomodoro;
using TopBar.Core.Settings;

namespace TopBar.Core.Tests;

public class PomodoroEngineTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 30, 9, 0, 0, TimeSpan.Zero);
    private static DateTimeOffset After(double minutes) => T0 + TimeSpan.FromMinutes(minutes);

    private static PomodoroEngine Engine(int work = 25, int shortBreak = 5, int sessions = 4, int longBreak = 15) =>
        new(new PomodoroSettings { WorkMinutes = work, ShortBreakMinutes = shortBreak, Sessions = sessions, LongBreakMinutes = longBreak });

    [Fact]
    public void Defaults_AreStoppedAtFirstWorkUnit()
    {
        var e = Engine();
        Assert.Equal(PomodoroRunState.Idle, e.State);
        Assert.Equal(PomodoroPhase.Work, e.Phase);
        Assert.Equal(1, e.Session);
        Assert.Equal(4, e.SessionCount);
        Assert.Equal(TimeSpan.FromMinutes(25), e.Remaining(T0));
    }

    [Fact]
    public void SettingsDefaults_MatchSpec()
    {
        var p = new PomodoroSettings();
        Assert.Equal(25, p.WorkMinutes);
        Assert.Equal(5, p.ShortBreakMinutes);
        Assert.Equal(4, p.Sessions);
        Assert.Equal(15, p.LongBreakMinutes);
    }

    [Fact]
    public void Running_CountsDown()
    {
        var e = Engine();
        e.Start(T0);
        Assert.Equal(TimeSpan.FromMinutes(20), e.Remaining(After(5)));
        Assert.False(e.Update(After(5)));
    }

    [Fact]
    public void Pause_FreezesRemaining_ResumeContinues()
    {
        var e = Engine();
        e.Start(T0);
        e.Pause(After(10));
        Assert.Equal(PomodoroRunState.Paused, e.State);
        Assert.Equal(TimeSpan.FromMinutes(15), e.Remaining(After(60)));
        Assert.False(e.Update(After(60))); // a paused timer never advances

        e.Start(After(60));
        Assert.Equal(TimeSpan.FromMinutes(10), e.Remaining(After(65)));
    }

    [Fact]
    public void Toggle_StartsAndPauses()
    {
        var e = Engine();
        e.Toggle(T0);
        Assert.Equal(PomodoroRunState.Running, e.State);
        e.Toggle(After(1));
        Assert.Equal(PomodoroRunState.Paused, e.State);
    }

    [Fact]
    public void WorkEnd_PreparesShortBreak_ButDoesNotStartIt()
    {
        var e = Engine();
        e.Start(T0);
        Assert.True(e.Update(After(25)));
        Assert.Equal(PomodoroPhase.ShortBreak, e.Phase);
        Assert.Equal(PomodoroRunState.Paused, e.State);
        Assert.Equal(1, e.Session);
        Assert.Equal(TimeSpan.FromMinutes(5), e.Remaining(After(25)));
        Assert.Equal(TimeSpan.FromMinutes(5), e.Remaining(After(500))); // still waiting
        Assert.False(e.Update(After(500)));

        e.Start(After(500));
        Assert.Equal(PomodoroRunState.Running, e.State);
        Assert.Equal(TimeSpan.FromMinutes(3), e.Remaining(After(502)));
    }

    [Fact]
    public void FullCycle_FourUnits_EndsWithLongBreakThenIdle()
    {
        var e = Engine();
        e.Start(T0);
        var t = 0.0;

        for (var unit = 1; unit <= 4; unit++)
        {
            Assert.Equal(PomodoroPhase.Work, e.Phase);
            Assert.Equal(unit, e.Session);
            t += 25;
            Assert.True(e.Update(After(t)));
            e.Start(After(t)); // manual start of the next phase

            if (unit < 4)
            {
                Assert.Equal(PomodoroPhase.ShortBreak, e.Phase);
                t += 5;
                Assert.True(e.Update(After(t)));
                e.Start(After(t));
            }
        }

        Assert.Equal(PomodoroPhase.LongBreak, e.Phase);
        Assert.Equal(4, e.Session);
        Assert.Equal(TimeSpan.FromMinutes(15), e.Remaining(After(t)));

        t += 15;
        Assert.True(e.Update(After(t)));
        Assert.Equal(PomodoroRunState.Idle, e.State);
        Assert.Equal(PomodoroPhase.Work, e.Phase);
        Assert.Equal(1, e.Session);
        Assert.Equal(TimeSpan.FromMinutes(25), e.Remaining(After(t)));
    }

    [Fact]
    public void SingleUnit_GoesStraightToLongBreak()
    {
        var e = Engine(sessions: 1);
        e.Start(T0);
        Assert.True(e.Update(After(25)));
        Assert.Equal(PomodoroPhase.LongBreak, e.Phase);
    }

    [Fact]
    public void LateUpdate_AfterStandby_AdvancesOnlyOnePhase()
    {
        var e = Engine();
        e.Start(T0);
        Assert.True(e.Update(After(600))); // woke up 10 h later
        Assert.Equal(PomodoroPhase.ShortBreak, e.Phase);
        Assert.Equal(TimeSpan.FromMinutes(5), e.Remaining(After(600)));
    }

    [Fact]
    public void Skip_EndsWorkEarly_AndStartsBreakRunning()
    {
        var e = Engine();
        e.Start(T0);
        Assert.True(e.Skip(After(22))); // 3 minutes left
        Assert.Equal(PomodoroPhase.ShortBreak, e.Phase);
        Assert.Equal(PomodoroRunState.Running, e.State);
        Assert.Equal(TimeSpan.FromMinutes(5), e.Remaining(After(22)));
    }

    [Fact]
    public void Skip_WhilePaused_StartsNextPhaseRunning()
    {
        var e = Engine();
        e.Start(T0);
        e.Pause(After(10));
        e.Skip(After(11));
        Assert.Equal(PomodoroPhase.ShortBreak, e.Phase);
        Assert.Equal(PomodoroRunState.Running, e.State);
    }

    [Fact]
    public void Skip_WhileIdle_DoesNothing()
    {
        var e = Engine();
        Assert.False(e.Skip(T0));
        Assert.Equal(PomodoroRunState.Idle, e.State);
        Assert.Equal(PomodoroPhase.Work, e.Phase);
    }

    [Fact]
    public void Skip_DuringLongBreak_EndsCycle()
    {
        var e = Engine(sessions: 1);
        e.Start(T0);
        e.Update(After(25));
        e.Skip(After(26));
        Assert.Equal(PomodoroRunState.Idle, e.State);
    }

    [Fact]
    public void Reset_ReturnsToStart()
    {
        var e = Engine();
        e.Start(T0);
        e.Update(After(25));
        e.Reset();
        Assert.Equal(PomodoroRunState.Idle, e.State);
        Assert.Equal(PomodoroPhase.Work, e.Phase);
        Assert.Equal(1, e.Session);
        Assert.Equal(TimeSpan.FromMinutes(25), e.Remaining(After(30)));
    }

    [Fact]
    public void ApplySettings_WhenIdle_UsesNewDuration()
    {
        var e = Engine();
        e.ApplySettings(new PomodoroSettings { WorkMinutes = 50 }, T0);
        Assert.Equal(TimeSpan.FromMinutes(50), e.Remaining(T0));
    }

    [Fact]
    public void ApplySettings_WhileRunning_ShortensButNeverExtendsCurrentPhase()
    {
        var e = Engine();
        e.Start(T0);

        e.ApplySettings(new PomodoroSettings { WorkMinutes = 50 }, After(5));
        Assert.Equal(TimeSpan.FromMinutes(20), e.Remaining(After(5))); // unchanged

        e.ApplySettings(new PomodoroSettings { WorkMinutes = 10 }, After(5));
        Assert.Equal(TimeSpan.FromMinutes(10), e.Remaining(After(5))); // capped
        Assert.True(e.Update(After(15)));
    }

    [Fact]
    public void ApplySettings_WhilePaused_CapsRemaining()
    {
        var e = Engine();
        e.Start(T0);
        e.Pause(After(1));
        e.ApplySettings(new PomodoroSettings { WorkMinutes = 10 }, After(2));
        Assert.Equal(TimeSpan.FromMinutes(10), e.Remaining(After(2)));
    }

    [Fact]
    public void ApplySettings_LoweringUnitCount_InShortBreak_GoesToLongBreakNext()
    {
        var e = Engine(sessions: 4);
        e.Start(T0);
        e.Update(After(25));       // work 1 done → short break, session 1
        e.Start(After(25));
        e.Update(After(30));       // → work 2
        e.Start(After(30));
        e.Update(After(55));       // → short break, session 2
        e.Start(After(55));
        e.ApplySettings(new PomodoroSettings { Sessions = 2 }, After(55));
        Assert.Equal(2, e.Session);
        Assert.True(e.Update(After(60)));
        Assert.Equal(PomodoroPhase.LongBreak, e.Phase);
    }

    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(0.2, "00:01")]   // rounded up: never 00:00 while time is left
    [InlineData(59, "00:59")]
    [InlineData(60, "01:00")]
    [InlineData(25 * 60, "25:00")]
    [InlineData(99 * 60, "99:00")]
    public void Format_RoundsUpToSeconds(double seconds, string expected) =>
        Assert.Equal(expected, PomodoroFormat.Timer(TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void Normalize_ClampsPomodoroSettings()
    {
        var s = new AppSettings
        {
            Pomodoro = new PomodoroSettings { WorkMinutes = 0, ShortBreakMinutes = 500, Sessions = 99, LongBreakMinutes = -3 },
        };
        s.Normalize();
        Assert.Equal(1, s.Pomodoro.WorkMinutes);
        Assert.Equal(99, s.Pomodoro.ShortBreakMinutes);
        Assert.Equal(12, s.Pomodoro.Sessions);
        Assert.Equal(1, s.Pomodoro.LongBreakMinutes);
    }

    [Fact]
    public void Normalize_FillsMissingPomodoroSection()
    {
        var s = new AppSettings { Pomodoro = null! };
        s.Normalize();
        Assert.Equal(25, s.Pomodoro.WorkMinutes);
    }
}
