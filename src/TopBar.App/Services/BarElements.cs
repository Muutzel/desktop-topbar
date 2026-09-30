using TopBar.Core.Settings;

namespace TopBar.App.Services;

/// <summary>Display names and the "show" flags of the placeable bar elements.</summary>
internal static class BarElements
{
    public static string Name(string id) => Loc.Get("Element_" + id);

    /// <summary>False if the element is placed on the bar but switched off on its own settings page (Weather, Desktops, CPU, ...).</summary>
    public static bool IsEnabled(AppSettings s, string id) => id switch
    {
        BarElementIds.Desktops => s.Desktops.Show,
        BarElementIds.Weather => s.Weather.Show,
        BarElementIds.Cpu => s.Stats.ShowCpu,
        BarElementIds.Gpu => s.Stats.ShowGpu,
        BarElementIds.Ram => s.Stats.ShowRam,
        BarElementIds.Network => s.Stats.ShowNetwork,
        _ => true,
    };

    /// <summary>Switches the element's own "show" flag on, so placing it on the bar always makes it appear.</summary>
    public static void Enable(AppSettings s, string id)
    {
        switch (id)
        {
            case BarElementIds.Desktops: s.Desktops.Show = true; break;
            case BarElementIds.Weather: s.Weather.Show = true; break;
            case BarElementIds.Cpu: s.Stats.ShowCpu = true; break;
            case BarElementIds.Gpu: s.Stats.ShowGpu = true; break;
            case BarElementIds.Ram: s.Stats.ShowRam = true; break;
            case BarElementIds.Network: s.Stats.ShowNetwork = true; break;
        }
    }
}
