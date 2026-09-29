namespace TopBar.App.Services;

internal static class AppPaths
{
    public static string DataDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TopBar");
    public static string SettingsFile { get; } = Path.Combine(DataDir, "settings.json");
    public static string LogDir { get; } = Path.Combine(DataDir, "logs");
}
