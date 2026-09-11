using StringConnect.Core.Models;

namespace StringConnect.Windows.Services;

/// <summary>
/// TextPattern을 지원하지 않는 콘텐츠(예: 스캔 PDF의 이미지 페이지)에서 문자열 위치를 보완하기
/// 위한 확장 지점이다. MVP에서는 실제 OCR 엔진을 연동하지 않고 항상 실패를 반환하는 구현만
/// 제공하며, 필요 시 Windows.Media.Ocr(OS 내장) 또는 Tesseract 등을 붙여 넣을 수 있도록
/// 인터페이스만 정의해 둔다.
/// </summary>
public interface IOcrFallbackProvider
{
    Task<ScreenRect?> TryLocateAsync(TextAnchor anchor, ScreenRect searchRegion);
}

public sealed class NullOcrFallbackProvider : IOcrFallbackProvider
{
    public Task<ScreenRect?> TryLocateAsync(TextAnchor anchor, ScreenRect searchRegion) =>
        Task.FromResult<ScreenRect?>(null);
}
