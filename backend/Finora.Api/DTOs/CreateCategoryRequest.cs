using System.ComponentModel.DataAnnotations;
using Finora.Api.Data.Entities;

namespace Finora.Api.DTOs;

// POST /api/categories 요청. 길이 제한은 화면 입력칸(maxLength=20)과 같다.
// ParentId가 없으면 대분류, 있으면 그 카테고리의 하위 분류를 만든다. (최대 3단계, 결정 D23)
// Type은 대분류를 만들 때만 필요하다. 하위 분류는 상위 카테고리의 종류를 그대로 따르므로 보내도 쓰지 않는다.
// 그래서 [Required] 대신 CategoryService가 "대분류인데 Type이 없으면" 오류를 낸다.
// Type을 "CategoryType?"(null 허용)로 둔 이유: null 불가 enum은 값이 빠져도 기본값(Income)이 들어가서 빠졌는지 알 수 없다.
public record CreateCategoryRequest(
    [Required(ErrorMessage = "카테고리 이름을 입력하세요.")]
    [MaxLength(20, ErrorMessage = "카테고리 이름은 20자 이하로 입력하세요.")]
    string Name,

    CategoryType? Type,

    int? ParentId = null);
