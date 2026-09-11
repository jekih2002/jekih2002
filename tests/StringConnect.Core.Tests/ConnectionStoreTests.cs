using StringConnect.Core.Models;
using StringConnect.Core.Persistence;
using Xunit;

namespace StringConnect.Core.Tests;

public class ConnectionStoreTests
{
    [Fact]
    public void RoundTrip_PreservesConnectionsAndDocuments()
    {
        var store = new ConnectionStore();
        store.RegisterDocument(new DocumentRef { DocumentId = "doc-a", AppKind = AppKind.Word, Path = "C:/a.docx" });
        store.RegisterDocument(new DocumentRef { DocumentId = "doc-b", AppKind = AppKind.Pdf, Path = "C:/b.pdf" });

        var from = new TextAnchor { DocumentId = "doc-a", SelectedText = "압력센서", ContextBefore = "...", ContextAfter = "..." };
        var to = new TextAnchor { DocumentId = "doc-b", SelectedText = "수압에 따른 수위 산출", ContextBefore = "...", ContextAfter = "..." };
        store.AddConnection(from, to, "#FF0000", "관련 근거");

        var json = store.ToJson();
        var restored = ConnectionStore.FromJson(json);

        Assert.Single(restored.Connections);
        Assert.Equal(2, restored.Documents.Count);
        Assert.Equal("압력센서", restored.Connections[0].From.SelectedText);
        Assert.Equal("#FF0000", restored.Connections[0].Color);
    }

    [Fact]
    public void GetConnectionsForDocument_ReturnsOnlyMatching()
    {
        var store = new ConnectionStore();
        var a1 = new TextAnchor { DocumentId = "doc-a", SelectedText = "x" };
        var b1 = new TextAnchor { DocumentId = "doc-b", SelectedText = "y" };
        var c1 = new TextAnchor { DocumentId = "doc-c", SelectedText = "z" };
        store.AddConnection(a1, b1);
        store.AddConnection(a1, c1);

        var resultA = store.GetConnectionsForDocument("doc-a");
        Assert.Equal(2, resultA.Count);

        var resultB = store.GetConnectionsForDocument("doc-b");
        Assert.Single(resultB);
    }

    [Fact]
    public void RemoveConnection_RemovesById()
    {
        var store = new ConnectionStore();
        var connection = store.AddConnection(
            new TextAnchor { DocumentId = "doc-a", SelectedText = "x" },
            new TextAnchor { DocumentId = "doc-b", SelectedText = "y" });

        Assert.True(store.RemoveConnection(connection.Id));
        Assert.Empty(store.Connections);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsThroughFile()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"stringconnect-test-{Guid.NewGuid():N}.json");
        try
        {
            var store = new ConnectionStore();
            store.AddConnection(
                new TextAnchor { DocumentId = "doc-a", SelectedText = "압력센서" },
                new TextAnchor { DocumentId = "doc-b", SelectedText = "수위 산출" });

            store.Save(tempFile);
            var loaded = ConnectionStore.Load(tempFile);

            Assert.Single(loaded.Connections);
            Assert.Equal("압력센서", loaded.Connections[0].From.SelectedText);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
