using System.Windows.Threading;
using StringConnect.Core.Matching;
using StringConnect.Core.Models;
using StringConnect.Core.Persistence;

namespace StringConnect.Windows.Services;

public sealed class AnchorResolution
{
    public required TextAnchor Anchor { get; init; }
    public ScreenRect? CurrentRect { get; init; }
    public bool WindowIsOpen { get; init; }
}

/// <summary>
/// 일정 주기마다 모든 연결의 양쪽 앵커에 대해 현재 화면 위치를 다시 계산한다. 창 이동이나
/// 스크롤에 따라 끈의 끝점을 갱신하기 위한 핵심 구동부다. 실제로 UI Automation을 통해
/// 텍스트 범위를 재조회하는 비용이 큰 작업은 OpenWindowIndex에서 창을 찾은 앵커에 대해서만
/// 수행되므로, 연결 개수 자체를 인위적으로 제한하지 않고도 매 주기 비용을 열려 있는 창
/// 수준으로 억제한다. 위치가 모호하여 자동 복원이 불가능한 앵커는 세션당 한 번만
/// AmbiguousMatchDetected 이벤트로 알려 상위 계층이 사용자 확인을 받도록 한다.
/// </summary>
public sealed class ConnectionPollingService : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly ConnectionStore _store;
    private readonly HashSet<TextAnchor> _flaggedAnchors = new();

    public event Action<IReadOnlyList<AnchorResolution>>? Resolved;
    public event Action<TextAnchor, string, IReadOnlyList<MatchCandidate>>? AmbiguousMatchDetected;

    public ConnectionPollingService(ConnectionStore store, TimeSpan interval)
    {
        _store = store;
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = interval };
        _timer.Tick += (_, _) => Tick();
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();
    public void RequestImmediateRefresh() => Tick();

    private void Tick()
    {
        var openWindows = OpenWindowIndex.Build();
        var anchors = CollectAnchors();
        var results = new List<AnchorResolution>(anchors.Count);

        foreach (var anchor in anchors)
        {
            if (!openWindows.TryGetWindow(anchor.DocumentId, out var window))
            {
                results.Add(new AnchorResolution { Anchor = anchor, CurrentRect = null, WindowIsOpen = false });
                continue;
            }

            var located = TextLocationService.TryLocateAnchor(anchor, window);
            if (located?.ScreenRect is { } rect) anchor.LastKnownScreenRect = rect;

            if (located != null && located.Match.RequiresUserConfirmation && _flaggedAnchors.Add(anchor))
            {
                AmbiguousMatchDetected?.Invoke(anchor, located.DocumentTextForReview ?? string.Empty, located.Match.Candidates);
            }

            results.Add(new AnchorResolution { Anchor = anchor, CurrentRect = located?.ScreenRect, WindowIsOpen = true });
        }

        Resolved?.Invoke(results);
    }

    private List<TextAnchor> CollectAnchors()
    {
        var anchors = new List<TextAnchor>(_store.Connections.Count * 2);
        foreach (var connection in _store.Connections)
        {
            anchors.Add(connection.From);
            anchors.Add(connection.To);
        }
        return anchors;
    }

    public void Dispose() => _timer.Stop();
}
