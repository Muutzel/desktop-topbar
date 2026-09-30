using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TopBar.App.Services;
using TopBar.App.ViewModels;
using TopBar.Core.Stats;

namespace TopBar.App.Controls;

/// <summary>Right section: CPU · GPU · RAM · network · gear. Click on a value opens the Task Manager.</summary>
public enum StatKind
{
    Cpu,
    Gpu,
    Ram,
    Network,
}

/// <summary>One system value as its own bar element: CPU, GPU, RAM or network. Click opens the Task Manager.</summary>
public sealed partial class StatControl : UserControl
{
    // Icons from the bundled Fluent System Icons font (codepoints from FluentSystemIcons-Regular.json, 20 px regular).
    private const int GlyphCpu = 58540;      // developer_board
    private const int GlyphGpu = 60827;      // top_speed
    private const int GlyphRam = 58463;      // data_usage
    private const int GlyphNetwork = 62554;  // globe

    private readonly StatsViewModel _vm;
    private readonly StatEntry _entry;
    private readonly string _widestSample;
    private readonly Button _button;
    private readonly TextBlock _label;
    private readonly TextBlock? _value;
    /// <summary>Table mode (network): number and unit cells (down number, down unit, up number, up unit).</summary>
    private readonly TextBlock[]? _cells;

    public StatControl(StatsViewModel vm, StatKind kind)
    {
        InitializeComponent();
        _vm = vm;

        var (entry, label, glyph, widest) = kind switch
        {
            StatKind.Cpu => (vm.Cpu, "CPU", GlyphCpu, "88 %"),
            StatKind.Gpu => (vm.Gpu, "GPU", GlyphGpu, "88 %"),
            StatKind.Ram => (vm.Ram, "RAM", GlyphRam, "88 %"),
            _ => (vm.Network, "", GlyphNetwork, ""),
        };
        _entry = entry;
        _widestSample = widest;

        var icon = new TextBlock
        {
            Text = char.ConvertFromUtf32(glyph),
            FontFamily = (FontFamily)Application.Current.Resources["FluentIconsFont"],
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _label = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Resources["SecondaryTextBrush"],
        };
        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        content.Children.Add(icon);
        content.Children.Add(_label);

        if (kind == StatKind.Network)
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
            _cells = [downNumber, downUnit, upNumber, upUnit];
            content.Children.Add(grid);
        }
        else
        {
            _value = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Left };
            content.Children.Add(_value);
        }

        _button = new Button
        {
            Style = (Style)Application.Current.Resources["BarButtonStyle"],
            Margin = new Thickness(0, 4, 0, 4),
            Padding = new Thickness(6, 0, 6, 0),
            Content = content,
        };
        _button.Click += (_, _) => OpenTaskManager();
        Host.Content = _button;

        Loaded += (_, _) => { _vm.Updated += Refresh; Refresh(); };
        Unloaded += (_, _) => _vm.Updated -= Refresh;
        ActualThemeChanged += (_, _) => Refresh();
    }

    private void Refresh()
    {
        var e = _entry;
        _button.Visibility = e.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        _label.Visibility = _vm.ShowLabels && !string.IsNullOrEmpty(_label.Text) ? Visibility.Visible : Visibility.Collapsed;
        if (_cells is { } cells)
        {
            for (var i = 0; i < cells.Length; i++) cells[i].Text = i < e.Cells.Length ? e.Cells[i] : "";
            var numberWidth = MeasureWidth(cells[0], "0000");
            var unitWidth = e.UnitSamples.Length == 0 ? 0 : e.UnitSamples.Max(u => MeasureWidth(cells[1], u));
            cells[0].MinWidth = cells[2].MinWidth = numberWidth; // fixed columns: no jumping
            cells[1].MinWidth = cells[3].MinWidth = unitWidth;
        }
        else if (_value is { } value)
        {
            value.Text = e.Text;
            value.MinWidth = MeasureWidth(value, _widestSample); // fixed width: no jumping
            switch (e.Level)
            {
                case StatFormat.Level.Critical: value.Foreground = (Brush)Resources["CriticalBrush"]; break;
                case StatFormat.Level.Warning: value.Foreground = (Brush)Resources["WarnBrush"]; break;
                default: value.ClearValue(TextBlock.ForegroundProperty); break;
            }
        }
        ToolTipService.SetToolTip(_button, e.Tooltip);
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
}
