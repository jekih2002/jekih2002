using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using StringConnect.Core.Geometry;
using StringConnect.Core.Models;
using StringConnect.Windows.Native;
using Point = System.Windows.Point;

namespace StringConnect.Windows;

/// <summary>
/// 화면 전체를 덮는 투명 오버레이 창이다. 평상시에는 WS_EX_TRANSPARENT를 설정하여 마우스 클릭과
/// 키보드 입력이 아래에 있는 실제 애플리케이션으로 그대로 전달되도록 하고, 연결을 그리는 역할만
/// 수행한다. 트레이 메뉴의 "편집 모드"로 전환하면 클릭스루가 해제되어 끈을 직접 클릭할 수 있다.
/// </summary>
public sealed class VisualConnection
{
    public required Connection Connection { get; init; }
    public required string Color { get; init; }
    public ScreenRect? FromRect { get; init; }
    public ScreenRect? ToRect { get; init; }
    public ScreenRect? FromLastKnown { get; init; }
    public ScreenRect? ToLastKnown { get; init; }
}

public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => SetClickThrough(true);
        Left = 0;
        Top = 0;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    public void SetClickThrough(bool enabled)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var exStyle = NativeMethods.GetWindowLong(handle, NativeMethods.GWL_EXSTYLE);

        exStyle = enabled
            ? exStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED
            : (exStyle | NativeMethods.WS_EX_LAYERED) & ~NativeMethods.WS_EX_TRANSPARENT;

        NativeMethods.SetWindowLong(handle, NativeMethods.GWL_EXSTYLE, exStyle);
    }

    public void Render(IReadOnlyList<VisualConnection> connections)
    {
        DrawingSurface.Children.Clear();

        foreach (var item in connections)
        {
            if (item.FromRect is { } fromRect && item.ToRect is { } toRect)
            {
                DrawStrand(fromRect, toRect, item.Color, item.Connection.Note);
                continue;
            }

            if (item.FromRect is { } visibleFrom)
            {
                DrawEdgeIndicator(visibleFrom, item.ToLastKnown, item.Color);
            }
            else if (item.ToRect is { } visibleTo)
            {
                DrawEdgeIndicator(visibleTo, item.FromLastKnown, item.Color);
            }
            // 양쪽 모두 화면에 없는 경우(두 창이 모두 닫혀 있거나 최소화된 경우)에는
            // 연결관계 자체는 저장소에 유지한 채, 이번 갱신에서는 아무것도 그리지 않는다.
        }
    }

    private void DrawStrand(ScreenRect from, ScreenRect to, string colorHex, string note)
    {
        var curve = StringGeometry.BuildCurve(from, to);
        var figure = new PathFigure { StartPoint = ToPoint(curve.Start) };
        figure.Segments.Add(new BezierSegment(ToPoint(curve.Control1), ToPoint(curve.Control2), ToPoint(curve.End), true));

        var path = new Path
        {
            Data = new PathGeometry(new[] { figure }),
            Stroke = ToBrush(colorHex),
            StrokeThickness = 2.5,
            ToolTip = string.IsNullOrWhiteSpace(note) ? null : note,
        };

        DrawingSurface.Children.Add(path);
    }

    private void DrawEdgeIndicator(ScreenRect visibleRect, ScreenRect? offScreenLastKnown, string colorHex)
    {
        if (offScreenLastKnown is not { } lastKnown) return;

        var screenBounds = new ScreenRect(0, 0, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        var referencePoint = new Point2D(visibleRect.CenterX, visibleRect.CenterY);
        var anchorPoint = new Point2D(lastKnown.CenterX, lastKnown.CenterY);

        var indicator = EdgeIndicatorCalculator.Calculate(screenBounds, anchorPoint, referencePoint);
        if (indicator == null) return;

        var marker = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = ToBrush(colorHex),
            ToolTip = "연결 대상 문자열이 화면 밖에 있다.",
        };
        Canvas.SetLeft(marker, indicator.Value.Position.X - 5);
        Canvas.SetTop(marker, indicator.Value.Position.Y - 5);
        DrawingSurface.Children.Add(marker);
    }

    private static SolidColorBrush ToBrush(string colorHex) =>
        (SolidColorBrush)new BrushConverter().ConvertFromString(colorHex)!;

    private static Point ToPoint(Point2D p) => new(p.X, p.Y);
}
