using Finora.Api.DTOs;
using Finora.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Finora.Api.Controllers;

// 카테고리 API. 로그인해야 호출할 수 있다. (fallback policy)
// Controller는 HTTP 요청/응답만 담당하고, 규칙(중복 금지 등)은 CategoryService가 처리한다. (계획서 7장)
[ApiController]
[Route("api/categories")]
public class CategoriesController(CategoryService categories) : ControllerBase
{
    // GET /api/categories — 내 카테고리 목록 (Global Query Filter가 현재 사용자 것만 돌려준다)
    [HttpGet]
    public Task<List<CategoryResponse>> GetAll(CancellationToken cancellationToken) =>
        categories.GetAllAsync(cancellationToken);

    // POST /api/categories — 추가. 201 Created + 만든 카테고리
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await categories.CreateAsync(request, cancellationToken);
        if (result.Errors is not null) return this.ValidationErrors(result.Errors);
        // 201은 "새로 만들어짐"을 뜻한다. 개별 조회 API가 없어서 위치(Location) 없이 본문만 돌려준다.
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    // PUT /api/categories/{id} — 이름 변경
    // {id:int}: 주소의 이 부분을 int로 받는다. 숫자가 아니면 이 API에 연결되지 않는다(404).
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> Rename(int id, [FromBody] RenameCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await categories.RenameAsync(id, request, cancellationToken);
        if (result.IsNotFound) return NotFound();
        if (result.Errors is not null) return this.ValidationErrors(result.Errors);
        return result.Value!;
    }

    // DELETE /api/categories/{id} — 삭제. 204 No Content
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await categories.DeleteAsync(id, cancellationToken);
        if (result.IsNotFound) return NotFound();
        // 409 Conflict: 요청은 맞지만 지금 상태(거래에서 사용 중)에서는 할 수 없다. 문구는 detail로 보낸다.
        if (result.ConflictMessage is not null) return Problem(detail: result.ConflictMessage, statusCode: StatusCodes.Status409Conflict);
        return NoContent();
    }
}
