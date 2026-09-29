using TopBar.Core.Layout;

namespace TopBar.Core.Tests;

public class ThreeSectionLayoutTests
{
    [Fact]
    public void Symmetric_CenterIsCentered()
    {
        var r = ThreeSectionLayout.Compute(1000, 100, 200, 100);
        Assert.Equal(400, r.Center.X);
        Assert.Equal(200, r.Center.Width);
        Assert.Equal(0, r.Left.X);
        Assert.Equal(100, r.Left.Width);
        Assert.Equal(900, r.Right.X);
        Assert.Equal(100, r.Right.Width);
    }

    [Fact]
    public void Asymmetric_CenterStaysExactlyCentered()
    {
        var r = ThreeSectionLayout.Compute(1000, 50, 200, 380);
        Assert.Equal(400, r.Center.X);
        Assert.Equal(50, r.Left.Width);
        Assert.Equal(380, r.Right.Width);
        Assert.Equal(620, r.Right.X);
    }

    [Fact]
    public void LeftTooWide_IsLimitedByGap()
    {
        var r = ThreeSectionLayout.Compute(1000, 600, 200, 100);
        Assert.Equal(400 - 16, r.Left.Width);
        Assert.Equal(400, r.Center.X);
    }

    [Fact]
    public void RightTooWide_IsLimitedByGap()
    {
        var r = ThreeSectionLayout.Compute(1000, 100, 200, 900);
        Assert.Equal(400 - 16, r.Right.Width);
        Assert.Equal(1000, r.Right.X + r.Right.Width);
    }

    [Fact]
    public void CenterWiderThanTotal_FillsAndSidesCollapse()
    {
        var r = ThreeSectionLayout.Compute(500, 100, 800, 100);
        Assert.Equal(0, r.Center.X);
        Assert.Equal(500, r.Center.Width);
        Assert.Equal(0, r.Left.Width);
        Assert.Equal(0, r.Right.Width);
    }
}
