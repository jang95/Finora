using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Finora.Api.Tests;

// 비밀번호 변경 POST /api/me/password (Phase 6)
public class PasswordChangeTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private const string NewPassword = "newpassword2";

    private readonly WebApplicationFactory<Program> _server = factory.WithWebHostBuilder(b =>
    {
        b.UseSetting("Auth:LoginPermitPerMinute", "1000");
        // SecurityStamp 확인 간격을 0으로: 요청마다 확인해서 "다른 기기 로그아웃"을 1분 기다리지 않고 바로 확인한다
        b.ConfigureTestServices(services =>
            services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero));
    });

    [Fact]
    public async Task Change_Succeeds_OnlyNewPasswordWorksAndThisDeviceStaysLoggedIn()
    {
        var client = await _server.RegisterAndLoginAsync();
        var email = await EmailOfAsync(client);

        var response = await ChangeAsync(client, TestServerExtensions.Password, NewPassword);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        // 바꾼 기기는 로그인 유지
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/me")).StatusCode);
        // 예전 비밀번호로는 로그인 안 되고, 새 비밀번호로는 된다
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, TestServerExtensions.Password)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await LoginAsync(email, NewPassword)).StatusCode);
    }

    [Fact]
    public async Task Change_LogsOutOtherDevices()
    {
        var deviceA = await _server.RegisterAndLoginAsync();
        var email = await EmailOfAsync(deviceA);
        // 같은 계정으로 다른 기기(B)에서도 로그인
        var deviceB = _server.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.NoContent,
            (await deviceB.PostAsJsonAsync("/api/auth/login", new { email, password = TestServerExtensions.Password })).StatusCode);

        await ChangeAsync(deviceA, TestServerExtensions.Password, NewPassword);

        // B의 Cookie는 예전 SecurityStamp라서 끊긴다. A는 새 Cookie를 받아서 유지된다
        Assert.Equal(HttpStatusCode.Unauthorized, (await deviceB.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await deviceA.GetAsync("/api/me")).StatusCode);
    }

    [Fact]
    public async Task Change_WithWrongCurrentPassword_Returns400OnCurrentPassword()
    {
        var client = await _server.RegisterAndLoginAsync();

        var response = await ChangeAsync(client, "wrongpassword9", NewPassword);

        await AssertFieldErrorAsync(response, "CurrentPassword");
    }

    [Theory]
    [InlineData("short1")]                           // 8자 미만
    [InlineData("nodigitpassword")]                  // 숫자 없음
    [InlineData(TestServerExtensions.Password)]      // 현재 비밀번호와 같음
    public async Task Change_WithInvalidNewPassword_Returns400OnNewPassword(string newPassword)
    {
        var client = await _server.RegisterAndLoginAsync();

        var response = await ChangeAsync(client, TestServerExtensions.Password, newPassword);

        await AssertFieldErrorAsync(response, "NewPassword");
        // 실패했으면 비밀번호는 그대로
        Assert.Equal(HttpStatusCode.NoContent, (await LoginAsync(await EmailOfAsync(client), TestServerExtensions.Password)).StatusCode);
    }

    [Fact]
    public async Task Change_WhenNotLoggedIn_Returns401()
    {
        var response = await ChangeAsync(_server.CreateHttpsClient(), TestServerExtensions.Password, NewPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- 테스트 도우미 ----

    private static Task<HttpResponseMessage> ChangeAsync(HttpClient client, string currentPassword, string newPassword) =>
        client.PostAsJsonAsync("/api/me/password", new { currentPassword, newPassword });

    // 새 client로 로그인해 본다 (다른 기기에서 로그인하는 것과 같음)
    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _server.CreateHttpsClient().PostAsJsonAsync("/api/auth/login", new { email, password });

    private static async Task<string> EmailOfAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/me")).GetProperty("email").GetString()!;

    private static async Task AssertFieldErrorAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out _), $"errors에 {field}가 없습니다: {body}");
    }
}
