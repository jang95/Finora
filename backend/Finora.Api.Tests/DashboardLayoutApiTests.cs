using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Finora.Api.Tests;

// 위젯 배치 API (결정 D27): 저장 전 빈 배열, 저장 → 조회, 덮어쓰기, 잘못된 값 거부
public class DashboardLayoutApiTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private readonly WebApplicationFactory<Program> _server =
        factory.WithWebHostBuilder(b => b.UseSetting("Auth:LoginPermitPerMinute", "1000"));

    // Frontend와 같은 모양 (camelCase JSON은 System.Text.Json이 대소문자 무시로 맞춰 준다)
    private record ItemDto(string Id, string Size, bool Hidden);

    private record LayoutDto(string Page, List<ItemDto> Items);

    [Fact]
    public async Task Get_BeforeSaving_ReturnsEmptyItems()
    {
        var client = await _server.RegisterAndLoginAsync();

        var layout = await client.GetFromJsonAsync<LayoutDto>("/api/dashboard-layouts/statistics");

        Assert.Equal("statistics", layout!.Page);
        Assert.Empty(layout.Items);
    }

    [Fact]
    public async Task Save_ThenGet_ReturnsSameItemsInOrder()
    {
        var client = await _server.RegisterAndLoginAsync();
        var items = new List<ItemDto>
        {
            new("categoryExpenses", "full", false),
            new("summary", "full", true),
            new("monthlyTotals", "half", false),
        };

        var save = await client.PutAsJsonAsync("/api/dashboard-layouts/statistics", new { items });
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);

        var layout = await client.GetFromJsonAsync<LayoutDto>("/api/dashboard-layouts/statistics");
        Assert.Equal(items, layout!.Items); // record는 값으로 비교된다 (순서·내용 모두 같아야 함)
    }

    [Fact]
    public async Task Save_Twice_OverwritesInsteadOfAddingRow()
    {
        var client = await _server.RegisterAndLoginAsync();
        await client.PutAsJsonAsync("/api/dashboard-layouts/statistics",
            new { items = new[] { new ItemDto("summary", "full", false) } });

        var second = await client.PutAsJsonAsync("/api/dashboard-layouts/statistics",
            new { items = new[] { new ItemDto("monthlyTotals", "half", false) } });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var layout = await client.GetFromJsonAsync<LayoutDto>("/api/dashboard-layouts/statistics");
        Assert.Single(layout!.Items);
        Assert.Equal("monthlyTotals", layout.Items[0].Id);
    }

    // [Theory] + [InlineData]: 같은 테스트를 여러 입력으로 반복한다
    [Theory]
    [InlineData("nope", "half")] // 모르는 위젯
    [InlineData("summary", "huge")] // 모르는 크기
    public async Task Save_RejectsUnknownIdOrSize(string id, string size)
    {
        var client = await _server.RegisterAndLoginAsync();

        var response = await client.PutAsJsonAsync("/api/dashboard-layouts/statistics",
            new { items = new[] { new ItemDto(id, size, false) } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Save_RejectsDuplicateWidget()
    {
        var client = await _server.RegisterAndLoginAsync();

        var response = await client.PutAsJsonAsync("/api/dashboard-layouts/statistics",
            new { items = new[] { new ItemDto("summary", "full", false), new ItemDto("summary", "half", false) } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnknownPage_Returns404()
    {
        var client = await _server.RegisterAndLoginAsync();

        var get = await client.GetAsync("/api/dashboard-layouts/nope");
        var put = await client.PutAsJsonAsync("/api/dashboard-layouts/nope", new { items = Array.Empty<ItemDto>() });

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
    }
}
