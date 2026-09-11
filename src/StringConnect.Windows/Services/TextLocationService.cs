using System.Windows.Automation;
using System.Windows.Automation.Text;
using StringConnect.Core.Matching;
using StringConnect.Core.Models;

namespace StringConnect.Windows.Services;

public sealed class ResolvedSelection
{
    public required TextAnchor Anchor { get; init; }
    public required DocumentRef Document { get; init; }
    public required ScreenRect ScreenRect { get; init; }
}

public sealed class AnchorLocateResult
{
    public required MatchResult Match { get; init; }
    public ScreenRect? ScreenRect { get; init; }
    public string? DocumentTextForReview { get; init; }
}

/// <summary>
/// Windows UI Automation의 TextPattern을 이용해 (1) 현재 포커스된 컨트롤에서 선택된 문자열과
/// 그 문맥/화면 위치를 읽어오고, (2) 이미 연결에 참여 중인 문자열의 현재 화면 위치를 다시 계산한다.
/// TextPattern을 지원하지 않는 컨트롤(스캔 PDF 등 이미지 기반 콘텐츠)에서는 null을 반환하며,
/// 이 경우 상위 계층이 IOcrFallbackProvider 등 보완 수단을 사용할 수 있도록 여지를 남긴다.
/// </summary>
public static class TextLocationService
{
    private const int ContextLength = 60;

    public static ResolvedSelection? TryGetForegroundSelection()
    {
        var focused = AutomationElement.FocusedElement;
        if (focused == null) return null;

        if (!focused.TryGetCurrentPattern(TextPattern.Pattern, out var patternObj) || patternObj is not TextPattern textPattern)
            return null;

        var selection = textPattern.GetSelection();
        if (selection.Length == 0) return null;

        var range = selection[0];
        var selectedText = range.GetText(-1);
        if (string.IsNullOrWhiteSpace(selectedText)) return null;

        var rects = range.GetBoundingRectangles();
        if (rects.Length == 0) return null;
        var rect = rects[0];

        var contextBefore = ReadContextBefore(range);
        var contextAfter = ReadContextAfter(range);

        var window = GetTopLevelWindow(focused);
        var document = DocumentIdentifierService.Identify(window);

        var anchor = new TextAnchor
        {
            DocumentId = document.DocumentId,
            SelectedText = selectedText,
            ContextBefore = contextBefore,
            ContextAfter = contextAfter,
            LastKnownScreenRect = new ScreenRect(rect.X, rect.Y, rect.Width, rect.Height),
            LastResolvedAt = DateTimeOffset.UtcNow,
        };

        return new ResolvedSelection
        {
            Anchor = anchor,
            Document = document,
            ScreenRect = new ScreenRect(rect.X, rect.Y, rect.Width, rect.Height),
        };
    }

    /// <summary>
    /// 이미 연결에 포함된 앵커를 대상 창에서 다시 찾는다. 문서 전체 텍스트를 읽어 AnchorMatcher로
    /// 위치를 재확인한 뒤, 위치가 확정되면 해당 오프셋의 문자 범위를 다시 구성하여 경계 사각형을
    /// 얻는다. 위치가 모호하여 사용자 확인이 필요한 경우에는 화면 좌표 대신 문서 전체 텍스트를
    /// 함께 돌려주어, 상위 계층이 RestoreDialog 등으로 후보를 제시할 수 있게 한다.
    /// 문자 단위 이동의 정확도는 대상 애플리케이션의 UI Automation 구현에 따라 달라질 수 있어
    /// 배포 전 앱별 보정이 필요할 수 있다.
    /// </summary>
    public static AnchorLocateResult? TryLocateAnchor(TextAnchor anchor, AutomationElement window)
    {
        if (!window.TryGetCurrentPattern(TextPattern.Pattern, out var patternObj) || patternObj is not TextPattern textPattern)
            return null;

        var documentRange = textPattern.DocumentRange;
        var fullText = documentRange.GetText(-1);
        var matchResult = AnchorMatcher.Resolve(anchor, fullText);

        if (!matchResult.Success)
        {
            return new AnchorLocateResult { Match = matchResult, ScreenRect = null, DocumentTextForReview = fullText };
        }

        var found = documentRange.Clone();
        found.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextPatternRangeUnit.Character, matchResult.ResolvedOffset);
        found.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextPatternRangeUnit.Character, matchResult.ResolvedOffset);
        found.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextPatternRangeUnit.Character, anchor.SelectedText.Length);

        var rects = found.GetBoundingRectangles();
        var rect = rects.Length > 0
            ? new ScreenRect(rects[0].X, rects[0].Y, rects[0].Width, rects[0].Height)
            : (ScreenRect?)null;

        return new AnchorLocateResult { Match = matchResult, ScreenRect = rect };
    }

    /// <summary>선택 범위의 시작점에서 앞으로 ContextLength만큼만을 담은, 선택 문자열을 포함하지
    /// 않는 순수한 "앞 문맥" 범위를 만들어 텍스트를 읽는다.</summary>
    private static string ReadContextBefore(TextPatternRange range)
    {
        var probe = range.Clone();
        probe.MoveEndpointByRange(TextPatternRangeEndpoint.End, range, TextPatternRangeEndpoint.Start);
        probe.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextPatternRangeUnit.Character, -ContextLength);
        return probe.GetText(ContextLength + 10);
    }

    /// <summary>선택 범위의 끝점에서 뒤로 ContextLength만큼만을 담은, 선택 문자열을 포함하지
    /// 않는 순수한 "뒤 문맥" 범위를 만들어 텍스트를 읽는다.</summary>
    private static string ReadContextAfter(TextPatternRange range)
    {
        var probe = range.Clone();
        probe.MoveEndpointByRange(TextPatternRangeEndpoint.Start, range, TextPatternRangeEndpoint.End);
        probe.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextPatternRangeUnit.Character, ContextLength);
        return probe.GetText(ContextLength + 10);
    }

    private static AutomationElement GetTopLevelWindow(AutomationElement element)
    {
        var walker = TreeWalker.ControlViewWalker;
        var current = element;
        while (current != null && current.Current.ControlType != ControlType.Window)
        {
            var parent = walker.GetParent(current);
            if (parent == null) break;
            current = parent;
        }
        return current ?? element;
    }
}
