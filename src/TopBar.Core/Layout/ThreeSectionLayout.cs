namespace TopBar.Core.Layout;

public readonly record struct SectionSlot(double X, double Width);
public readonly record struct ThreeSectionResult(SectionSlot Left, SectionSlot Center, SectionSlot Right);

/// <summary>Pure layout math for the three bar sections. The center is exactly centered regardless of the side widths.</summary>
public static class ThreeSectionLayout
{
    public const double DefaultGap = 16;

    public static ThreeSectionResult Compute(double totalWidth, double left, double center, double right, double gap = DefaultGap)
    {
        var w = Math.Max(0, totalWidth);

        double centerWidth, centerX;
        if (center > w) { centerWidth = w; centerX = 0; }
        else { centerWidth = Math.Max(0, center); centerX = (w - centerWidth) / 2; }

        var leftWidth = Math.Max(0, Math.Min(left, centerX - gap));

        var rightSpace = w - (centerX + centerWidth) - gap;
        var rightWidth = Math.Max(0, Math.Min(right, rightSpace));
        var rightX = w - rightWidth;

        return new ThreeSectionResult(new SectionSlot(0, leftWidth), new SectionSlot(centerX, centerWidth), new SectionSlot(rightX, rightWidth));
    }
}
