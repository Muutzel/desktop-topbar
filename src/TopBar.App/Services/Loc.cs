using Microsoft.Windows.ApplicationModel.Resources;

namespace TopBar.App.Services;

/// <summary>UI strings from Strings/de-DE/Resources.resw. Falls back to the key so a missing string is visible but harmless.</summary>
internal static class Loc
{
    private static readonly ResourceLoader? Loader = Create();

    private static ResourceLoader? Create()
    {
        try { return new ResourceLoader(); }
        catch { return null; }
    }

    public static string Get(string key)
    {
        try
        {
            var value = Loader?.GetString(key);
            return string.IsNullOrEmpty(value) ? key : value;
        }
        catch
        {
            return key;
        }
    }
}
