using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TopBar.Core.Services;
using TopBar.Core.Settings;

namespace TopBar.App.Services;

/// <summary>Shared behavior of the settings pages: guarded loading, immediate and debounced (300 ms) saving.</summary>
internal sealed class PageState
{
    private readonly Debouncer _debounce = new(300);

    public ISettingsService Settings { get; } = AppController.Services.GetRequiredService<ISettingsService>();
    public bool Loading { get; private set; }

    /// <summary>Runs <paramref name="load"/> with change handlers suppressed.</summary>
    public void Load(Action load)
    {
        Loading = true;
        try { load(); }
        finally { Loading = false; }
    }

    public void Save(Action<AppSettings> change)
    {
        if (!Loading) Settings.Update(change);
    }

    public void SaveDebounced(Action<AppSettings> change)
    {
        if (!Loading) _debounce.Run(() => Settings.Update(change));
    }

    public void Flush() => _debounce.Flush();

    /// <summary>Gives every ToggleSwitch below <paramref name="root"/> localized "Ein"/"Aus" labels.</summary>
    public static void LocalizeToggles(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ToggleSwitch t)
            {
                t.OnContent = Loc.Get("Common_On");
                t.OffContent = Loc.Get("Common_Off");
            }
            LocalizeToggles(child);
        }
    }
}
