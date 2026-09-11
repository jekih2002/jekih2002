namespace StringConnect.Core.Models;

/// <summary>
/// 서로 다른 두 문서(또는 같은 문서 내 두 위치)에 고정된 문자열 사이의 연결 하나를 나타낸다.
/// 하나의 문자열은 여러 개의 Connection의 From 또는 To로 동시에 참여할 수 있으며,
/// 연결 개수 자체에는 인위적인 상한을 두지 않는다.
/// </summary>
public sealed class Connection
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required TextAnchor From { get; init; }
    public required TextAnchor To { get; init; }
    public string Color { get; set; } = "#3A86FF";
    public string Note { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
