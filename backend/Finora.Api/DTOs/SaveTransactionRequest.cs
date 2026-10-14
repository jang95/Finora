using System.ComponentModel.DataAnnotations;
using Finora.Api.Data.Entities;

namespace Finora.Api.DTOs;

// POST /api/transactions, PUT /api/transactions/{id} 요청 (등록과 수정이 같은 모양).
// Frontend의 NewTransaction(types/transaction.ts)과 같은 이름이다. accountId 등 여기 없는 값은 받더라도 무시된다. (결정 D16)
// 값 타입(DateOnly, enum, decimal)을 "?"(null 허용)로 둔 이유는 CreateCategoryRequest 설명 참고 — 빠지면 [Required]가 잡게 한다.
public record SaveTransactionRequest(
    [Required(ErrorMessage = "날짜를 입력하세요.")]
    DateOnly? Date,

    [Required(ErrorMessage = "수입/지출 종류를 선택하세요.")]
    TransactionType? Type,

    int? CategoryId,

    [Required(ErrorMessage = "금액을 입력하세요.")]
    decimal? Amount,

    [MaxLength(100, ErrorMessage = "메모는 100자 이하로 입력하세요.")]
    string? Memo);
