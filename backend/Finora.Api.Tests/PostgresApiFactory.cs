using Finora.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Finora.Api.Tests;

// 실제 PostgreSQL이 필요한 테스트용 서버 (결정 D13).
// Testcontainers가 테스트 시작 때 Docker로 빈 PostgreSQL 컨테이너를 띄우고, 끝나면 지운다.
// 개발용 DB(finora-db)와는 완전히 별개라 서로 데이터가 섞이지 않는다. 실행 중에는 Docker가 켜져 있어야 한다.
//
// IAsyncLifetime(xUnit): 이 Factory를 쓰는 테스트들이 시작되기 전 InitializeAsync, 모두 끝난 뒤 DisposeAsync를 호출한다.
public class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // 운영·개발과 같은 버전(compose.yaml)으로 테스트한다.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // 마이그레이션을 적용해 운영과 같은 테이블 구조를 만든다.
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<FinoraDbContext>().Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 컨테이너가 시작될 때 정해진 접속 문자열을 쓴다. (User Secrets 값보다 우선)
        builder.UseSetting("ConnectionStrings:Finora", _postgres.GetConnectionString());
    }

    // WebApplicationFactory에도 DisposeAsync가 있어서, IAsyncLifetime 쪽을 따로 구현한다. (명시적 인터페이스 구현)
    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
