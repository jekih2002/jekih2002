using System.Windows.Forms;

namespace StringConnect.Windows.Services;

/// <summary>
/// 작업 표시줄 트레이 아이콘을 통해 편집 모드 전환(오버레이 클릭스루 해제), 연결 목록 창 열기,
/// 종료 명령을 제공한다. 오버레이 자체는 평상시 클릭과 키보드 입력을 아래 애플리케이션으로
/// 그대로 전달해야 하므로, 끈의 색상/메모 편집이나 삭제 같은 조작은 이 트레이 메뉴를 통해
/// 별도의 일반 창(ConnectionListWindow)에서 이루어진다.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _icon;

    public event Action? ToggleEditModeRequested;
    public event Action? OpenConnectionListRequested;
    public event Action? ExitRequested;

    public TrayIconService()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("편집 모드 전환", null, (_, _) => ToggleEditModeRequested?.Invoke());
        menu.Items.Add("연결 목록 보기", null, (_, _) => OpenConnectionListRequested?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("종료", null, (_, _) => ExitRequested?.Invoke());

        _icon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "StringConnect",
            ContextMenuStrip = menu,
        };
    }

    public void ShowBalloon(string title, string message) =>
        _icon.ShowBalloonTip(2000, title, message, ToolTipIcon.Info);

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
