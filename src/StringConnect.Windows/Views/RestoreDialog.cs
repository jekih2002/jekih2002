using System.Windows;
using System.Windows.Controls;
using StringConnect.Core.Matching;

namespace StringConnect.Windows.Views;

/// <summary>
/// 문서를 다시 열었을 때 AnchorMatcher가 위치를 확정하지 못한 경우(동일 문자열 반복 등)
/// 사용자에게 후보 목록을 보여주고 직접 고르게 하는 대화상자다. XAML 대신 코드로 구성하여
/// 컴파일 도구 없이도 x:Class/InitializeComponent 불일치 위험 없이 정확한 동작을 보장한다.
/// </summary>
public sealed class RestoreDialog : Window
{
    private readonly ListBox _list;

    public int? SelectedOffset { get; private set; }

    public RestoreDialog(string selectedText, IReadOnlyList<MatchCandidate> candidates, string documentText)
    {
        Title = "연결 복원 확인";
        Width = 480;
        Height = 360;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var panel = new DockPanel { Margin = new Thickness(12) };

        var header = new TextBlock
        {
            Text = $"\"{selectedText}\" 문자열이 여러 곳에서 발견되어 연결 위치를 확정할 수 없다.\n올바른 위치를 선택한다.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8),
        };
        DockPanel.SetDock(header, Dock.Top);
        panel.Children.Add(header);

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var okButton = new Button { Content = "이 위치로 복원", Width = 110, Margin = new Thickness(4) };
        var skipButton = new Button { Content = "이 연결 건너뛰기", Width = 110, Margin = new Thickness(4) };
        okButton.Click += (_, _) =>
        {
            SelectedOffset = (_list.SelectedItem as CandidateItem)?.Offset;
            DialogResult = true;
        };
        skipButton.Click += (_, _) =>
        {
            SelectedOffset = null;
            DialogResult = false;
        };
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(skipButton);
        DockPanel.SetDock(buttonPanel, Dock.Bottom);
        panel.Children.Add(buttonPanel);

        _list = new ListBox { DisplayMemberPath = nameof(CandidateItem.Preview) };
        foreach (var candidate in candidates)
        {
            var start = Math.Max(0, candidate.Offset - 30);
            var end = Math.Min(documentText.Length, candidate.Offset + selectedText.Length + 30);
            var preview = documentText[start..end].Replace('\n', ' ');
            _list.Items.Add(new CandidateItem(candidate.Offset, $"…{preview}…  (일치도 {candidate.Score:F1})"));
        }
        if (_list.Items.Count > 0) _list.SelectedIndex = 0;
        panel.Children.Add(_list);

        Content = panel;
    }

    private sealed record CandidateItem(int Offset, string Preview);
}
