using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TopBar.App.Services;
using TopBar.App.ViewModels;
using TopBar.Core.Services;
using TopBar.Core.Stats;

namespace TopBar.App.Controls;

/// <summary>Right section: CPU · GPU · RAM · network · gear. Click on a value opens the Task Manager.</summary>
public sealed partial class SystemStatsControl : UserControl
{
    // Icons from the bundled Fluent System Icons font (codepoints from FluentSystemIcons-Regular.json, 20 px regular).
    private const int GlyphCpu = 58540;      // developer_board
    private const int GlyphGpu = 60827;      // top_speed
    private const int GlyphRam = 58463;      // data_usage
    private const int GlyphNetwork = 62554;  // globe

    private sealed class StatItem
    {
        public required Button Button { get; init; }
        public required TextBlock Label { get; init; }
        public required TextBlock Value { get; init; }
        public required StatEntry Entry { get; init; }
        public required string WidestSample { get; init; }
    }

    private readonly StatsViewModel _vm;
    private readonly List<StatItem> _items = new();

    public SystemStatsControl(StatsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        var index = 0;
        Add(vm.Cpu, "CPU", GlyphCpu, "100 %", index++);
        Add(vm.Gpu, "GPU", GlyphGpu, "100 %", index++);
        Add(vm.Ram, "RAM", GlyphRam, "100 %", index++);
        Add(vm.Network, "", GlyphNetwork, "↓ 999,9 MB/s  ↑ 999,9 MB/s", index);

        Loaded += (_, _) => { _vm.Updated += Refresh; Refresh(); };
        Unloaded += (_, _) => _vm.Updated -= Refresh;
        ActualThemeChanged += (_, _) => Refresh();
    }

    private void Add(StatEntry entry, string label, int glyph, string widest, int position)
    {
        var icon = new TextBlock
        {
            Text = char.ConvertFromUtf32(glyph),
            FontFamily = (FontFamily)Application.Current.Resources["FluentIconsFont"],
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var labelBlock = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Resources["SecondaryTextBrush"],
        };
        var value = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right };

        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        content.Children.Add(icon);
        content.Children.Add(labelBlock);
        content.Children.Add(value);

        var button = new Button
        {
            Style = (Style)Application.Current.Resources["BarButtonStyle"],
            Margin = new Thickness(0, 4, 0, 4),
            Content = content,
        };
        button.Click += (_, _) => OpenTaskManager();

        Row.Children.Insert(position, button);
        _items.Add(new StatItem { Button = button, Label = labelBlock, Value = value, Entry = entry, WidestSample = widest });
    }

    private void Refresh()
    {
        foreach (var item in _items)
        {
            var e = item.Entry;
            item.Button.Visibility = e.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            item.Label.Visibility = _vm.ShowLabels && !string.IsNullOrEmpty(item.Label.Text) ? Visibility.Visible : Visibility.Collapsed;
            item.Value.Text = e.Text;
            item.Value.MinWidth = MeasureWidth(item.Value, item.WidestSample); // fixed width: no jumping
            ToolTipService.SetToolTip(item.Button, e.Tooltip);

            switch (e.Level)
            {
                case StatFormat.Level.Critical: item.Value.Foreground = (Brush)Resources["CriticalBrush"]; break;
                case StatFormat.Level.Warning: item.Value.Foreground = (Brush)Resources["WarnBrush"]; break;
                default: item.Value.ClearValue(TextBlock.ForegroundProperty); break;
            }
        }
    }

    private static double MeasureWidth(TextBlock reference, string sample)
    {
        var probe = new TextBlock { Text = sample, FontFamily = reference.FontFamily, FontSize = reference.FontSize };
        probe.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        return Math.Ceiling(probe.DesiredSize.Width);
    }

    private static void OpenTaskManager()
    {
        try { Process.Start(new ProcessStartInfo("taskmgr.exe") { UseShellExecute = true }); }
        catch (Exception ex) { AppController.LogWarning(ex, "Could not start Task Manager"); }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e) => AppController.OpenSettings();
}
