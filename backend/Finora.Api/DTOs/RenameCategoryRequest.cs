using System.ComponentModel.DataAnnotations;

namespace Finora.Api.DTOs;

// PUT /api/categories/{id} 요청. 이름만 바꾼다.
// 종류(수입/지출)는 바꾸지 않는다 — 이미 쓴 거래의 의미가 달라지기 때문. (Phase 2 categoryService 규칙과 같음)
public record RenameCategoryRequest(
    [Required(ErrorMessage = "카테고리 이름을 입력하세요.")]
    [MaxLength(20, ErrorMessage = "카테고리 이름은 20자 이하로 입력하세요.")]
    string Name);
