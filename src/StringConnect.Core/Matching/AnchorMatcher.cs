using StringConnect.Core.Models;

namespace StringConnect.Core.Matching;

/// <summary>
/// 저장된 TextAnchor(선택 문자열 + 앞뒤 문맥)를 현재 문서 전체 텍스트와 대조하여 위치를 재확인한다.
/// 동일 문자열이 여러 번 등장하는 문서에서는 앞뒤 문맥과의 일치 정도로 후보를 채점하며,
/// 1순위와 2순위 후보의 점수 차이가 충분히 크지 않으면 자동 복원 대신 사용자 확인을 요구한다.
/// </summary>
public static class AnchorMatcher
{
    private const double ConfidentMarginRatio = 1.3;

    public static MatchResult Resolve(TextAnchor anchor, string currentDocumentText)
    {
        if (string.IsNullOrEmpty(anchor.SelectedText) || currentDocumentText is null)
        {
            return new MatchResult { Success = false, RequiresUserConfirmation = true };
        }

        var occurrences = FindAllOccurrences(currentDocumentText, anchor.SelectedText);

        if (occurrences.Count == 0)
        {
            return new MatchResult { Success = false, RequiresUserConfirmation = true };
        }

        if (occurrences.Count == 1)
        {
            var offset = occurrences[0];
            return new MatchResult
            {
                Success = true,
                ResolvedOffset = offset,
                Confidence = 1.0,
                RequiresUserConfirmation = false,
                Candidates = new[] { new MatchCandidate { Offset = offset, Score = 1.0 } },
            };
        }

        var candidates = occurrences
            .Select(offset => new MatchCandidate { Offset = offset, Score = ScoreContext(anchor, currentDocumentText, offset) })
            .OrderByDescending(c => c.Score)
            .ToList();

        var best = candidates[0];
        var runnerUp = candidates.Count > 1 ? candidates[1] : new MatchCandidate { Offset = -1, Score = 0 };
        var confident = best.Score > 0 && (runnerUp.Score == 0 || best.Score >= runnerUp.Score * ConfidentMarginRatio);

        return new MatchResult
        {
            Success = confident,
            ResolvedOffset = best.Offset,
            Confidence = NormalizeScore(best.Score, anchor),
            RequiresUserConfirmation = !confident,
            Candidates = candidates,
        };
    }

    private static List<int> FindAllOccurrences(string text, string needle)
    {
        var result = new List<int>();
        var searchFrom = 0;
        while (true)
        {
            var found = text.IndexOf(needle, searchFrom, StringComparison.Ordinal);
            if (found < 0) break;
            result.Add(found);
            searchFrom = found + 1;
        }
        return result;
    }

    private static double ScoreContext(TextAnchor anchor, string text, int offset)
    {
        var beforeActual = text[..offset];
        var afterActual = text[(offset + anchor.SelectedText.Length)..];

        var beforeScore = CommonSuffixLength(beforeActual, anchor.ContextBefore);
        var afterScore = CommonPrefixLength(afterActual, anchor.ContextAfter);

        return beforeScore + afterScore;
    }

    private static double NormalizeScore(double score, TextAnchor anchor)
    {
        var max = anchor.ContextBefore.Length + anchor.ContextAfter.Length;
        return max == 0 ? 0 : Math.Clamp(score / max, 0, 1);
    }

    private static int CommonSuffixLength(string a, string b)
    {
        var i = a.Length - 1;
        var j = b.Length - 1;
        var count = 0;
        while (i >= 0 && j >= 0 && a[i] == b[j])
        {
            count++;
            i--;
            j--;
        }
        return count;
    }

    private static int CommonPrefixLength(string a, string b)
    {
        var i = 0;
        var count = 0;
        while (i < a.Length && i < b.Length && a[i] == b[i])
        {
            count++;
            i++;
        }
        return count;
    }
}
