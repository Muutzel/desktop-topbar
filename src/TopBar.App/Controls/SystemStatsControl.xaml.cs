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
        public TextBlock? Value { get; init; }
        /// <summary>Table mode: number and unit cells (down number, down unit, up number, up unit).</summary>
        public TextBlock[]? Cells { get; init; }
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
        Add(vm.Cpu, "CPU", GlyphCpu, "88 %", index++);
        Add(vm.Gpu, "GPU", GlyphGpu, "88 %", index++);
        Add(vm.Ram, "RAM", GlyphRam, "88 %", index++);
        Add(vm.Network, "", GlyphNetwork, "", index, table: true);

        Loaded += (_, _) => { _vm.Updated += Refresh; Refresh(); };
        Unloaded += (_, _) => _vm.Updated -= Refresh;
        ActualThemeChanged += (_, _) => Refresh();
    }

    private void Add(StatEntry entry, string label, int glyph, string widest, int position, bool table = false)
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
        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        content.Children.Add(icon);
        content.Children.Add(labelBlock);

        TextBlock? value = null;
        TextBlock[]? cells = null;
        if (table)
        {
            // Fixed columns: ↓ number unit   ↑ number unit. Numbers right-aligned, units left-aligned.
            var grid = new Grid { ColumnSpacing = 4, VerticalAlignment = VerticalAlignment.Center };
            for (var i = 0; i < 6; i++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            TextBlock Cell(string text, int column, TextAlignment align, bool secondary = false)
            {
                var tb = new TextBlock { Text = text, TextAlignment = align, VerticalAlignment = VerticalAlignment.Center };
                if (secondary) tb.Foreground = (Brush)Resources["SecondaryTextBrush"];
                Grid.SetColumn(tb, column);
                grid.Children.Add(tb);
                return tb;
            }
            Cell("↓", 0, TextAlignment.Left, secondary: true);
            var downNumber = Cell("", 1, TextAlignment.Right);
            var downUnit = Cell("", 2, TextAlignment.Left);
            var upArrow = Cell("↑", 3, TextAlignment.Left, secondary: true);
            upArrow.Margin = new Thickness(8, 0, 0, 0);
            var upNumber = Cell("", 4, TextAlignment.Right);
            var upUnit = Cell("", 5, TextAlignment.Left);
            cells = [downNumber, downUnit, upNumber, upUnit];
            content.Children.Add(grid);
        }
        else
        {
            value = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Left };
            content.Children.Add(value);
        }

        var button = new Button
        {
            Style = (Style)Application.Current.Resources["BarButtonStyle"],
            Margin = new Thickness(0, 4, 0, 4),
            Padding = new Thickness(6, 0, 6, 0),
            Content = content,
        };
        button.Click += (_, _) => OpenTaskManager();

        Row.Children.Insert(position, button);
        _items.Add(new StatItem { Button = button, Label = labelBlock, Value = value, Cells = cells, Entry = entry, WidestSample = widest });
    }

    private void Refresh()
    {
        foreach (var item in _items)
        {
            var e = item.Entry;
            item.Button.Visibility = e.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            item.Label.Visibility = _vm.ShowLabels && !string.IsNullOrEmpty(item.Label.Text) ? Visibility.Visible : Visibility.Collapsed;
            if (item.Cells is { } cells)
            {
                for (var i = 0; i < cells.Length; i++) cells[i].Text = i < e.Cells.Length ? e.Cells[i] : "";
                var numberWidth = MeasureWidth(cells[0], "0000");
                var unitWidth = e.UnitSamples.Length == 0 ? 0 : e.UnitSamples.Max(u => MeasureWidth(cells[1], u));
                cells[0].MinWidth = cells[2].MinWidth = numberWidth; // fixed columns: no jumping
                cells[1].MinWidth = cells[3].MinWidth = unitWidth;
            }
            else if (item.Value is { } value)
            {
                value.Text = e.Text;
                value.MinWidth = MeasureWidth(value, item.WidestSample); // fixed width: no jumping
                switch (e.Level)
                {
                    case StatFormat.Level.Critical: value.Foreground = (Brush)Resources["CriticalBrush"]; break;
                    case StatFormat.Level.Warning: value.Foreground = (Brush)Resources["WarnBrush"]; break;
                    default: value.ClearValue(TextBlock.ForegroundProperty); break;
                }
            }
            ToolTipService.SetToolTip(item.Button, e.Tooltip);
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
