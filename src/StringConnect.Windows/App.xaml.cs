using System.IO;
using System.Windows;
using StringConnect.Core.Matching;
using StringConnect.Core.Models;
using StringConnect.Core.Persistence;
using StringConnect.Windows.Services;
using StringConnect.Windows.ViewModels;
using StringConnect.Windows.Views;

namespace StringConnect.Windows;

/// <summary>
/// 애플리케이션 진입점이다. 오버레이 창, 전역 단축키, 주기적 위치 갱신, 트레이 아이콘을
/// 시작 시 모두 구성하고, 이벤트를 서로 연결한다. 메인 창을 두지 않고 트레이 상주 형태로
/// 동작하며, 창을 닫아도 종료되지 않도록 ShutdownMode=OnExplicitShutdown으로 설정했다.
/// </summary>
public partial class App : Application
{
    private ConnectionStore _store = new();
    private string _storeFilePath = string.Empty;
    private OverlayWindow _overlay = null!;
    private GlobalHotkeyService _hotkeys = null!;
    private ConnectionPollingService _polling = null!;
    private TrayIconService _tray = null!;
    private ConnectionManagerViewModel _manager = null!;
    private ConnectionListWindow? _listWindow;
    private bool _editMode;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _storeFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StringConnect", "connections.json");
        _store = ConnectionStore.Load(_storeFilePath);

        _manager = new ConnectionManagerViewModel(_store, _storeFilePath);
        _manager.ConnectionCreated += _ => _polling.RequestImmediateRefresh();

        _overlay = new OverlayWindow();
        _overlay.Show();

        _hotkeys = new GlobalHotkeyService(_overlay);
        _hotkeys.ConnectionStartRequested += () => _manager.OnConnectionStartRequested(TextLocationService.TryGetForegroundSelection());
        _hotkeys.ConnectionCompleteRequested += () => _manager.OnConnectionCompleteRequested(TextLocationService.TryGetForegroundSelection());

        _tray = new TrayIconService();
        _manager.StatusMessage += message => _tray.ShowBalloon("StringConnect", message);
        _tray.ToggleEditModeRequested += ToggleEditMode;
        _tray.OpenConnectionListRequested += ShowConnectionList;
        _tray.ExitRequested += () => Shutdown();

        _polling = new ConnectionPollingService(_store, TimeSpan.FromMilliseconds(150));
        _polling.Resolved += OnAnchorsResolved;
        _polling.AmbiguousMatchDetected += OnAmbiguousMatchDetected;
        _polling.Start();
    }

    private void OnAmbiguousMatchDetected(TextAnchor anchor, string documentText, IReadOnlyList<MatchCandidate> candidates)
    {
        Dispatcher.Invoke(() =>
        {
            var dialog = new RestoreDialog(anchor.SelectedText, candidates, documentText) { Owner = _overlay };
            if (dialog.ShowDialog() == true && dialog.SelectedOffset is { } offset)
            {
                RebindAnchorContext(anchor, documentText, offset);
                _store.Save(_storeFilePath);
                _polling.RequestImmediateRefresh();
            }
        });
    }

    private static void RebindAnchorContext(TextAnchor anchor, string documentText, int offset)
    {
        // 사용자가 후보 중 하나를 직접 골라 확정했으므로, 현재 문맥으로 앵커를 다시 학습시켜
        // 이후 폴링에서는 같은 위치가 모호함 없이 곧바로 확정되도록 한다.
        const int contextLength = 60;
        var beforeStart = Math.Max(0, offset - contextLength);
        var afterEnd = Math.Min(documentText.Length, offset + anchor.SelectedText.Length + contextLength);

        anchor.ContextBefore = documentText[beforeStart..offset];
        anchor.ContextAfter = documentText[(offset + anchor.SelectedText.Length)..afterEnd];
    }

    private void ToggleEditMode()
    {
        _editMode = !_editMode;
        _overlay.SetClickThrough(!_editMode);
        _tray.ShowBalloon("StringConnect", _editMode ? "편집 모드로 전환했다." : "편집 모드를 해제했다.");
    }

    private void OnAnchorsResolved(IReadOnlyList<AnchorResolution> resolutions)
    {
        var rectByAnchor = resolutions.ToDictionary(r => r.Anchor, r => r.CurrentRect);

        var visuals = _store.Connections.Select(connection => new VisualConnection
        {
            Connection = connection,
            Color = connection.Color,
            FromRect = rectByAnchor.GetValueOrDefault(connection.From),
            ToRect = rectByAnchor.GetValueOrDefault(connection.To),
            FromLastKnown = connection.From.LastKnownScreenRect,
            ToLastKnown = connection.To.LastKnownScreenRect,
        }).ToList();

        Dispatcher.Invoke(() => _overlay.Render(visuals));
    }

    private void ShowConnectionList()
    {
        if (_listWindow == null)
        {
            _listWindow = new ConnectionListWindow(_store, _storeFilePath);
            _listWindow.ConnectionsChanged += () => _polling.RequestImmediateRefresh();
        }
        else
        {
            _listWindow.Refresh();
        }

        _listWindow.Show();
        _listWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _polling.Dispose();
        _hotkeys.Dispose();
        _tray.Dispose();
        base.OnExit(e);
    }
}
