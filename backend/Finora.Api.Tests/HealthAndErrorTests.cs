using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Finora.Api.Tests;

// IClassFixture<T>: 이 클래스의 테스트들이 FinoraApiFactory(테스트용 서버) 하나를 같이 쓴다.
// 테스트마다 서버를 새로 띄우면 느리기 때문이다.
public class HealthAndErrorTests(FinoraApiFactory factory) : IClassFixture<FinoraApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // [Fact] = 입력 없이 한 번 실행되는 테스트 하나.
    // 이름은 "무엇을_어떤 상황에서_어떻게 되어야 하는지"로 짓는다.
    [Fact]
    public async Task Health_WhenDatabaseUnavailable_Returns503WithDatabaseUnavailable()
    {
        var response = await _client.GetAsync("/api/health");

        // Assert = 기대한 값과 실제 값이 다르면 테스트 실패
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("degraded", body.GetProperty("status").GetString());
        Assert.Equal("unavailable", body.GetProperty("database").GetString());
    }

    // 로그인 전에는 없는 주소도 404가 아니라 401이다. (fallback policy는 주소가 없는 요청에도 적용된다)
    // 어떤 주소가 존재하는지 로그인 전에는 알려주지 않는다. 로그인 상태의 404 확인은 로그인 API가 생긴 뒤 추가한다.
    [Fact]
    public async Task UnknownRoute_WhenNotLoggedIn_Returns401ProblemDetailsWithTraceId()
    {
        var response = await _client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(401, body.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
    }
}
