using Finora.Api.DTOs;
using Finora.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Finora.Api.Controllers;

// 저금 API (결정 D24). 로그인해야 호출할 수 있다. (fallback policy)
// 규칙(종류별 기록, 이자 → 가계부 수입 연결 등)은 SavingService가 처리하고, 여기서는 결과를 HTTP 응답으로만 바꾼다.
[ApiController]
[Route("api/savings")]
public class SavingsController(SavingService savings) : ControllerBase
{
    // GET /api/savings — 내 저금 목록 (현재 금액 포함)
    [HttpGet]
    public Task<List<SavingResponse>> GetAll(CancellationToken cancellationToken) =>
        savings.GetAllAsync(cancellationToken);

    // GET /api/savings/{id} — 저금 + 기록 목록. 내 저금이 아니면 404
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SavingDetailResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var result = await savings.GetAsync(id, cancellationToken);
        if (result.IsNotFound) return NotFound();
        return result.Value!;
    }

    // POST /api/savings — 등록 (이미 가입한 상품이면 openingAmount로 기존 금액). 201 + 저장된 저금
    [HttpPost]
    public async Task<ActionResult<SavingResponse>> Create([FromBody] SaveSavingRequest request, CancellationToken cancellationToken)
    {
        var result = await savings.CreateAsync(request, cancellationToken);
        if (result.Errors is not null) return this.ValidationErrors(result.Errors);
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    // PUT /api/savings/{id} — 수정
    [HttpPut("{id:int}")]
    public async Task<ActionResult<SavingResponse>> Update(int id, [FromBody] SaveSavingRequest request, CancellationToken cancellationToken) =>
        ToResponse(await savings.UpdateAsync(id, request, cancellationToken));

    // PUT /api/savings/{id}/status — 상태 변경 (진행 중 / 완료 / 보관)
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<SavingResponse>> UpdateStatus(int id, [FromBody] UpdateSavingStatusRequest request, CancellationToken cancellationToken) =>
        ToResponse(await savings.UpdateStatusAsync(id, request, cancellationToken));

    // DELETE /api/savings/{id} — 삭제 (기록과 연결된 이자 수입 거래도 함께)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await savings.DeleteAsync(id, cancellationToken);
        if (result.IsNotFound) return NotFound();
        return NoContent();
    }

    // POST /api/savings/{id}/close — 만기 수령·중도해지 (적금·예금). 저금 + 기록 목록을 돌려준다
    [HttpPost("{id:int}/close")]
    public async Task<ActionResult<SavingDetailResponse>> Close(int id, [FromBody] CloseSavingRequest request, CancellationToken cancellationToken) =>
        ToResponse(await savings.CloseAsync(id, request, cancellationToken));

    // POST /api/savings/{id}/entries — 기록 추가 (입금 / 출금 / 이자). 201 + 저장된 기록
    [HttpPost("{id:int}/entries")]
    public async Task<ActionResult<SavingEntryResponse>> CreateEntry(int id, [FromBody] SaveSavingEntryRequest request, CancellationToken cancellationToken)
    {
        var result = await savings.CreateEntryAsync(id, request, cancellationToken);
        if (result.Value is not null) return StatusCode(StatusCodes.Status201Created, result.Value);
        return ToResponse(result);
    }

    // PUT /api/savings/{id}/entries/{entryId} — 기록 수정 (이자면 연결된 가계부 수입도 함께)
    [HttpPut("{id:int}/entries/{entryId:int}")]
    public async Task<ActionResult<SavingEntryResponse>> UpdateEntry(
        int id, int entryId, [FromBody] SaveSavingEntryRequest request, CancellationToken cancellationToken) =>
        ToResponse(await savings.UpdateEntryAsync(id, entryId, request, cancellationToken));

    // DELETE /api/savings/{id}/entries/{entryId} — 기록 삭제 (이자면 연결된 가계부 수입도 함께)
    [HttpDelete("{id:int}/entries/{entryId:int}")]
    public async Task<IActionResult> DeleteEntry(int id, int entryId, CancellationToken cancellationToken)
    {
        var result = await savings.DeleteEntryAsync(id, entryId, cancellationToken);
        if (result.IsNotFound) return NotFound();
        if (result.ConflictMessage is not null) return Problem(detail: result.ConflictMessage, statusCode: StatusCodes.Status409Conflict);
        return NoContent();
    }

    // ServiceResult → HTTP 응답 (404 / 400 입력칸 오류 / 409 충돌 / 200 + 값). 이 Controller의 여러 API가 같은 순서로 바꾼다.
    // 제네릭 메서드 <T>: 응답 값의 타입만 다르고 바꾸는 방법은 같아서 하나로 쓴다 (ServiceResult<T> 설명 참고)
    private ActionResult<T> ToResponse<T>(ServiceResult<T> result)
    {
        if (result.IsNotFound) return NotFound();
        if (result.Errors is not null) return this.ValidationErrors(result.Errors);
        // 409 Conflict: 요청은 맞지만 지금 상태에서는 할 수 없다 (CategoriesController.Delete 설명 참고)
        if (result.ConflictMessage is not null) return Problem(detail: result.ConflictMessage, statusCode: StatusCodes.Status409Conflict);
        return result.Value!;
    }
}
