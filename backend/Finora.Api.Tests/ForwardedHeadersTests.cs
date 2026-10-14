using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Finora.Api.Tests;

// 프록시(Tailscale Funnel) 뒤에서 로그인 요청 횟수 제한이 실제 사용자 IP별로 세어지는지 (Phase 7, 결정 D20)
// 횟수 제한은 1분에 2회로 줄여서 확인한다. 3번째 요청이 429면 "같은 사람"으로 센 것이다.
public class ForwardedHeadersTests(FinoraApiFactory factory) : IClassFixture<FinoraApiFactory>
{
    // 테스트 서버는 접속 IP가 항상 같다. 이 헤더 값으로 "누가 앱에 직접 접속했는지"(프록시 또는 외부)를 흉내 낸다.
    private const string TestRemoteIpHeader = "X-Test-Remote-IP";

    [Fact]
    public async Task ViaTrustedProxy_CountsEachClientIpSeparately()
    {
        var client = CreateClient();

        // 127.0.0.1(같은 기기의 프록시)이 사용자 A의 요청을 두 번 전달 → A는 한도 소진
        await LoginAsync(client, remoteIp: "127.0.0.1", forwardedFor: "198.51.100.1");
        await LoginAsync(client, remoteIp: "127.0.0.1", forwardedFor: "198.51.100.1");
        var thirdFromA = await LoginAsync(client, remoteIp: "127.0.0.1", forwardedFor: "198.51.100.1");
        // 같은 프록시를 거친 사용자 B는 따로 센다
        var firstFromB = await LoginAsync(client, remoteIp: "127.0.0.1", forwardedFor: "198.51.100.2");

        Assert.Equal(HttpStatusCode.TooManyRequests, thirdFromA.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, firstFromB.StatusCode);
    }

    [Fact]
    public async Task FromUntrustedSource_IgnoresForwardedHeader()
    {
        var client = CreateClient();

        // 프록시가 아닌 곳(외부 IP)에서 직접 보낸 X-Forwarded-For는 믿지 않는다 → 값을 바꿔도 같은 사람으로 센다
        await LoginAsync(client, remoteIp: "203.0.113.9", forwardedFor: "198.51.100.1");
        await LoginAsync(client, remoteIp: "203.0.113.9", forwardedFor: "198.51.100.2");
        var third = await LoginAsync(client, remoteIp: "203.0.113.9", forwardedFor: "198.51.100.3");

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task ViaTrustedProxy_UsesOnlyRightmostValue()
    {
        var client = CreateClient();

        // 사용자가 헤더 왼쪽에 가짜 IP를 넣어도, 프록시가 오른쪽에 붙인 실제 IP(198.51.100.7)로 센다
        await LoginAsync(client, remoteIp: "127.0.0.1", forwardedFor: "10.0.0.1, 198.51.100.7");
        await LoginAsync(client, remoteIp: "127.0.0.1", forwardedFor: "10.0.0.2, 198.51.100.7");
        var third = await LoginAsync(client, remoteIp: "127.0.0.1", forwardedFor: "10.0.0.3, 198.51.100.7");

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task KnownNetworksSetting_TrustsAdditionalProxyRange()
    {
        // Docker 내부망(172.16.0.0/12)의 프록시를 설정으로 추가한 경우
        var client = CreateClient(b => b.UseSetting("ForwardedHeaders:KnownNetworks:0", "172.16.0.0/12"));

        await LoginAsync(client, remoteIp: "172.18.0.1", forwardedFor: "198.51.100.1");
        await LoginAsync(client, remoteIp: "172.18.0.1", forwardedFor: "198.51.100.1");
        var firstFromB = await LoginAsync(client, remoteIp: "172.18.0.1", forwardedFor: "198.51.100.2");

        Assert.NotEqual(HttpStatusCode.TooManyRequests, firstFromB.StatusCode);
    }

    // ---- 테스트 도우미 ----

    // 서버를 테스트마다 새로 만든다 (요청 횟수가 테스트끼리 섞이지 않게)
    // Action<IWebHostBuilder>?: 설정을 더 바꾸고 싶을 때 넘기는 함수 (없으면 null)
    private HttpClient CreateClient(Action<IWebHostBuilder>? configure = null) =>
        factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Auth:LoginPermitPerMinute", "2");
            b.ConfigureTestServices(services => services.AddTransient<IStartupFilter, TestRemoteIpStartupFilter>());
            configure?.Invoke(b);
        }).CreateClient();

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string remoteIp, string forwardedFor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new { }) };
        request.Headers.Add(TestRemoteIpHeader, remoteIp);
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        return client.SendAsync(request);
    }

    // IStartupFilter: 앱(Program.cs)의 미들웨어보다 "앞에" 미들웨어를 끼워 넣는다.
    // 여기서 접속 IP를 바꿔 두면, 뒤의 UseForwardedHeaders가 그 IP를 "앱에 직접 접속한 곳"으로 본다.
    private class TestRemoteIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(TestRemoteIpHeader, out var ip))
                    context.Connection.RemoteIpAddress = IPAddress.Parse(ip.ToString());
                await nextMiddleware(context);
            });
            next(app);
        };
    }
}
