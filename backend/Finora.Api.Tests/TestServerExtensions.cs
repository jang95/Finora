using System.Net;
using System.Net.Http.Json;
using Finora.Api.Data;
using Finora.Api.Data.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Finora.Api.Tests;

// 여러 테스트 클래스가 함께 쓰는 도우미 (확장 메서드: server.RegisterAndLoginAsync()처럼 부른다)
public static class TestServerExtensions
{
    public const string Password = "password1";

    // Secure Cookie는 HTTPS 요청에만 다시 보내지므로 테스트 client 주소도 https로 둔다.
    public static HttpClient CreateHttpsClient(this WebApplicationFactory<Program> server) =>
        server.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    // 초대코드 발급 → 가입 API → 로그인 API. 로그인 Cookie를 가진 client를 돌려준다. (새 사용자 한 명)
    public static async Task<HttpClient> RegisterAndLoginAsync(this WebApplicationFactory<Program> server)
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var inviteCode = $"TEST-{Guid.NewGuid():N}"[..14].ToUpperInvariant();
        await using (var db = server.CreateDb())
        {
            db.InviteCodes.Add(new InviteCode { Code = inviteCode, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        var client = server.CreateHttpsClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new { inviteCode, email, password = Password });
        Assert.Equal(HttpStatusCode.NoContent, register.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        return client;
    }

    // 테스트 서버와 같은 DB에 직접 접속하는 DbContext (테스트 데이터 준비·결과 확인용).
    // 로그인한 요청이 아니므로 Global Query Filter 때문에 사용자 소유 데이터는 보이지 않는다. 필요하면 IgnoreQueryFilters()를 쓴다.
    public static FinoraDbContext CreateDb(this WebApplicationFactory<Program> server) =>
        server.Services.CreateScope().ServiceProvider.GetRequiredService<FinoraDbContext>();
}
