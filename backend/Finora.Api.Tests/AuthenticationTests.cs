using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Finora.Api.Tests;

// 인증 기본 설정(Program.cs)을 검증한다.
public class AuthenticationTests(FinoraApiFactory factory) : IClassFixture<FinoraApiFactory>
{
    // WithWebHostBuilder: 기존 테스트 서버 설정에 이 테스트만의 설정을 덧붙인 서버를 만든다.
    // AddApplicationPart: 테스트 프로젝트에 있는 ProtectedProbeController도 서버가 Controller로 인식하게 한다.
    private readonly HttpClient _client = factory
        .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ProtectedProbeController).Assembly)))
        .CreateClient();

    [Fact]
    public async Task EndpointWithoutAttribute_WhenNotLoggedIn_Returns401NotRedirect()
    {
        var response = await _client.GetAsync("/api/test/protected");

        // 로그인 페이지로 이동(302)하지 않고 401 + ProblemDetails로 응답해야 한다.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(401, body.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Health_WhenNotLoggedIn_IsAllowed()
    {
        var response = await _client.GetAsync("/api/health");

        // [AllowAnonymous]라서 401이 아니다. (테스트 DB는 연결되지 않으므로 503)
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}

// 테스트 전용 Controller. [Authorize]를 일부러 붙이지 않았다.
// fallback policy 덕분에 아무 표시가 없어도 로그인이 필요해야 한다.
[ApiController]
[Route("api/test/protected")]
public class ProtectedProbeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
