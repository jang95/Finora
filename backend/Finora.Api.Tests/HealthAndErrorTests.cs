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

    [Fact]
    public async Task UnknownRoute_Returns404ProblemDetailsWithTraceId()
    {
        var response = await _client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
    }
}
