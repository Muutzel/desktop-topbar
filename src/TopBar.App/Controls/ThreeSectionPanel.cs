using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using TopBar.Core.Layout;

namespace TopBar.App.Controls;

/// <summary>
/// Exactly three children (left, center, right). The center child is placed exactly in the middle of the panel,
/// independent of how wide the side children are. Overflowing side content is clipped, not wrapped.
/// </summary>
public sealed class ThreeSectionPanel : Panel
{
    public double Gap { get; set; } = ThreeSectionLayout.DefaultGap;

    protected override Size MeasureOverride(Size availableSize)
    {
        double sum = 0, max = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            sum += child.DesiredSize.Width;
            max = Math.Max(max, child.DesiredSize.Height);
        }
        return new Size(
            double.IsInfinity(availableSize.Width) ? sum : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? max : availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count < 3) return finalSize;

        var left = Children[0];
        var center = Children[1];
        var right = Children[2];

        var layout = ThreeSectionLayout.Compute(finalSize.Width, left.DesiredSize.Width, center.DesiredSize.Width, right.DesiredSize.Width, Gap);
        Place(left, layout.Left, finalSize.Height);
        Place(center, layout.Center, finalSize.Height);
        Place(right, layout.Right, finalSize.Height);
        return finalSize;
    }

    private static void Place(UIElement element, SectionSlot slot, double height)
    {
        element.Arrange(new Rect(slot.X, 0, slot.Width, height));
        element.Clip = new RectangleGeometry { Rect = new Rect(0, 0, slot.Width, height) };
    }
}
