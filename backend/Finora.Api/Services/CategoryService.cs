using Finora.Api.Auth;
using Finora.Api.Data;
using Finora.Api.Data.Entities;
using Finora.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Finora.Api.Services;

// 카테고리 조회·추가·이름 변경·삭제.
// db.Categories는 Global Query Filter 때문에 항상 "현재 사용자 것"만 보인다. 그래서 다른 사람의 Id로 요청하면
// 찾지 못한 것과 똑같이 404가 된다. (403이 아니라 404 — 남의 데이터가 있는지조차 알려주지 않음, 계획서 9장)
public class CategoryService(FinoraDbContext db, CurrentUser currentUser)
{
    private const string DuplicateMessage = "같은 이름의 카테고리가 이미 있습니다.";

    // 대분류 › 중분류 › 소분류 (결정 D23)
    private const int MaxDepth = 3;

    public async Task<List<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.Categories
            .OrderBy(c => c.Type)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.Id)
            // Select로 DTO 모양으로 바꾼다. DB에서도 필요한 컬럼만 조회한다.
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Type, c.ParentId))
            .ToListAsync(cancellationToken);

    public async Task<ServiceResult<CategoryResponse>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (name == "")
            return ServiceResult<CategoryResponse>.Invalid(nameof(request.Name), "카테고리 이름을 입력하세요.");

        CategoryType type;
        if (request.ParentId is null)
        {
            if (request.Type is null)
                return ServiceResult<CategoryResponse>.Invalid(nameof(request.Type), "수입/지출 종류를 선택하세요.");
            type = request.Type.Value;
        }
        else
        {
            // 상위 카테고리도 db.Categories에서 찾으므로 Global Query Filter가 적용된다.
            // 다른 사용자의 카테고리 Id를 보내면 찾지 못해서 "없음"이 된다. (남의 카테고리 아래에 만들 수 없음)
            var parentDepth = await GetDepthAsync(request.ParentId.Value, cancellationToken);
            if (parentDepth is null)
                return ServiceResult<CategoryResponse>.Invalid(nameof(request.ParentId), "상위 카테고리를 찾을 수 없습니다.");
            if (parentDepth >= MaxDepth)
                return ServiceResult<CategoryResponse>.Invalid(nameof(request.ParentId), "소분류 아래에는 더 나눌 수 없습니다.");
            // 하위 분류는 상위 카테고리와 같은 종류다. 요청의 Type은 쓰지 않는다.
            type = await db.Categories.Where(c => c.Id == request.ParentId).Select(c => c.Type).SingleAsync(cancellationToken);
        }

        if (await IsDuplicateAsync(type, request.ParentId, name, exceptId: null, cancellationToken))
            return ServiceResult<CategoryResponse>.Invalid(nameof(request.Name), DuplicateMessage);

        // 새 카테고리는 같은 부모 아래(대분류는 같은 종류의 대분류 중) 맨 뒤에 붙인다. (?? -1: 하나도 없으면 0번부터)
        var lastOrder = await db.Categories
            .Where(c => c.Type == type && c.ParentId == request.ParentId)
            .MaxAsync(c => (int?)c.SortOrder, cancellationToken) ?? -1;
        var now = DateTime.UtcNow;
        var category = new Category
        {
            // 소유자는 요청 값이 아니라 인증 정보에서 정한다. (CLAUDE.md 규칙)
            UserId = currentUser.Id ?? throw new InvalidOperationException("로그인한 사용자가 없습니다."),
            ParentId = request.ParentId,
            Name = name,
            Type = type,
            SortOrder = lastOrder + 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Categories.Add(category);

        if (!await TrySaveAsync(cancellationToken))
            return ServiceResult<CategoryResponse>.Invalid(nameof(request.Name), DuplicateMessage);
        return ServiceResult<CategoryResponse>.Ok(ToResponse(category));
    }

    public async Task<ServiceResult<CategoryResponse>> RenameAsync(int id, RenameCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
            return ServiceResult<CategoryResponse>.NotFound();

        var name = request.Name.Trim();
        if (name == "")
            return ServiceResult<CategoryResponse>.Invalid(nameof(request.Name), "카테고리 이름을 입력하세요.");
        if (await IsDuplicateAsync(category.Type, category.ParentId, name, exceptId: id, cancellationToken))
            return ServiceResult<CategoryResponse>.Invalid(nameof(request.Name), DuplicateMessage);

        // 조회한 Entity의 값을 바꾸고 SaveChanges하면 EF Core가 바뀐 컬럼만 UPDATE한다. (변경 추적)
        category.Name = name;
        category.UpdatedAt = DateTime.UtcNow;
        if (!await TrySaveAsync(cancellationToken))
            return ServiceResult<CategoryResponse>.Invalid(nameof(request.Name), DuplicateMessage);
        return ServiceResult<CategoryResponse>.Ok(ToResponse(category));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
            return ServiceResult<bool>.NotFound();

        // 하위 분류가 있으면 지울 수 없다. 하위 분류를 먼저 지워야 한다. (DB 외래키도 막는다)
        if (await db.Categories.AnyAsync(c => c.ParentId == id, cancellationToken))
            return ServiceResult<bool>.Conflict("하위 분류가 있는 카테고리는 삭제할 수 없습니다. 하위 분류를 먼저 삭제하세요.");

        // 거래에서 쓰는 카테고리는 지울 수 없다. 지우면 그 거래들이 "미분류"가 되어 통계가 틀어진다.
        if (await db.Transactions.AnyAsync(t => t.CategoryId == id, cancellationToken))
            return ServiceResult<bool>.Conflict("거래에서 사용 중인 카테고리는 삭제할 수 없습니다.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    // 내 카테고리 중 같은 종류·같은 부모 아래에 같은 이름이 있는지. 이름 변경 때는 자기 자신(exceptId)은 빼고 본다.
    // 대분류끼리는 parentId가 둘 다 null이라 c.ParentId == parentId가 참이 된다. (EF Core가 null 비교를 IS NULL로 바꿔 준다)
    private Task<bool> IsDuplicateAsync(CategoryType type, int? parentId, string name, int? exceptId, CancellationToken cancellationToken) =>
        db.Categories.AnyAsync(c => c.Type == type && c.ParentId == parentId && c.Name == name && c.Id != exceptId, cancellationToken);

    // 카테고리가 몇 단계인지 (대분류 1, 중분류 2, 소분류 3). 내 카테고리가 아니거나 없으면 null.
    // 최대 3단계라 부모를 두 번까지만 따라가면 된다. 한 번의 조회로 Parent.Parent까지 JOIN한다.
    private async Task<int?> GetDepthAsync(int id, CancellationToken cancellationToken)
    {
        var chain = await db.Categories
            .Where(c => c.Id == id)
            .Select(c => new { c.ParentId, GrandParentId = c.Parent!.ParentId })
            .SingleOrDefaultAsync(cancellationToken);
        if (chain is null) return null;
        if (chain.ParentId is null) return 1;
        return chain.GrandParentId is null ? 2 : 3;
    }

    private static CategoryResponse ToResponse(Category c) => new(c.Id, c.Name, c.Type, c.ParentId);

    // 저장하다가 고유 인덱스(UserId, Type, ParentId, Name) 위반이 나면 false.
    // 위의 중복 확인과 저장 사이에 같은 이름이 먼저 저장되는 경우(두 창에서 동시에 추가 등)를 DB가 최종으로 막아 준다.
    private async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return false;
        }
    }
}
