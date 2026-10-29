using Finora.Api.Auth;
using Finora.Api.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Finora.Api.Data;

// DbContext = C# 코드와 DB 사이의 연결 창구. 여기서 테이블 목록과 세부 규칙을 정한다.
// 생성자의 options(접속 문자열 등)는 Program.cs에서 등록한 값이 자동으로 들어온다. (의존성 주입)
// IdentityDbContext<FinoraUser>를 상속하면 Identity 테이블(AspNetUsers, AspNetRoles 등)이 함께 만들어진다.
// 계획서 8장의 Users 테이블 = AspNetUsers
// currentUser: 지금 요청한 사용자. 사용자 소유 테이블의 조회 범위를 정하는 데 쓴다. (아래 Global Query Filter)
public class FinoraDbContext(DbContextOptions<FinoraDbContext> options, CurrentUser currentUser)
    : IdentityDbContext<FinoraUser>(options)
{
    // DbSet<T> = 테이블 하나. db.InviteCodes.Where(...) 처럼 조회한다.
    public DbSet<InviteCode> InviteCodes => Set<InviteCode>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    // Global Query Filter에서 쓰는 현재 사용자 Id.
    // DbContext의 속성으로 두어야 EF Core가 쿼리를 실행할 때마다 그 시점의 값을 읽는다. (모델은 한 번만 만들어지고 재사용되므로)
    private string? CurrentUserId => currentUser.Id;

    // 클래스 모양만으로 정할 수 없는 규칙(길이 제한, 고유 인덱스 등)을 적는 곳.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity 테이블 설정을 먼저 적용한다. 빠뜨리면 Identity 테이블 구성이 깨진다.
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InviteCode>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(32);
            // 같은 초대코드가 두 번 생기지 않도록 고유(unique) 인덱스
            e.HasIndex(x => x.Code).IsUnique();
            // 외래 키: UsedByUserId는 AspNetUsers.Id를 가리킨다.
            // 사용자가 삭제되면 코드 기록은 남기고 UsedByUserId만 비운다(SetNull).
            e.HasOne<FinoraUser>()
                .WithMany()
                .HasForeignKey(x => x.UsedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(50);
            // enum을 숫자(0, 1)가 아니라 "Income"/"Expense" 문자열로 저장한다. DB를 직접 봐도 뜻을 알 수 있다.
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
            // 한 사용자 안에서 같은 종류·같은 부모 아래에 같은 이름은 하나만 (다른 사용자와는 겹쳐도 된다, 결정 D23)
            // 대분류는 ParentId가 null이다. 보통 고유 인덱스는 null끼리를 "서로 다른 값"으로 봐서 대분류 이름이 겹쳐도 통과시킨다.
            // AreNullsDistinct(false) = PostgreSQL의 NULLS NOT DISTINCT: null끼리도 같은 값으로 보고 중복을 막는다.
            e.HasIndex(x => new { x.UserId, x.Type, x.ParentId, x.Name }).IsUnique().AreNullsDistinct(false);
            // 하위 분류가 있는 카테고리는 지울 수 없다. CategoryService도 먼저 확인하지만, DB가 최종으로 막는다.
            // NoAction인 이유는 아래 Transaction → Category와 같다 (사용자 삭제 Cascade 때 순서 문제).
            e.HasOne(x => x.Parent)
                .WithMany()
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.NoAction);
            // 사용자가 삭제되면 그 사용자의 카테고리도 함께 삭제한다(Cascade).
            e.HasOne<FinoraUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Global Query Filter: 이 테이블을 조회할 때마다 "WHERE UserId = 현재 사용자" 조건이 자동으로 붙는다.
            // 코드에서 Where를 깜빡해도 다른 사람 데이터가 나오지 않는다. 로그인 전(null)이면 아무것도 조회되지 않는다.
            // 꼭 필요한 곳(테스트, 관리 작업)에서만 IgnoreQueryFilters()로 끌 수 있다.
            e.HasQueryFilter(x => x.UserId == CurrentUserId);
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            // numeric(18,2): 전체 18자리 중 소수 2자리. float/double은 쓰지 않는다 (결정 D4)
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Memo).HasMaxLength(100);
            // 월별 조회는 항상 "내 거래 중 이 기간"이라 (UserId, Date) 인덱스로 빨리 찾는다.
            e.HasIndex(x => new { x.UserId, x.Date });
            e.HasOne<FinoraUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // 거래에서 쓰는 카테고리는 지울 수 없다. CategoryService도 먼저 확인하지만, DB가 최종으로 막는다.
            // Restrict가 아니라 NoAction인 이유: 사용자를 지우면 거래·카테고리가 함께 Cascade로 지워지는데,
            // Restrict는 행마다 즉시 검사해서 카테고리가 먼저 지워지는 순간 실패할 수 있다. NoAction은 문장이 끝날 때 검사한다.
            e.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasQueryFilter(x => x.UserId == CurrentUserId);
        });
    }
}
