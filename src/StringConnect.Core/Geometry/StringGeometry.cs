using StringConnect.Core.Models;

namespace StringConnect.Core.Geometry;

public readonly record struct Point2D(double X, double Y);

public readonly record struct StringCurve(Point2D Start, Point2D Control1, Point2D Control2, Point2D End);

/// <summary>
/// 두 문자열 영역 사이를 잇는 끈 형태의 3차 베지어 곡선을 계산한다. 실을 눈으로 볼 때 자연스럽게
/// 느껴지도록 두 영역의 중심 높이 사이에 약간의 처짐(sag)을 준다. 시작/끝 점은 상대 영역이 놓인
/// 방향에 따라 자신의 좌/우 변 중 마주 보는 쪽 변의 중앙에서 나가도록 한다.
/// </summary>
public static class StringGeometry
{
    private const double SagFactor = 0.15;

    public static StringCurve BuildCurve(ScreenRect from, ScreenRect to)
    {
        var start = ExitPoint(from, to);
        var end = ExitPoint(to, from);

        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        var sag = distance * SagFactor;

        var control1 = new Point2D(start.X + dx / 3, start.Y + dy / 3 + sag);
        var control2 = new Point2D(start.X + dx * 2 / 3, start.Y + dy * 2 / 3 + sag);

        return new StringCurve(start, control1, control2, end);
    }

    private static Point2D ExitPoint(ScreenRect self, ScreenRect other)
    {
        var goingRight = other.CenterX >= self.CenterX;
        var x = goingRight ? self.Right : self.X;
        return new Point2D(x, self.CenterY);
    }
}
