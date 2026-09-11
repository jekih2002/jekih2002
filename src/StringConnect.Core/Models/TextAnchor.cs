namespace StringConnect.Core.Models;

/// <summary>
/// 문서 내 특정 문자열에 고정되는 연결점이다. 문자열 자체와 그 앞뒤 문맥, 문서 내 오프셋을
/// 함께 저장하여, 문서를 다시 열었을 때 동일한 위치를 다시 찾아낼 수 있도록 한다.
/// 화면상 위치는 창 이동이나 스크롤에 따라 계속 바뀌므로 영속 식별자로 사용하지 않는다.
/// </summary>
public sealed class TextAnchor
{
    public required string DocumentId { get; init; }
    public required string SelectedText { get; init; }
    /// <summary>연결 위치가 모호하여 사용자가 직접 확정한 경우, 확정된 위치를 기준으로
    /// 다시 채워지므로 init이 아닌 set으로 둔다(RestoreDialog를 통한 재학습).</summary>
    public string ContextBefore { get; set; } = string.Empty;
    public string ContextAfter { get; set; } = string.Empty;

    /// <summary>문자열이 처음 연결될 당시 문서 전체 텍스트 기준 문자 오프셋이다. 참고용이며,
    /// 문서가 수정된 이후에는 AnchorMatcher가 문맥 기반으로 다시 계산한 오프셋을 우선한다.</summary>
    public int DocumentOffset { get; init; } = -1;

    /// <summary>화면 밖으로 사라졌을 때 가장자리 표시를 그리기 위해 마지막으로 확인된 화면 위치를 보관한다.
    /// 연결관계 자체와는 무관한 캐시 값이므로 저장/복원 시 유실되어도 무방하다.</summary>
    public ScreenRect? LastKnownScreenRect { get; set; }

    public DateTimeOffset LastResolvedAt { get; set; } = DateTimeOffset.UtcNow;
}
