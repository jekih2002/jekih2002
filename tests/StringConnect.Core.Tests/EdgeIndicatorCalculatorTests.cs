using StringConnect.Core.Geometry;
using StringConnect.Core.Models;
using Xunit;

namespace StringConnect.Core.Tests;

public class EdgeIndicatorCalculatorTests
{
    private static readonly ScreenRect Screen = new(0, 0, 1920, 1080);

    [Fact]
    public void PointInsideScreen_ReturnsNull()
    {
        var result = EdgeIndicatorCalculator.Calculate(Screen, new Point2D(960, 540), new Point2D(500, 500));
        Assert.Null(result);
    }

    [Fact]
    public void PointFarToTheRight_ClampsToRightEdge()
    {
        var reference = new Point2D(960, 540);
        var anchor = new Point2D(5000, 540);

        var indicator = EdgeIndicatorCalculator.Calculate(Screen, anchor, reference);

        Assert.NotNull(indicator);
        Assert.True(indicator!.Value.Position.X <= Screen.Right);
        Assert.True(indicator.Value.Position.X > Screen.Right - 50);
    }

    [Fact]
    public void PointAboveScreen_ClampsToTopEdge()
    {
        var reference = new Point2D(960, 540);
        var anchor = new Point2D(960, -3000);

        var indicator = EdgeIndicatorCalculator.Calculate(Screen, anchor, reference);

        Assert.NotNull(indicator);
        Assert.True(indicator!.Value.Position.Y < 50);
    }

    [Fact]
    public void PointBelowScreen_ClampsToBottomEdge()
    {
        var reference = new Point2D(960, 540);
        var anchor = new Point2D(960, 5000);

        var indicator = EdgeIndicatorCalculator.Calculate(Screen, anchor, reference);

        Assert.NotNull(indicator);
        Assert.True(indicator!.Value.Position.Y > Screen.Bottom - 50);
    }
}
