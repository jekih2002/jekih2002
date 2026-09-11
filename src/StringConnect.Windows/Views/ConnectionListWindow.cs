using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StringConnect.Core.Models;
using StringConnect.Core.Persistence;

namespace StringConnect.Windows.Views;

/// <summary>
/// 현재 저장된 모든 연결을 목록으로 보여주고, 각 연결의 색상/메모 확인과 삭제를 지원하는 창이다.
/// 오버레이 자체는 클릭스루 상태가 기본이므로, 끈에 대한 편집 조작은 이 별도 창을 통해 이루어진다.
/// </summary>
public sealed class ConnectionListWindow : Window
{
    private readonly ConnectionStore _store;
    private readonly string _storeFilePath;
    private readonly ItemsControl _list;

    public event Action? ConnectionsChanged;

    public ConnectionListWindow(ConnectionStore store, string storeFilePath)
    {
        _store = store;
        _storeFilePath = storeFilePath;

        Title = "연결 목록";
        Width = 560;
        Height = 480;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        _list = new ItemsControl();
        var scroll = new ScrollViewer { Content = _list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Content = scroll;

        Refresh();
    }

    public void Refresh()
    {
        _list.Items.Clear();
        foreach (var connection in _store.Connections)
        {
            _list.Items.Add(BuildRow(connection));
        }
    }

    private UIElement BuildRow(Connection connection)
    {
        var row = new Grid { Margin = new Thickness(8) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

        var swatch = new System.Windows.Shapes.Rectangle
        {
            Width = 12,
            Height = 12,
            Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(connection.Color)!,
        };
        Grid.SetColumn(swatch, 0);

        var text = new TextBlock
        {
            Text = $"{connection.From.SelectedText}  ↔  {connection.To.SelectedText}" +
                   (string.IsNullOrWhiteSpace(connection.Note) ? "" : $"\n메모: {connection.Note}"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(6, 0, 6, 0),
        };
        Grid.SetColumn(text, 1);

        var deleteButton = new Button { Content = "삭제", Width = 50 };
        deleteButton.Click += (_, _) =>
        {
            _store.RemoveConnection(connection.Id);
            _store.Save(_storeFilePath);
            Refresh();
            ConnectionsChanged?.Invoke();
        };
        Grid.SetColumn(deleteButton, 2);

        row.Children.Add(swatch);
        row.Children.Add(text);
        row.Children.Add(deleteButton);
        return row;
    }
}
