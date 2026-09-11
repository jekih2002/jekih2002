using System.Runtime.InteropServices;

namespace StringConnect.Windows.Native;

/// <summary>
/// 전역 단축키 등록과 오버레이 창의 클릭스루(click-through) 속성 제어에 필요한 최소한의
/// Win32 API 선언이다. 확장 창 스타일 비트는 64비트 환경에서도 하위 32비트 범위 내에서
/// 동작하므로 GetWindowLong/SetWindowLong(포인터 크기가 아닌 32비트 버전)로 충분하다.
/// </summary>
internal static class NativeMethods
{
    public const int WM_HOTKEY = 0x0312;

    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_LAYERED = 0x00080000;

    [Flags]
    public enum ModifierKeys : uint
    {
        None = 0x0000,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008,
    }

    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
