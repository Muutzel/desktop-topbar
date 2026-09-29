using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TopBar.App.Controls;

/// <summary>Header + slider + current value text. Raises <see cref="ValueChanged"/> for user and programmatic changes.</summary>
public sealed class SliderRow : UserControl
{
    private readonly TextBlock _header = new();
    private readonly Slider _slider = new() { Width = 320, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly TextBlock _value = new() { VerticalAlignment = VerticalAlignment.Center, MinWidth = 56 };

    public SliderRow()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        row.Children.Add(_slider);
        row.Children.Add(_value);

        var root = new StackPanel { Spacing = 4 };
        root.Children.Add(_header);
        root.Children.Add(row);
        Content = root;

        _slider.ValueChanged += (_, e) =>
        {
            UpdateText();
            ValueChanged?.Invoke(e.NewValue);
        };
    }

    public event Action<double>? ValueChanged;

    public string Header { get => _header.Text; set => _header.Text = value; }
    public double Minimum { get => _slider.Minimum; set => _slider.Minimum = value; }
    public double Maximum { get => _slider.Maximum; set => _slider.Maximum = value; }
    public double Step { get => _slider.StepFrequency; set { _slider.StepFrequency = value; _slider.SmallChange = value; } }
    public double Value { get => _slider.Value; set { _slider.Value = value; UpdateText(); } }
    /// <summary>Text appended to the value (e.g. " px" or " %").</summary>
    public string Suffix { get; set; } = "";
    /// <summary>Optional multiplier for the shown value (e.g. 100 for 0..1 → percent).</summary>
    public double DisplayScale { get; set; } = 1;

    private void UpdateText() => _value.Text = $"{Math.Round(_slider.Value * DisplayScale)}{Suffix}";
}
