namespace TopBar.Core.Settings;

public enum BarZone
{
    Left,
    Center,
    Right,
}

/// <summary>Stable ids of all elements that can be placed on the bar.</summary>
public static class BarElementIds
{
    public const string Desktops = "Desktops";
    public const string Pomodoro = "Pomodoro";
    public const string Weather = "Weather";
    public const string Clock = "Clock";
    public const string Date = "Date";
    public const string Cpu = "Cpu";
    public const string Gpu = "Gpu";
    public const string Ram = "Ram";
    public const string Network = "Network";
    public const string SettingsButton = "Settings";

    public static readonly IReadOnlyList<string> All =
    [
        Desktops, Pomodoro, Weather, Clock, Date, Cpu, Gpu, Ram, Network, SettingsButton,
    ];
}

/// <summary>Which element sits in which zone of the bar, in display order (left to right).</summary>
public sealed class LayoutSettings
{
    public List<string> Left { get; set; } = [BarElementIds.Desktops, BarElementIds.Pomodoro];
    public List<string> Center { get; set; } = [BarElementIds.Weather, BarElementIds.Clock, BarElementIds.Date];
    public List<string> Right { get; set; } = [BarElementIds.Cpu, BarElementIds.Gpu, BarElementIds.Ram, BarElementIds.Network, BarElementIds.SettingsButton];

    public List<string> Zone(BarZone zone) => zone switch
    {
        BarZone.Left => Left,
        BarZone.Center => Center,
        _ => Right,
    };

    /// <summary>Elements that are currently in no zone (hidden from the bar).</summary>
    public List<string> Unplaced()
    {
        var placed = new HashSet<string>(Left.Concat(Center).Concat(Right), StringComparer.Ordinal);
        return BarElementIds.All.Where(id => !placed.Contains(id)).ToList();
    }

    /// <summary>Puts <paramref name="id"/> into <paramref name="zone"/> at <paramref name="index"/> (clamped). Moves it if it is placed already.</summary>
    public void Place(string id, BarZone zone, int index = int.MaxValue)
    {
        if (!BarElementIds.All.Contains(id)) return;
        Remove(id);
        var list = Zone(zone);
        list.Insert(Math.Clamp(index, 0, list.Count), id);
    }

    public void Remove(string id)
    {
        Left.RemoveAll(x => x == id);
        Center.RemoveAll(x => x == id);
        Right.RemoveAll(x => x == id);
    }

    /// <summary>Drops null/unknown ids and duplicates (the first occurrence wins, zones are read Left, Center, Right) and fixes casing.</summary>
    public void Normalize()
    {
        Left ??= [];
        Center ??= [];
        Right ??= [];

        var seen = new HashSet<string>(StringComparer.Ordinal);
        Clean(Left, seen);
        Clean(Center, seen);
        Clean(Right, seen);
    }

    private static void Clean(List<string> list, HashSet<string> seen)
    {
        var cleaned = new List<string>();
        foreach (var raw in list)
        {
            var id = BarElementIds.All.FirstOrDefault(a => string.Equals(a, raw, StringComparison.OrdinalIgnoreCase));
            if (id is not null && seen.Add(id)) cleaned.Add(id);
        }
        list.Clear();
        list.AddRange(cleaned);
    }
}
