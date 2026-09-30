using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TopBar.App.Services;
using TopBar.App.ViewModels;
using TopBar.Core.Services;

namespace TopBar.App.Controls;

public enum ClockPart
{
    Time,
    Date,
}

/// <summary>Either the time or the date as its own bar element (so both can be placed independently).</summary>
public sealed class ClockControl : UserControl
{
    private readonly ClockViewModel _clock;
    private readonly ISettingsService _settings;
    private readonly ClockPart _part;
    private readonly TextBlock _text = new() { VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center };

    public ClockControl(ClockViewModel clock, ISettingsService settings, ClockPart part)
    {
        _clock = clock;
        _settings = settings;
        _part = part;
        VerticalAlignment = VerticalAlignment.Stretch;
        Content = _text;

        Loaded += (_, _) => { _clock.Updated += Refresh; Refresh(); };
        Unloaded += (_, _) => _clock.Updated -= Refresh;
    }

    private void Refresh()
    {
        if (_part == ClockPart.Date)
        {
            _text.Text = _clock.Date;
            return;
        }

        _text.Text = _clock.Time;
        // Fixed width for the time so the digits never make the block jump: measured with the widest digit combination.
        var c = _settings.Current.Clock;
        _text.MinWidth = TextMeasure.Width(_text, c.ShowSeconds
            ? (c.Use24h ? "88:88:88" : "88:88:88 PM")
            : (c.Use24h ? "88:88" : "88:88 PM"));
    }
}
