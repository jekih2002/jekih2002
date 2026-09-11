using StringConnect.Core.Geometry;
using StringConnect.Core.Models;
using Xunit;

namespace StringConnect.Core.Tests;

public class StringGeometryTests
{
    [Fact]
    public void BuildCurve_ExitsFromFacingEdges_WhenTargetIsToTheRight()
    {
        var from = new ScreenRect(0, 100, 200, 40);
        var to = new ScreenRect(500, 100, 200, 40);

        var curve = StringGeometry.BuildCurve(from, to);

        Assert.Equal(from.Right, curve.Start.X, 3);
        Assert.Equal(to.X, curve.End.X, 3);
    }

    [Fact]
    public void BuildCurve_ExitsFromFacingEdges_WhenTargetIsToTheLeft()
    {
        var from = new ScreenRect(500, 100, 200, 40);
        var to = new ScreenRect(0, 100, 200, 40);

        var curve = StringGeometry.BuildCurve(from, to);

        Assert.Equal(from.X, curve.Start.X, 3);
        Assert.Equal(to.Right, curve.End.X, 3);
    }

    [Fact]
    public void BuildCurve_SagsDownward_ForVisualCatenaryEffect()
    {
        var from = new ScreenRect(0, 100, 200, 40);
        var to = new ScreenRect(500, 100, 200, 40);

        var curve = StringGeometry.BuildCurve(from, to);
        var straightLineY = (curve.Start.Y + curve.End.Y) / 2;

        Assert.True(curve.Control1.Y > straightLineY);
        Assert.True(curve.Control2.Y > straightLineY);
    }
}
