using System.Diagnostics;
using System.Windows.Automation;
using StringConnect.Core.Models;

namespace StringConnect.Windows.Services;

/// <summary>
/// 최상위 창 하나로부터 안정적인 DocumentId를 만들어낸다. 파일 경로를 확보할 수 있는 경우
/// (창 제목에서 유추) 경로를 키로 사용하여 문서를 다시 열었을 때도 동일한 식별자가 나오도록 하고,
/// 그렇지 못한 경우 프로세스명과 창 제목의 조합으로 대체한다. 후자는 제목이 바뀌면 식별자도
/// 바뀌는 한계가 있으며, 이는 향후 앱별(Word COM, 브라우저 확장 등) 연동으로 보완할 부분이다.
/// </summary>
public static class DocumentIdentifierService
{
    public static DocumentRef Identify(AutomationElement window)
    {
        var processId = window.Current.ProcessId;
        string processName;
        try
        {
            processName = Process.GetProcessById(processId).ProcessName;
        }
        catch
        {
            processName = "unknown";
        }

        var title = window.Current.Name ?? string.Empty;
        var appKind = ClassifyApp(processName);
        var path = TryExtractPathHint(title);

        var stableKey = path ?? $"{processName}::{title}";
        var documentId = $"{appKind}:{stableKey}";

        return new DocumentRef
        {
            DocumentId = documentId,
            AppKind = appKind,
            Path = path,
            WindowTitleHint = title,
        };
    }

    private static AppKind ClassifyApp(string processName)
    {
        return processName.ToLowerInvariant() switch
        {
            "winword" => AppKind.Word,
            "acrord32" or "acrord64" or "sumatrapdf" or "foxitpdfreader" => AppKind.Pdf,
            "chrome" or "msedge" or "firefox" => AppKind.Browser,
            _ => AppKind.Generic,
        };
    }

    private static string? TryExtractPathHint(string title)
    {
        // Word/Adobe Reader 등의 창 제목은 대개 "파일명 - 프로그램명" 형태이므로 파일명 부분만
        // 분리해 문서 식별 키의 후보로 쓴다. 실제 절대 경로 확보는 앱별 COM/접근성 연동이
        // 필요하며 이는 MVP 범위를 벗어나 향후 과제로 남긴다.
        var separatorIndex = title.LastIndexOf(" - ", StringComparison.Ordinal);
        return separatorIndex > 0 ? title[..separatorIndex].Trim() : null;
    }
}
