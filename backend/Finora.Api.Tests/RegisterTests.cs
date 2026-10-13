using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Finora.Api.Data;
using Finora.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finora.Api.Tests;

// 초대코드 회원가입 (POST /api/auth/register). 실제 PostgreSQL로 검증한다.
// 테스트끼리 DB를 함께 쓰므로, 테스트마다 서로 다른 초대코드·이메일을 만들어 겹치지 않게 한다.
public class RegisterTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithValidInvite_CreatesUserAndMarksInviteUsed()
    {
        var code = await CreateInviteAsync();
        var email = NewEmail();

        // 소문자·앞뒤 공백으로 입력해도 같은 코드로 인식해야 한다.
        var response = await RegisterAsync($"  {code.ToLowerInvariant()} ", email, "password1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var db = CreateDb();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        Assert.NotEqual("password1", user.PasswordHash); // 비밀번호는 평문이 아니라 Hash로 저장된다
        var invite = await db.InviteCodes.SingleAsync(x => x.Code == code);
        Assert.NotNull(invite.UsedAt);
        Assert.Equal(user.Id, invite.UsedByUserId);
    }

    [Fact]
    public async Task Register_WithUnknownInvite_Returns400AndCreatesNoUser()
    {
        var email = NewEmail();

        var response = await RegisterAsync("NOPE-NOPE-NOPE", email, "password1");

        await AssertFieldErrorAsync(response, "InviteCode");
        await using var db = CreateDb();
        Assert.False(await db.Users.AnyAsync(u => u.Email == email));
    }

    [Fact]
    public async Task Register_WithUsedInvite_Returns400()
    {
        var code = await CreateInviteAsync();
        await RegisterAsync(code, NewEmail(), "password1");

        // 같은 코드로 두 번째 가입 시도
        var response = await RegisterAsync(code, NewEmail(), "password1");

        await AssertFieldErrorAsync(response, "InviteCode");
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400AndKeepsInviteUnused()
    {
        var code = await CreateInviteAsync();

        var response = await RegisterAsync(code, NewEmail(), "short"); // 8자 미만, 숫자 없음

        await AssertFieldErrorAsync(response, "Password");
        // 가입이 실패했으니 초대코드는 다시 쓸 수 있어야 한다.
        await using var db = CreateDb();
        var invite = await db.InviteCodes.SingleAsync(x => x.Code == code);
        Assert.Null(invite.UsedAt);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400AndKeepsInviteUnused()
    {
        var email = NewEmail();
        await RegisterAsync(await CreateInviteAsync(), email, "password1");
        var secondCode = await CreateInviteAsync();

        var response = await RegisterAsync(secondCode, email, "password1");

        await AssertFieldErrorAsync(response, "Email");
        await using var db = CreateDb();
        Assert.Null((await db.InviteCodes.SingleAsync(x => x.Code == secondCode)).UsedAt);
    }

    // ---- 테스트 도우미 ----

    private Task<HttpResponseMessage> RegisterAsync(string inviteCode, string email, string password) =>
        _client.PostAsJsonAsync("/api/auth/register", new { inviteCode, email, password });

    // 400 + ValidationProblemDetails이고, errors에 해당 입력칸이 있는지 확인한다.
    private static async Task AssertFieldErrorAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out _), $"errors에 {field}가 없습니다: {body}");
    }

    // 테스트 서버와 같은 DB에 직접 접속하는 DbContext. (테스트 데이터 준비·결과 확인용)
    private FinoraDbContext CreateDb()
    {
        var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<FinoraDbContext>();
    }

    private async Task<string> CreateInviteAsync()
    {
        var code = $"TEST-{Guid.NewGuid():N}"[..14].ToUpperInvariant();
        await using var db = CreateDb();
        db.InviteCodes.Add(new InviteCode { Code = code, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return code;
    }

    // 가짜 이메일 (example.com은 예시 전용으로 예약된 도메인)
    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";
}
