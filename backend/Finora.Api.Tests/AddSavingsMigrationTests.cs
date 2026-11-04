using Finora.Api.Auth;
using Finora.Api.Data;
using Finora.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.PostgreSql;

namespace Finora.Api.Tests;

// Migration AddSavings가 기존 사용자에게 "이자" 카테고리를 넣는 SQL을 확인한다 (결정 D24).
// PostgresApiFactory는 빈 DB에 Migration을 모두 적용하므로 "이미 가입한 사용자"가 없어 이 SQL이 검사되지 않는다.
// 그래서 컨테이너를 따로 띄워 ① 이전 Migration까지 적용 → ② 기존 사용자 데이터 넣기 → ③ AddSavings 적용 순서로 운영 DB 상황을 재현한다.
public class AddSavingsMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task AddSavings_AddsInterestCategoryToExistingUsersOnce()
    {
        await using var db = CreateDb();
        // IMigrator: Migration을 이름으로 골라 적용하는 EF Core 내부 서비스. MigrateAsync("이름")은 그 Migration까지만 적용한다.
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("AddCategoryParent");

        // 기존 사용자 3명: 기본 수입 카테고리가 있는 사람 / 이미 "이자"를 만든 사람 / 수입 카테고리가 하나도 없는 사람
        var withDefaults = await AddUserAsync(db, "급여", "용돈", "기타수입");
        var alreadyHasInterest = await AddUserAsync(db, "급여", "이자");
        var noIncome = await AddUserAsync(db);

        await migrator.MigrateAsync("AddSavings");

        // 위에서 넣은 엔티티는 DbContext가 기억하고 있어서(변경 추적) 새로 조회한 값이 섞일 수 있다. 새 DbContext로 확인한다.
        await using var check = CreateDb();
        var interest = await check.Categories.IgnoreQueryFilters()
            .Where(c => c.Name == "이자" && c.Type == CategoryType.Income && c.ParentId == null)
            .ToListAsync();

        // 맨 뒤(SortOrder 최댓값 + 1)에 붙는다
        Assert.Equal(3, interest.Single(c => c.UserId == withDefaults).SortOrder);
        // 이미 있으면 건너뛴다 (그대로 1개, 원래 순서 유지)
        Assert.Equal(1, interest.Single(c => c.UserId == alreadyHasInterest).SortOrder);
        // 수입 카테고리가 없으면 0부터
        Assert.Equal(0, interest.Single(c => c.UserId == noIncome).SortOrder);
    }

    // 로그인 요청이 아니므로 현재 사용자 없이 만든다 (Global Query Filter 때문에 조회는 IgnoreQueryFilters로)
    private FinoraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        return new FinoraDbContext(options, new CurrentUser(new HttpContextAccessor()));
    }

    // 사용자 한 명과 수입 대분류들을 넣고 사용자 Id를 돌려준다. params: 개수를 정하지 않고 여러 값을 받는다 (TS의 ...rest와 비슷)
    private static async Task<string> AddUserAsync(FinoraDbContext db, params string[] incomeNames)
    {
        var user = new FinoraUser { UserName = $"user-{Guid.NewGuid():N}@example.com" };
        db.Users.Add(user);
        var now = DateTime.UtcNow;
        db.Categories.AddRange(incomeNames.Select((name, index) => new Category
        {
            UserId = user.Id,
            Name = name,
            Type = CategoryType.Income,
            SortOrder = index,
            CreatedAt = now,
            UpdatedAt = now,
        }));
        await db.SaveChangesAsync();
        return user.Id;
    }
}
