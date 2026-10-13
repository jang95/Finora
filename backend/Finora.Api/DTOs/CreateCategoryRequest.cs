using System.ComponentModel.DataAnnotations;
using Finora.Api.Data.Entities;

namespace Finora.Api.DTOs;

// POST /api/categories 요청. 길이 제한은 화면 입력칸(maxLength=20)과 같다.
// Type을 "CategoryType?"(null 허용)로 둔 이유: null 불가 enum은 값이 빠져도 기본값(Income)이 들어가서 [Required]가 잡지 못한다.
public record CreateCategoryRequest(
    [Required(ErrorMessage = "카테고리 이름을 입력하세요.")]
    [MaxLength(20, ErrorMessage = "카테고리 이름은 20자 이하로 입력하세요.")]
    string Name,

    [Required(ErrorMessage = "수입/지출 종류를 선택하세요.")]
    CategoryType? Type);
