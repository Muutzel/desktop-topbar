using TopBar.Core.Settings;

namespace TopBar.Core.Tests;

public class LayoutSettingsTests
{
    [Fact]
    public void Defaults_PlaceEveryElementExactlyOnce()
    {
        var l = new LayoutSettings();
        var all = l.Left.Concat(l.Center).Concat(l.Right).ToList();
        Assert.Equal(BarElementIds.All.Count, all.Count);
        Assert.Equal(BarElementIds.All.OrderBy(x => x), all.OrderBy(x => x));
        Assert.Empty(l.Unplaced());
    }

    [Fact]
    public void Place_MovesElementBetweenZonesAtIndex()
    {
        var l = new LayoutSettings();
        l.Place(BarElementIds.Clock, BarZone.Left, 0);
        Assert.Equal(BarElementIds.Clock, l.Left[0]);
        Assert.DoesNotContain(BarElementIds.Clock, l.Center);
        Assert.Equal(BarElementIds.Desktops, l.Left[1]);
    }

    [Fact]
    public void Place_ClampsIndex()
    {
        var l = new LayoutSettings();
        l.Place(BarElementIds.Cpu, BarZone.Center, 99);
        Assert.Equal(BarElementIds.Cpu, l.Center[^1]);
        l.Place(BarElementIds.Gpu, BarZone.Center, -5);
        Assert.Equal(BarElementIds.Gpu, l.Center[0]);
    }

    [Fact]
    public void Place_UnknownId_IsIgnored()
    {
        var l = new LayoutSettings();
        l.Place("Nope", BarZone.Left);
        Assert.DoesNotContain("Nope", l.Left);
    }

    [Fact]
    public void Remove_MakesElementUnplaced_AndPlaceBringsItBack()
    {
        var l = new LayoutSettings();
        l.Remove(BarElementIds.Weather);
        Assert.Equal([BarElementIds.Weather], l.Unplaced());
        l.Place(BarElementIds.Weather, BarZone.Right);
        Assert.Empty(l.Unplaced());
        Assert.Equal(BarElementIds.Weather, l.Right[^1]);
    }

    [Fact]
    public void Normalize_DropsUnknownAndDuplicates_FixesCasing()
    {
        var l = new LayoutSettings
        {
            Left = ["clock", "Bogus", "Clock", "cpu"],
            Center = ["CPU", "date"],
            Right = null!,
        };
        l.Normalize();
        Assert.Equal(["Clock", "Cpu"], l.Left);
        Assert.Equal(["Date"], l.Center);
        Assert.Empty(l.Right);
    }

    [Fact]
    public void IsPlaced_FollowsPlaceAndRemove()
    {
        var l = new LayoutSettings();
        Assert.True(l.IsPlaced(BarElementIds.Gpu));
        l.Remove(BarElementIds.Gpu);
        Assert.False(l.IsPlaced(BarElementIds.Gpu));
        l.Place(BarElementIds.Gpu, BarZone.Center);
        Assert.True(l.IsPlaced(BarElementIds.Gpu));
    }
}
