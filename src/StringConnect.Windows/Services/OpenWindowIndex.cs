using System.Windows.Automation;

namespace StringConnect.Windows.Services;

/// <summary>
/// 현재 열려 있는 최상위 창들을 한 번 훑어 DocumentId → AutomationElement 매핑을 만든다.
/// 폴링 주기마다 다시 만들어지며, 연결에 필요한 값비싼 재조회(TryResolveCurrentRect)는
/// 이 인덱스에서 창을 찾은 앵커에 대해서만 수행되므로, 전체 연결 개수가 아니라
/// "현재 열려 있고 실제로 연결에 관련된 창의 수"에 의해 매 주기 비용이 제한된다.
/// </summary>
public sealed class OpenWindowIndex
{
    private readonly Dictionary<string, AutomationElement> _byDocumentId;

    private OpenWindowIndex(Dictionary<string, AutomationElement> byDocumentId)
    {
        _byDocumentId = byDocumentId;
    }

    public bool TryGetWindow(string documentId, out AutomationElement window) =>
        _byDocumentId.TryGetValue(documentId, out window!);

    public static OpenWindowIndex Build()
    {
        var map = new Dictionary<string, AutomationElement>();
        var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
        var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, condition);

        foreach (AutomationElement window in windows)
        {
            try
            {
                var doc = DocumentIdentifierService.Identify(window);
                map[doc.DocumentId] = window;
            }
            catch
            {
                // 접근 권한이 없거나 조회 도중 닫힌 창은 이번 주기에서는 건너뛴다.
            }
        }

        return new OpenWindowIndex(map);
    }
}
