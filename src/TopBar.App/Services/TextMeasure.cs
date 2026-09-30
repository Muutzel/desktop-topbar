using Microsoft.UI.Xaml.Controls;

namespace TopBar.App.Services;

internal static class TextMeasure
{
    /// <summary>Width of <paramref name="sample"/> rendered with the font of <paramref name="reference"/> (used for fixed, non-jumping widths).</summary>
    public static double Width(TextBlock reference, string sample)
    {
        var probe = new TextBlock
        {
            Text = sample,
            FontFamily = reference.FontFamily,
            FontSize = reference.FontSize,
            FontWeight = reference.FontWeight,
        };
        probe.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        return Math.Ceiling(probe.DesiredSize.Width);
    }
}
