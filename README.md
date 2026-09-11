# StringConnect

## 1. 개요

본 프로그램은 서로 다른 애플리케이션(워드 프로세서, PDF 뷰어, 웹 브라우저 등)에서 열람 중인
문서 내의 특정 문자열과 문자열 사이를 화면 위에 끈 형태의 곡선으로 연결하여 표시하고, 창을
이동하거나 문서를 스크롤하더라도 그 연결관계를 계속 유지하는 것을 목적으로 한다. 이는 화면
위에 필기를 얹는 기존 오버레이 프로그램(Epic Pen Pro, Presentify 등)이나 문서 간 양방향 링크를
목록 형태로 제공하는 프로그램(Hookmark)만으로는 충족되지 않는 요구, 즉 "문자열과 문자열의
연결관계를 시각적으로 지속시키는 기능"을 겨냥한 것이다.

## 2. 아키텍처

솔루션은 다음 세 개의 프로젝트로 구성된다.

```
StringConnect.sln
├─ src/StringConnect.Core       순수 도메인 로직 (플랫폼 독립, net8.0)
├─ src/StringConnect.Windows    Windows 전용 UI/오버레이/UI Automation 연동 (net8.0-windows, WPF)
└─ tests/StringConnect.Core.Tests   Core 계층 단위 테스트 (xunit)
```

Core 계층과 Windows 계층을 분리한 이유는 다음과 같다. 문자열 위치 재탐색(AnchorMatcher),
연결 저장/복원(ConnectionStore), 끈의 곡선 기하(StringGeometry), 화면 밖 표시 위치 계산
(EdgeIndicatorCalculator)은 운영체제의 접근성 API와 무관하게 그 정확성을 독립적으로 검증할 수
있는 로직이다. 이를 Windows 전용 UI 코드와 분리함으로써, Windows 환경이 없는 곳에서도
`dotnet test`만으로 핵심 로직의 정확성을 확인할 수 있도록 하였다. 반면 실제 문서 위에서
문자열의 화면 위치를 읽어오는 부분은 Windows UI Automation의 TextPattern에 의존하므로,
이는 Windows 전용 프로젝트에만 위치한다.

### 2.1 Core 계층의 구성 요소

| 파일 | 역할 |
| --- | --- |
| `Models/TextAnchor.cs` | 문서 내 한 문자열에 대한 연결점. 문자열 자체, 앞뒤 문맥, 문서 오프셋을 저장한다 |
| `Models/DocumentRef.cs` | 문서를 식별하는 정보(문서 종류, 경로, 창 제목 힌트) |
| `Models/Connection.cs` | 두 TextAnchor 사이의 연결 하나. 색상과 메모를 가진다 |
| `Persistence/ConnectionStore.cs` | 문서 목록과 연결 목록을 JSON으로 저장/복원한다 |
| `Matching/AnchorMatcher.cs` | 문서를 다시 열었을 때, 저장된 문맥을 근거로 문자열의 현재 위치를 재탐색한다 |
| `Geometry/StringGeometry.cs` | 두 화면 영역 사이를 잇는 베지어 곡선(끈)의 제어점을 계산한다 |
| `Geometry/EdgeIndicatorCalculator.cs` | 연결 대상이 화면 밖으로 나갔을 때 가장자리 표시 위치를 계산한다 |

### 2.2 Windows 계층의 구성 요소

