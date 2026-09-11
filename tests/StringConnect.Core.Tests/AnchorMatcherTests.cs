using StringConnect.Core.Matching;
using StringConnect.Core.Models;
using Xunit;

namespace StringConnect.Core.Tests;

public class AnchorMatcherTests
{
    [Fact]
    public void SingleOccurrence_ResolvesWithFullConfidence()
    {
        var text = "서론... 압력센서는 유량을 측정한다 ...결론";
        var anchor = new TextAnchor { DocumentId = "d", SelectedText = "압력센서" };

        var result = AnchorMatcher.Resolve(anchor, text);

        Assert.True(result.Success);
        Assert.False(result.RequiresUserConfirmation);
        Assert.Equal(1.0, result.Confidence);
        Assert.Equal(text.IndexOf("압력센서", StringComparison.Ordinal), result.ResolvedOffset);
    }

    [Fact]
    public void DuplicateOccurrences_DisambiguatedByContext()
    {
        var text = "머리말 압력센서 A방식 설명 본문 ... 압력센서 B방식 설명 끝맺음";
        var targetOffset = text.IndexOf("압력센서 B", StringComparison.Ordinal);
        var anchor = new TextAnchor
        {
            DocumentId = "d",
            SelectedText = "압력센서",
            ContextBefore = text[..targetOffset],
            ContextAfter = text[(targetOffset + "압력센서".Length)..],
        };

        var result = AnchorMatcher.Resolve(anchor, text);

        Assert.True(result.Success);
        Assert.False(result.RequiresUserConfirmation);
        Assert.Equal(targetOffset, result.ResolvedOffset);
    }

    [Fact]
    public void AmbiguousDuplicatesWithoutContext_RequireUserConfirmation()
    {
        var text = "압력센서 그리고 압력센서";
        var anchor = new TextAnchor { DocumentId = "d", SelectedText = "압력센서", ContextBefore = "", ContextAfter = "" };

        var result = AnchorMatcher.Resolve(anchor, text);

        Assert.True(result.RequiresUserConfirmation);
        Assert.Equal(2, result.Candidates.Count);
    }

    [Fact]
    public void MissingText_ReturnsFailureAndRequiresConfirmation()
    {
        var anchor = new TextAnchor { DocumentId = "d", SelectedText = "삭제된문자열" };

        var result = AnchorMatcher.Resolve(anchor, "이 문서에는 해당 문자열이 없다");

        Assert.False(result.Success);
        Assert.True(result.RequiresUserConfirmation);
    }
}
