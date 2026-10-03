using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TopBar.App.Services;
using TopBar.Core.Services;
using TopBar.Core.Settings;

namespace TopBar.App.SettingsPages;

public sealed partial class InfoPage : Page
{
    public InfoPage()
    {
        InitializeComponent();
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "?";
        AppNameText.Text = "TopBar";
        VersionText.Text = string.Format(Loc.Get("Info_Version"), version);
    }

    private void OpenLogsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LogDir);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{AppPaths.LogDir}\"") { UseShellExecute = true });
        }
        catch (Exception ex) { AppController.LogWarning(ex, "Could not open log folder"); }
    }

    private async void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Loc.Get("Info_Reset_Title"),
            Content = Loc.Get("Info_Reset_Message"),
            PrimaryButtonText = Loc.Get("Info_Reset_Confirm"),
            CloseButtonText = Loc.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        AppController.Services.GetRequiredService<ISettingsService>().Update(s =>
        {
            var features = s.Features; // machine-level switches (edited by hand in settings.json) survive a reset
            s.CopyFrom(new AppSettings());
            s.Features = features;
        });
    }
}
