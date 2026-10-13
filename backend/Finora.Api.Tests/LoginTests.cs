using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Finora.Api.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Finora.Api.Tests;

// 로그인 / 로그아웃 / GET /api/me. 실제 PostgreSQL로 검증한다.
public class LoginTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private const string Password = "password1";

    // 이 클래스는 로그인을 여러 번 하므로 요청 횟수 제한을 크게 올린 서버를 쓴다. (제한 자체는 LoginRateLimitTests에서 확인)
    private readonly WebApplicationFactory<Program> _server =
        factory.WithWebHostBuilder(b => b.UseSetting("Auth:LoginPermitPerMinute", "1000"));

    [Fact]
    public async Task Login_WithCorrectPassword_SetsSecureCookieAndMeReturnsEmail()
    {
        var email = await CreateUserAsync();
        var client = CreateClient();

        var response = await LoginAsync(client, email, Password);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        // Set-Cookie 헤더에 보안 속성이 붙어 있는지 확인한다.
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("finora.auth="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        // 같은 client는 받은 Cookie를 다음 요청에 자동으로 보낸다. (브라우저와 같음)
        var me = await client.GetFromJsonAsync<JsonElement>("/api/me");
        Assert.Equal(email, me.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_WithRememberMe_SetsPersistentCookie()
    {
        var email = await CreateUserAsync();

        var withoutRemember = await LoginAsync(CreateClient(), email, Password, rememberMe: false);
        var withRemember = await LoginAsync(CreateClient(), email, Password, rememberMe: true);

        // expires가 있으면 브라우저를 닫아도 남는 Cookie, 없으면 브라우저를 닫을 때 사라지는 Cookie다.
        Assert.DoesNotContain("expires=", AuthCookie(withoutRemember), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", AuthCookie(withRemember), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithWrongPasswordOrUnknownEmail_Returns401WithSameMessage()
    {
        var email = await CreateUserAsync();
        var client = CreateClient();

        var wrongPassword = await LoginAsync(client, email, "wrong-password1");
        var unknownEmail = await LoginAsync(client, $"nobody-{Guid.NewGuid():N}@example.com", Password);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        // 가입된 이메일인지 알 수 없도록 문구가 같아야 한다.
        Assert.Equal(await DetailAsync(wrongPassword), await DetailAsync(unknownEmail));
    }

    [Fact]
    public async Task Login_After5Failures_IsLockedEvenWithCorrectPassword()
    {
        var email = await CreateUserAsync();
        var client = CreateClient();

        for (var i = 0; i < 5; i++)
            await LoginAsync(client, email, "wrong-password1");
        var response = await LoginAsync(client, email, Password); // 6번째는 맞는 비밀번호

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("잠겼습니다", await DetailAsync(response));
    }

    [Fact]
    public async Task Logout_ThenMe_Returns401()
    {
        var email = await CreateUserAsync();
        var client = CreateClient();
        await LoginAsync(client, email, Password);

        var logout = await client.PostAsync("/api/auth/logout", null);
        var me = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Me_WhenNotLoggedIn_Returns401()
    {
        var response = await CreateClient().GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // 로그인 전에는 401이었던 없는 주소가, 로그인 후에는 404 ProblemDetails가 된다. (HealthAndErrorTests 참고)
    [Fact]
    public async Task UnknownRoute_WhenLoggedIn_Returns404ProblemDetails()
    {
        var client = CreateClient();
        await LoginAsync(client, await CreateUserAsync(), Password);

        var response = await client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // ---- 테스트 도우미 ----

    // Secure Cookie는 HTTPS 요청에만 다시 보내지므로 테스트 client 주소도 https로 둔다.
    private HttpClient CreateClient() =>
        _server.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password, bool rememberMe = false) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password, rememberMe });

    private static string AuthCookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("finora.auth="));

    private static async Task<string?> DetailAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString();

    // 가입 API를 거치지 않고 UserManager로 바로 사용자를 만든다. (가입은 RegisterTests에서 검증)
    private async Task<string> CreateUserAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        using var scope = _server.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FinoraUser>>();
        var result = await userManager.CreateAsync(new FinoraUser { UserName = email, Email = email, CreatedAt = DateTime.UtcNow }, Password);
        Assert.True(result.Succeeded);
        return email;
    }
}

// 로그인 요청 횟수 제한. 기본값(1분에 10회)을 그대로 쓰는 별도 서버에서 확인한다.
// 횟수 제한은 입력 검사·DB 조회보다 먼저 적용되므로, 빈 요청(400)으로 세도 된다. DB가 필요 없어 가짜 DB 서버를 쓴다.
public class LoginRateLimitTests(FinoraApiFactory factory) : IClassFixture<FinoraApiFactory>
{
    [Fact]
    public async Task Login_MoreThan10TimesPerMinute_Returns429()
    {
        var client = factory.CreateClient();
        var body = new { };

        for (var i = 0; i < 10; i++)
            await client.PostAsJsonAsync("/api/auth/login", body);
        var response = await client.PostAsJsonAsync("/api/auth/login", body);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
}
