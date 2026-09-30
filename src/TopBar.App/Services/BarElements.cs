using TopBar.Core.Settings;

namespace TopBar.App.Services;

/// <summary>Display names of the placeable bar elements.</summary>
internal static class BarElements
{
    public static string Name(string id) => Loc.Get("Element_" + id);
}
