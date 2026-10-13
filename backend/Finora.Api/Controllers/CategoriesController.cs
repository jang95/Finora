using Finora.Api.Data;
using Finora.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Finora.Api.Controllers;

// 카테고리 API. 로그인해야 호출할 수 있다. (fallback policy)
// 추가·이름 변경·삭제는 Phase 5에서 만든다. (결정 D15)
[ApiController]
[Route("api/categories")]
public class CategoriesController(FinoraDbContext db) : ControllerBase
{
    // GET /api/categories — 내 카테고리 목록
    // Where(UserId == ...)를 쓰지 않아도 Global Query Filter가 현재 사용자 것만 돌려준다. (FinoraDbContext)
    [HttpGet]
    public async Task<List<CategoryResponse>> GetAll(CancellationToken cancellationToken) =>
        await db.Categories
            .OrderBy(c => c.Type)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.Id)
            // Select로 DTO 모양으로 바꾼다. DB에서도 필요한 컬럼만 조회한다.
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Type))
            .ToListAsync(cancellationToken);
}