| 파일 | 역할 |
| --- | --- |
| `OverlayWindow.xaml(.cs)` | 화면 전체를 덮는 투명 오버레이. 평상시 클릭스루 상태로 아래 앱에 입력을 그대로 전달한다 |
| `Native/NativeMethods.cs` | 전역 단축키 등록, 클릭스루 창 스타일 전환을 위한 Win32 P/Invoke 선언 |
| `Services/GlobalHotkeyService.cs` | 연결 시작(Ctrl+Alt+1)/완료(Ctrl+Alt+2) 전역 단축키 처리 |
| `Services/TextLocationService.cs` | UI Automation TextPattern으로 선택 문자열과 문맥/화면 위치를 읽고, 저장된 앵커의 현재 위치를 재탐색한다 |
| `Services/DocumentIdentifierService.cs` | 창 하나로부터 안정적인 DocumentId를 만든다 |
| `Services/OpenWindowIndex.cs` | 현재 열린 최상위 창 목록을 DocumentId 기준으로 인덱싱한다 |
| `Services/ConnectionPollingService.cs` | 주기적으로 모든 연결의 화면 위치를 갱신하고, 위치가 모호한 경우 이벤트로 알린다 |
| `Services/OcrFallback.cs` | TextPattern을 지원하지 않는 콘텐츠(스캔 PDF 등)를 위한 OCR 보완 확장 지점 (MVP는 미구현) |
| `Services/TrayIconService.cs` | 트레이 아이콘을 통한 편집 모드 전환/연결 목록/종료 메뉴 |
| `ViewModels/ConnectionManagerViewModel.cs` | 두 번의 단축키 입력으로 하나의 연결을 만드는 상태 기계 |
| `Views/RestoreDialog.cs` | 위치가 모호할 때 사용자가 후보 중 하나를 직접 고르게 하는 대화상자 |
| `Views/ConnectionListWindow.cs` | 저장된 연결 목록 확인/삭제 창 |
| `App.xaml(.cs)` | 위 구성 요소를 모두 연결하는 진입점 |

## 3. 요구사항 대비표에 따른 구현 방식

| 필요한 기능 | 본 프로그램의 구현 방식 |
| --- | --- |
| 기존 앱 위에 끈 표시 | `OverlayWindow`가 `WS_EX_LAYERED \| WS_EX_TRANSPARENT` 확장 스타일을 가진 전체 화면 창으로 동작한다. 평상시에는 클릭과 키보드 입력이 아래 애플리케이션으로 그대로 전달되며, 트레이 메뉴의 "편집 모드"에서만 오버레이가 입력을 가로챈다 |
| 문자열에 연결점 고정 | `TextAnchor`가 선택 문자열, 앞뒤 문맥(각 60자), 문서 오프셋을 함께 저장한다. 화면 좌표는 저장하지 않고 매 조회 시 다시 계산한다 |
| 창 이동·스크롤 대응 | `ConnectionPollingService`가 150ms 주기로 열려 있는 각 연결 대상 창의 TextPattern을 다시 조회하여 끈의 끝점을 갱신한다 |
| 문서를 다시 열었을 때 복원 | `AnchorMatcher`가 저장된 문맥과 현재 문서 텍스트를 대조하여 위치를 재확인한다. 동일 문자열이 한 번만 나타나면 자동 복원하고, 여러 번 나타나 판단이 모호하면 `RestoreDialog`로 사용자 확인을 요청한다 |
| 화면 밖으로 사라진 문구 처리 | 연결관계 자체는 `ConnectionStore`에 그대로 남기고, `EdgeIndicatorCalculator`가 화면 가장자리에 방향 표시 마커를 그린다 |
| 연결 개수 확대 | `Connection` 목록에는 인위적인 개수 제한을 두지 않는다. 다만 매 폴링 주기마다 UI Automation을 통한 값비싼 재조회는 `OpenWindowIndex`에서 창을 찾은 앵커에 대해서만 수행되므로, 비용은 저장된 연결의 총수가 아니라 "현재 열려 있고 실제로 연결에 관련된 창의 수"에 의해 제한된다 |

## 4. 알려진 한계와 향후 과제

- **문서 식별의 안정성**: 파일 경로를 직접 확보할 수 없는 경우 창 제목으로부터 문서를 식별하므로,
  제목이 바뀌는 문서(제목 없음 상태의 새 문서, 제목 표시 방식이 다른 앱)에서는 동일 문서로
  재인식되지 않을 수 있다. Word COM 자동화, PDF 리더 플러그인, 브라우저 확장 등 앱별 연동을
  통해 실제 파일 경로/URL을 직접 확보하는 것이 다음 단계의 개선 방향이다.
