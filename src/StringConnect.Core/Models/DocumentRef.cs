namespace StringConnect.Core.Models;

public enum AppKind
{
    Generic,
    Word,
    Pdf,
    Browser,
}

/// <summary>
/// 연결의 대상이 되는 문서 하나를 식별하기 위한 정보다. DocumentId는 같은 문서를 다시 열었을 때도
/// 동일한 값으로 재생성될 수 있어야 하며, 이를 위해 가능한 경우 파일 경로를, 그렇지 않은 경우
/// 프로세스명과 창 제목의 조합을 사용한다.
/// </summary>
public sealed class DocumentRef
{
    public required string DocumentId { get; init; }
    public AppKind AppKind { get; init; } = AppKind.Generic;
    public string? Path { get; init; }
    public string? WindowTitleHint { get; init; }
}
