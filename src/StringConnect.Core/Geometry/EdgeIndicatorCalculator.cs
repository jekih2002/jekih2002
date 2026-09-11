using StringConnect.Core.Models;

namespace StringConnect.Core.Geometry;

public readonly record struct EdgeIndicator(Point2D Position, double DirectionAngleDegrees);

/// <summary>
/// 연결 대상 문자열이 현재 화면 영역 밖에 있을 때, 화면 가장자리에 표시할 마커의 위치를 계산한다.
/// 기준점(reference)에서 대상점(anchor)으로 향하는 직선이 화면 경계와 만나는 지점을 구하고,
/// 경계에 바짝 붙지 않도록 일정한 여백(margin)만큼 안쪽으로 당긴다. 대상점이 이미 화면 안에 있으면
/// null을 반환하여 별도의 마커가 필요 없음을 알린다.
/// </summary>
public static class EdgeIndicatorCalculator
{
    private const double Margin = 12;

    public static EdgeIndicator? Calculate(ScreenRect screenBounds, Point2D anchorPoint, Point2D referencePoint)
    {
        var isInside = anchorPoint.X >= screenBounds.X && anchorPoint.X <= screenBounds.Right &&
                        anchorPoint.Y >= screenBounds.Y && anchorPoint.Y <= screenBounds.Bottom;
        if (isInside) return null;

        var dx = anchorPoint.X - referencePoint.X;
        var dy = anchorPoint.Y - referencePoint.Y;
        var angle = Math.Atan2(dy, dx);

        var minX = screenBounds.X + Margin;
        var maxX = screenBounds.Right - Margin;
        var minY = screenBounds.Y + Margin;
        var maxY = screenBounds.Bottom - Margin;

        var t = double.PositiveInfinity;
        if (dx > 0) t = Math.Min(t, (maxX - referencePoint.X) / dx);
        else if (dx < 0) t = Math.Min(t, (minX - referencePoint.X) / dx);
        if (dy > 0) t = Math.Min(t, (maxY - referencePoint.Y) / dy);
        else if (dy < 0) t = Math.Min(t, (minY - referencePoint.Y) / dy);

        if (double.IsInfinity(t) || t < 0) t = 0;

        var x = Math.Clamp(referencePoint.X + dx * t, minX, maxX);
        var y = Math.Clamp(referencePoint.Y + dy * t, minY, maxY);

        return new EdgeIndicator(new Point2D(x, y), angle * 180 / Math.PI);
    }
}
