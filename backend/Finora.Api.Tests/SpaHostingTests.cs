using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Finora.Api.Tests;

// React 빌드 파일 제공 (Phase 7, 결정 D19). 테스트마다 임시 폴더를 wwwroot로 쓰고 가짜 빌드 파일을 넣는다.
// IDisposable: xUnit이 테스트가 끝날 때 Dispose()를 불러 준다 → 임시 폴더를 지운다.
public class SpaHostingTests : IClassFixture<FinoraApiFactory>, IDisposable
{
    private const string IndexHtml = "<!doctype html><title>Finora</title>";

    private readonly string _webRoot = Directory.CreateTempSubdirectory("finora-wwwroot-").FullName;
    private readonly HttpClient _client;

    public SpaHostingTests(FinoraApiFactory factory)
    {
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), IndexHtml);
        Directory.CreateDirectory(Path.Combine(_webRoot, "assets"));
        File.WriteAllText(Path.Combine(_webRoot, "assets", "index-abc123.js"), "console.log('finora')");

        // UseWebRoot: 정적 파일 폴더(기본 wwwroot)를 바꾼다
        _client = factory.WithWebHostBuilder(b => b.UseWebRoot(_webRoot)).CreateHttpsClient();
    }

    public void Dispose() => Directory.Delete(_webRoot, recursive: true);

    [Theory]
    [InlineData("/")]
    [InlineData("/login")]                 // React Router 화면 주소 → 서버에는 없는 파일이지만 index.html
    [InlineData("/transactions")]
    public async Task ScreenRoutes_WhenNotLoggedIn_ReturnIndexHtmlWithNoCache(string path)
    {
        var response = await _client.GetAsync(path);

        // 로그인 전에도 화면은 열려야 한다 (안 그러면 로그인 화면 자체가 401)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(IndexHtml, await response.Content.ReadAsStringAsync());
        Assert.Equal("no-cache", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task HashedAsset_IsCachedForAYear()
    {
        var response = await _client.GetAsync("/assets/index-abc123.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("public, max-age=31536000, immutable", response.Headers.CacheControl?.ToString());
    }

    // 없는 파일은 index.html로 대신하지 않는다 (JS 자리에 HTML이 오면 브라우저에서 알아보기 어려운 오류가 난다).
    // 어떤 주소에도 맞지 않으므로 다른 없는 주소처럼 기본 정책(로그인 필요)이 적용되어 로그인 전에는 401이다.
    [Fact]
    public async Task MissingFile_IsNotServedAsIndexHtml()
    {
        var response = await _client.GetAsync("/assets/does-not-exist.js");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(IndexHtml, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnknownApi_WhenNotLoggedIn_Returns401NotIndexHtml()
    {
        var response = await _client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}

// 로그인한 상태에서 없는 /api 주소 → index.html(200)이 아니라 404 (화면 fallback에 API 주소가 섞이지 않는지)
public class ApiFallbackTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>, IDisposable
{
    private readonly string _webRoot = Directory.CreateTempSubdirectory("finora-wwwroot-").FullName;

    public void Dispose() => Directory.Delete(_webRoot, recursive: true);

    [Fact]
    public async Task UnknownApi_WhenLoggedIn_Returns404ProblemDetails()
    {
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), "<!doctype html>");
        var server = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Auth:LoginPermitPerMinute", "1000");
            b.UseWebRoot(_webRoot);
        });
        var client = await server.RegisterAndLoginAsync();

        var response = await client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
