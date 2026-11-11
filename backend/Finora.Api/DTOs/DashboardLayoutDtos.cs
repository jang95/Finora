using System.ComponentModel.DataAnnotations;

namespace Finora.Api.DTOs;

// 위젯 배치 API (결정 D27). Frontend types/dashboardLayout.ts의 WidgetLayoutItem과 같은 모양.
// Id·Size는 enum이 아니라 문자열이다: 위젯 종류는 Frontend가 정하고 Backend는 "아는 값인지"만 검사한다 (DashboardLayoutService).

// 배치 한 줄: 어떤 위젯을(Id), 어떤 크기로(Size: "half" | "full"), 숨겼는지(Hidden). 배열의 순서가 화면 순서
public record WidgetLayoutItemDto(
    [Required(ErrorMessage = "위젯 id가 없습니다.")]
    string Id,
    [Required(ErrorMessage = "위젯 크기가 없습니다.")]
    string Size,
    bool Hidden);

// PUT /api/dashboard-layouts/{page} 요청: 배치 전체를 통째로 바꾼다 (부분 수정 없음)
public record SaveDashboardLayoutRequest(
    [Required(ErrorMessage = "배치가 없습니다.")]
    List<WidgetLayoutItemDto> Items);

// GET/PUT 응답. 저장된 배치가 없으면 Items는 빈 배열 → Frontend가 기본 배치로 채운다
public record DashboardLayoutResponse(string Page, List<WidgetLayoutItemDto> Items);
