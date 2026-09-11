using System.ComponentModel;
using StringConnect.Core.Models;
using StringConnect.Core.Persistence;
using StringConnect.Windows.Services;

namespace StringConnect.Windows.ViewModels;

public enum ConnectionCaptureState
{
    Idle,
    WaitingForSecondSelection,
}

/// <summary>
/// "연결 시작" / "연결 완료" 단축키 두 번으로 하나의 Connection을 만들어내는 상태 기계다.
/// 첫 번째 단축키에서 선택된 문자열을 임시로 보관하고, 두 번째 단축키가 눌리면 두 앵커를 묶어
/// ConnectionStore에 추가한 뒤 즉시 디스크에 저장한다.
/// </summary>
public sealed class ConnectionManagerViewModel : INotifyPropertyChanged
{
    private readonly ConnectionStore _store;
    private readonly string _storeFilePath;
    private PendingSelection? _pending;

    public ConnectionCaptureState State { get; private set; } = ConnectionCaptureState.Idle;
    public string NextConnectionColor { get; set; } = "#3A86FF";

    public event Action<Connection>? ConnectionCreated;
    public event Action<string>? StatusMessage;
    public event PropertyChangedEventHandler? PropertyChanged;

    public ConnectionManagerViewModel(ConnectionStore store, string storeFilePath)
    {
        _store = store;
        _storeFilePath = storeFilePath;
    }

    public void OnConnectionStartRequested(ResolvedSelection? selection)
    {
        if (selection == null)
        {
            StatusMessage?.Invoke("연결을 시작할 문자열을 먼저 선택해야 한다.");
            return;
        }

        _store.RegisterDocument(selection.Document);
        _pending = new PendingSelection(selection.Anchor);
        State = ConnectionCaptureState.WaitingForSecondSelection;
        RaiseStateChanged();
        StatusMessage?.Invoke($"시작점 지정: \"{Truncate(selection.Anchor.SelectedText)}\". 연결할 문자열을 선택한 뒤 완료 단축키를 누른다.");
    }

    public void OnConnectionCompleteRequested(ResolvedSelection? selection)
    {
        if (_pending == null)
        {
            StatusMessage?.Invoke("먼저 시작 단축키로 첫 번째 문자열을 지정해야 한다.");
            return;
        }

        if (selection == null)
        {
            StatusMessage?.Invoke("연결을 완료할 문자열을 먼저 선택해야 한다.");
            return;
        }

        _store.RegisterDocument(selection.Document);
        var connection = _store.AddConnection(_pending.Anchor, selection.Anchor, NextConnectionColor);
        _store.Save(_storeFilePath);

        _pending = null;
        State = ConnectionCaptureState.Idle;
        RaiseStateChanged();

        ConnectionCreated?.Invoke(connection);
        StatusMessage?.Invoke("연결이 생성되었다.");
    }

    public void CancelPending()
    {
        if (_pending == null) return;
        _pending = null;
        State = ConnectionCaptureState.Idle;
        RaiseStateChanged();
        StatusMessage?.Invoke("연결 지정을 취소했다.");
    }

    private static string Truncate(string text) => text.Length <= 20 ? text : text[..20] + "…";

    private void RaiseStateChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));

    private sealed record PendingSelection(TextAnchor Anchor);
}