- **스캔 PDF 등 이미지 기반 콘텐츠**: UI Automation의 TextPattern을 지원하지 않는 콘텐츠에서는
  문자열 위치를 읽을 수 없다. `IOcrFallbackProvider` 인터페이스로 확장 지점만 마련해 두었으며,
  Windows.Media.Ocr 또는 Tesseract 등의 실제 연동은 이번 시제품 범위에 포함하지 않았다.
- **문자 단위 이동의 정확도**: `TryLocateAnchor`에서 `MoveEndpointByUnit`으로 오프셋만큼
  이동시키는 방식은 UI Automation 구현이 문단/줄 경계를 다루는 방식에 따라 대상 앱별로 오차가
  발생할 수 있다. 실제 배포 전 Word/Adobe Reader/주요 브라우저를 대상으로 한 보정이 필요하다.
- **macOS 지원**: 사용자가 제시한 검토 자료대로 macOS 버전은 접근성 API(Accessibility API)를
  이용해 별도로 구현해야 한다. Core 계층의 JSON 저장 형식은 플랫폼 독립적으로 설계하였으므로,
  Windows와 macOS 클라이언트가 동일한 연결 데이터를 공유하는 것이 가능하다. macOS 클라이언트
  자체는 이번 시제품에 포함하지 않았다.

## 5. 빌드 및 검증 방법

### 5.1 Core 계층 단위 테스트 (모든 플랫폼에서 실행 가능)

Core 계층은 Windows 전용 API에 의존하지 않으므로, .NET 8 SDK가 설치된 환경이라면 어디서든
아래 명령으로 검증할 수 있다.

```
dotnet test tests/StringConnect.Core.Tests/StringConnect.Core.Tests.csproj
```

> 본 코드는 Windows 전용 API를 다루는 개발 환경(.NET 8 SDK, Windows 10/11)이 아닌 리눅스
> 컨테이너에서 작성되었으며, 해당 환경에는 .NET SDK를 설치할 수 없어 위 테스트를 직접 실행하지
> 못했다. 따라서 테스트 통과 여부는 이 코드를 받은 환경에서 직접 확인해야 한다.

### 5.2 전체 애플리케이션 (Windows 10/11 필요)

`StringConnect.Windows` 프로젝트는 UI Automation 클라이언트 API와 WPF에 의존하므로 Windows
환경에서만 빌드/실행된다.

```
dotnet build src/StringConnect.Windows/StringConnect.Windows.csproj
dotnet run --project src/StringConnect.Windows/StringConnect.Windows.csproj
```

실행 후 워드 프로세서나 PDF 뷰어 등에서 문자열을 선택하고 `Ctrl+Alt+1`(연결 시작)을 누른 뒤,
다른 문서에서 연결할 문자열을 선택하고 `Ctrl+Alt+2`(연결 완료)를 누르면 두 문자열 사이에 곡선
형태의 끈이 나타난다. 트레이 아이콘의 "연결 목록 보기"에서 각 연결의 삭제가 가능하며, "편집
모드 전환"으로 오버레이의 클릭스루를 일시적으로 해제할 수 있다.

전체 솔루션(`StringConnect.sln`)에는 Windows 전용 프로젝트가 포함되어 있으므로, 솔루션 전체를
대상으로 한 `dotnet build`/`dotnet test`는 Windows에서만 성공한다. 리눅스/맥에서는 위 5.1절의
Core 테스트 프로젝트만 개별적으로 지정해 실행해야 한다.

## 6. 저장 데이터 위치

연결 데이터는 `%APPDATA%\StringConnect\connections.json`에 JSON 형태로 저장된다. 파일 경로와
형식이 공개되어 있으므로, 필요 시 별도 도구로 백업하거나 검사할 수 있다.
