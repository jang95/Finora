using System.ComponentModel.DataAnnotations;
using Finora.Api.DTOs;
using Finora.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Finora.Api.Controllers;

// 가계부 거래 API. 로그인해야 호출할 수 있다. (fallback policy)
[ApiController]
[Route("api/transactions")]
public class TransactionsController(TransactionService transactions) : ControllerBase
{
    // GET /api/transactions/monthly?year=2026&month=10 — 월 요약 + 날짜별 묶음 (목록·달력 탭)
    // [FromQuery]: 주소의 ?year=...&month=... 값을 받는다. [Range]를 벗어나면 [ApiController]가 400으로 응답한다.
    // [BindRequired]: 값이 아예 없어도 400. (없으면 int 기본값 0이 들어가고 [Range] 검사도 건너뛰어서, 0월로 날짜를 만들다 500이 났다)
    [HttpGet("monthly")]
    public Task<MonthlyLedgerResponse> GetMonthly(
        [FromQuery, BindRequired, Range(2000, 2100)] int year,
        [FromQuery, BindRequired, Range(1, 12)] int month,
        CancellationToken cancellationToken) =>
        transactions.GetMonthlyAsync(year, month, cancellationToken);

    // GET /api/transactions/yearly?year=2026 — 한 해 요약 + 1~12월 합계 (월별 탭)
    [HttpGet("yearly")]
    public Task<YearlyLedgerResponse> GetYearly([FromQuery, BindRequired, Range(2000, 2100)] int year, CancellationToken cancellationToken) =>
        transactions.GetYearlyAsync(year, cancellationToken);

    // POST /api/transactions — 등록. 201 + 저장된 거래 (카테고리 이름 포함)
    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> Create([FromBody] SaveTransactionRequest request, CancellationToken cancellationToken)
    {
        var result = await transactions.CreateAsync(request, cancellationToken);
        if (result.Errors is not null) return this.ValidationErrors(result.Errors);
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    // PUT /api/transactions/{id} — 수정. 내 거래가 아니면 404
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TransactionResponse>> Update(int id, [FromBody] SaveTransactionRequest request, CancellationToken cancellationToken)
    {
        var result = await transactions.UpdateAsync(id, request, cancellationToken);
        if (result.IsNotFound) return NotFound();
        if (result.Errors is not null) return this.ValidationErrors(result.Errors);
        return result.Value!;
    }

    // DELETE /api/transactions/{id} — 삭제. 내 거래가 아니면 404
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await transactions.DeleteAsync(id, cancellationToken);
        if (result.IsNotFound) return NotFound();
        return NoContent();
    }
}
