namespace StringConnect.Core.Models;

/// <summary>
/// 화면 좌표계에서 문자열이 차지하는 영역을 나타낸다. WPF의 Rect 등 특정 UI 프레임워크 타입에
/// 의존하지 않도록 Core 계층에서 독립적으로 정의한 값 형식이다.
/// </summary>
public readonly record struct ScreenRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public double CenterX => X + Width / 2;
    public double CenterY => Y + Height / 2;
}
