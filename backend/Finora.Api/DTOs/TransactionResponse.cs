using Finora.Api.Data.Entities;

namespace Finora.Api.DTOs;

// 거래 한 건 응답. Frontend의 Transaction 타입(types/transaction.ts)과 같은 이름이다.
// 계좌 관련 값(accountId, accountName 등)은 Phase 8에서 추가한다. 값이 null인 속성은 JSON에서 빠진다 (Program.cs 설정)
public record TransactionResponse(
    int Id,
    DateOnly Date,
    TransactionType Type,
    int? CategoryId,
    string? CategoryName,
    decimal Amount,
    string? Memo);
