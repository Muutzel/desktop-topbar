using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using TopBar.Core.Services;

namespace TopBar.Platform.Services;

public sealed class AutostartService : IAutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "TopBar";
    private readonly ILogger<AutostartService> _log;

    public AutostartService(ILogger<AutostartService> log) => _log = log;

    private static string ExpectedValue =>
        $"\"{Environment.ProcessPath}\" --autostart";

    public bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
                return key?.GetValue(ValueName) is string;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Autostart: could not read registry");
                return false;
            }
        }
    }

    public void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey, true);
            if (enabled) key.SetValue(ValueName, ExpectedValue, RegistryValueKind.String);
            else key.DeleteValue(ValueName, false);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Autostart: could not write registry");
        }
    }

    public void RepairPathIfNeeded()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            if (key?.GetValue(ValueName) is string current && !string.Equals(current, ExpectedValue, StringComparison.OrdinalIgnoreCase))
            {
                _log.LogInformation("Autostart: path changed, rewriting registry value");
                SetEnabled(true);
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Autostart: repair failed");
        }
    }
}
