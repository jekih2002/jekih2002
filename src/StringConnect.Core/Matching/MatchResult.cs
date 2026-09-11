namespace StringConnect.Core.Matching;

public sealed class MatchCandidate
{
    public required int Offset { get; init; }
    public required double Score { get; init; }
}

/// <summary>
/// 문서를 다시 열었을 때 저장된 TextAnchor를 현재 문서 텍스트에서 다시 찾은 결과다.
/// 동일 문자열이 반복되는 문서에서는 RequiresUserConfirmation이 true가 되며, 이 경우
/// 상위 계층(예: RestoreDialog)이 Candidates를 사용자에게 제시하여 직접 바로잡을 수 있게 한다.
/// </summary>
public sealed class MatchResult
{
    public bool Success { get; init; }
    public int ResolvedOffset { get; init; } = -1;
    public double Confidence { get; init; }
    public bool RequiresUserConfirmation { get; init; }
    public IReadOnlyList<MatchCandidate> Candidates { get; init; } = Array.Empty<MatchCandidate>();
}
