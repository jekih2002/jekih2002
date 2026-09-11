using System.Windows;
using System.Windows.Interop;
using StringConnect.Windows.Native;

namespace StringConnect.Windows.Services;

public enum VirtualKey : uint
{
    D1 = 0x31,
    D2 = 0x32,
}

/// <summary>
/// "연결 시작"과 "연결 완료" 전역 단축키를 등록하고, 다른 애플리케이션이 포커스를 가진 상태에서도
/// 눌림을 감지하여 이벤트로 알린다. 메시지 수신을 위해 오버레이 창의 HWND에 훅을 건다.
/// 기본값은 Ctrl+Alt+1(시작), Ctrl+Alt+2(완료)이며, 다른 프로그램과 충돌하면 등록이 실패한다.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private const int StartConnectionId = 0xB001;
    private const int CompleteConnectionId = 0xB002;

    private readonly HwndSource _source;

    public event Action? ConnectionStartRequested;
    public event Action? ConnectionCompleteRequested;

    public GlobalHotkeyService(Window messageWindow)
    {
        var handle = new WindowInteropHelper(messageWindow).EnsureHandle();
        _source = HwndSource.FromHwnd(handle) ?? throw new InvalidOperationException("윈도우 핸들을 확보할 수 없다.");
        _source.AddHook(WndProc);

        RegisterHotkey(StartConnectionId, NativeMethods.ModifierKeys.Control | NativeMethods.ModifierKeys.Alt, VirtualKey.D1);
        RegisterHotkey(CompleteConnectionId, NativeMethods.ModifierKeys.Control | NativeMethods.ModifierKeys.Alt, VirtualKey.D2);
    }

    private void RegisterHotkey(int id, NativeMethods.ModifierKeys modifiers, VirtualKey key)
    {
        if (!NativeMethods.RegisterHotKey(_source.Handle, id, (uint)modifiers, (uint)key))
        {
            throw new InvalidOperationException($"단축키 등록에 실패했다 (id={id}). 다른 프로그램이 동일한 단축키를 선점했을 수 있다.");
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (id == StartConnectionId) ConnectionStartRequested?.Invoke();
            else if (id == CompleteConnectionId) ConnectionCompleteRequested?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        NativeMethods.UnregisterHotKey(_source.Handle, StartConnectionId);
        NativeMethods.UnregisterHotKey(_source.Handle, CompleteConnectionId);
        _source.RemoveHook(WndProc);
    }
}
