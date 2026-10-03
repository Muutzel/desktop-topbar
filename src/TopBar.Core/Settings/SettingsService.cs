using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TopBar.Core.Services;

namespace TopBar.Core.Settings;

/// <summary>JSON-file backed settings. Atomic writes, broken files are renamed and replaced by defaults.</summary>
public sealed class SettingsService : ISettingsService
{
    private readonly string _path;
    private readonly ILogger _log;
    private readonly object _lock = new();

    public AppSettings Current { get; private set; }
    public event EventHandler<AppSettings>? Changed;

    public SettingsService(string path, ILogger<SettingsService>? log = null)
    {
        _path = path;
        _log = (ILogger?)log ?? NullLogger.Instance;
        Current = Load();
    }

    public void Update(Action<AppSettings> change)
    {
        lock (_lock)
        {
            change(Current);
            Current.Normalize();
            Save(Current);
        }
        Changed?.Invoke(this, Current);
    }

    private AppSettings Load()
    {
        if (!File.Exists(_path))
        {
            var defaults = new AppSettings();
            defaults.Normalize();
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(_path, Encoding.UTF8);
            var loaded = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.AppSettings)
                         ?? throw new JsonException("settings.json is empty");
            loaded.Normalize();
            // Write the file back if normalizing changed anything (new keys such as "Features" appear, invalid values are fixed).
            if (!string.Equals(Serialize(loaded), json, StringComparison.Ordinal)) Save(loaded);
            return loaded;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _log.LogWarning(ex, "Settings: file is broken, loading defaults");
            try
            {
                var dir = Path.GetDirectoryName(_path)!;
                var backup = Path.Combine(dir, $"settings.broken-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                File.Move(_path, backup, overwrite: true);
            }
            catch (Exception moveEx)
            {
                _log.LogWarning(moveEx, "Settings: could not back up broken file");
            }
            var defaults = new AppSettings();
            defaults.Normalize();
            Save(defaults);
            return defaults;
        }
    }

    private static string Serialize(AppSettings settings) => JsonSerializer.Serialize(settings, SettingsJsonContext.Default.AppSettings);

    private void Save(AppSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(_path)!;
            Directory.CreateDirectory(dir);
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, Serialize(settings), new UTF8Encoding(false));
            File.Move(tmp, _path, overwrite: true);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Settings: saving failed");
        }
    }
}
