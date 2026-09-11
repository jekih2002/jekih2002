using System.Text.Json;
using StringConnect.Core.Models;

namespace StringConnect.Core.Persistence;

/// <summary>
/// 등록된 문서 목록과 문자열 간 연결 전체를 보관하고 JSON으로 영속화하는 저장소다.
/// 애플리케이션을 재실행해도 동일한 연결관계를 복원할 수 있도록 하는 단일 진실 공급원(source of truth)이다.
/// </summary>
public sealed class ConnectionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public Dictionary<string, DocumentRef> Documents { get; init; } = new();
    public List<Connection> Connections { get; init; } = new();

    public DocumentRef RegisterDocument(DocumentRef document)
    {
        Documents[document.DocumentId] = document;
        return document;
    }

    public Connection AddConnection(TextAnchor from, TextAnchor to, string color = "#3A86FF", string note = "")
    {
        var connection = new Connection { From = from, To = to, Color = color, Note = note };
        Connections.Add(connection);
        return connection;
    }

    public bool RemoveConnection(Guid id) => Connections.RemoveAll(c => c.Id == id) > 0;

    public IReadOnlyList<Connection> GetConnectionsForDocument(string documentId) =>
        Connections.Where(c => c.From.DocumentId == documentId || c.To.DocumentId == documentId).ToList();

    public IReadOnlyList<Connection> GetConnectionsForSelection(string documentId, string selectedText) =>
        Connections.Where(c =>
            (c.From.DocumentId == documentId && c.From.SelectedText == selectedText) ||
            (c.To.DocumentId == documentId && c.To.SelectedText == selectedText)).ToList();

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static ConnectionStore FromJson(string json) =>
        JsonSerializer.Deserialize<ConnectionStore>(json, JsonOptions) ?? new ConnectionStore();

    public void Save(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(filePath, ToJson());
    }

    public static ConnectionStore Load(string filePath) =>
        File.Exists(filePath) ? FromJson(File.ReadAllText(filePath)) : new ConnectionStore();
}
