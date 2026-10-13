using Finora.Api.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Finora.Api.Data;

// DbContext = C# 코드와 DB 사이의 연결 창구. 여기서 테이블 목록과 세부 규칙을 정한다.
// 생성자의 options(접속 문자열 등)는 Program.cs에서 등록한 값이 자동으로 들어온다. (의존성 주입)
// IdentityDbContext<FinoraUser>를 상속하면 Identity 테이블(AspNetUsers, AspNetRoles 등)이 함께 만들어진다.
// 계획서 8장의 Users 테이블 = AspNetUsers
public class FinoraDbContext(DbContextOptions<FinoraDbContext> options) : IdentityDbContext<FinoraUser>(options)
{
    // DbSet<T> = 테이블 하나. db.InviteCodes.Where(...) 처럼 조회한다.
    public DbSet<InviteCode> InviteCodes => Set<InviteCode>();

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
    }
}
