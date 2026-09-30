using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TopBar.App.Services;

namespace TopBar.App.Controls;

/// <summary>The gear button that opens the settings window.</summary>
public sealed class SettingsButtonControl : UserControl
{
    public SettingsButtonControl()
    {
        VerticalAlignment = VerticalAlignment.Stretch;
        var button = new Button
        {
            Style = (Style)Application.Current.Resources["BarButtonStyle"],
            Margin = new Thickness(0, 4, 0, 4),
            Content = new FontIcon
            {
                FontFamily = (Microsoft.UI.Xaml.Media.FontFamily)Application.Current.Resources["SegoeIconsFont"],
                Glyph = "",
                FontSize = 14,
            },
        };
        button.Click += (_, _) => AppController.OpenSettings();
        Content = button;
    }
}
