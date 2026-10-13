using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Finora.Api.Tests;

// 카테고리 추가·이름 변경·삭제 API. (다른 사용자 데이터 접근은 UserIsolationTests에서 확인)
// 테스트마다 새 사용자로 시작해서 서로의 카테고리가 섞이지 않는다.
public class CategoryApiTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private readonly WebApplicationFactory<Program> _server =
        factory.WithWebHostBuilder(b => b.UseSetting("Auth:LoginPermitPerMinute", "1000"));

    private record CategoryDto(int Id, string Name, string Type);

    [Fact]
    public async Task Create_AddsCategoryAtEndOfSameType()
    {
        var client = await _server.RegisterAndLoginAsync();

        var response = await client.PostAsJsonAsync("/api/categories", new { name = "  반려동물 ", type = "Expense" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.Equal("반려동물", created!.Name); // 앞뒤 공백은 지운다
        Assert.Equal("Expense", created.Type);
        // 목록에서 지출 카테고리 중 맨 뒤에 있어야 한다.
        var expenses = (await GetAllAsync(client)).Where(c => c.Type == "Expense").ToList();
        Assert.Equal(created.Id, expenses[^1].Id); // [^1] = 마지막 항목
    }

    [Fact]
    public async Task Create_DuplicateNameInSameType_Returns400()
    {
        var client = await _server.RegisterAndLoginAsync();

        // "식비"는 기본 지출 카테고리에 이미 있다.
        var response = await client.PostAsJsonAsync("/api/categories", new { name = "식비", type = "Expense" });

        await AssertFieldErrorAsync(response, "Name");
    }

    [Fact]
    public async Task Create_SameNameInOtherType_IsAllowed()
    {
        var client = await _server.RegisterAndLoginAsync();

        // 지출에 있는 "식비"를 수입 쪽에 만드는 것은 허용 (고유 조건은 종류+이름)
        var response = await client.PostAsJsonAsync("/api/categories", new { name = "식비", type = "Income" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutTypeOrWithBlankName_Returns400()
    {
        var client = await _server.RegisterAndLoginAsync();

        var noType = await client.PostAsJsonAsync("/api/categories", new { name = "여행" });
        var blankName = await client.PostAsJsonAsync("/api/categories", new { name = "   ", type = "Expense" });

        await AssertFieldErrorAsync(noType, "Type");
        await AssertFieldErrorAsync(blankName, "Name");
    }

    [Fact]
    public async Task Rename_ChangesNameButNotType()
    {
        var client = await _server.RegisterAndLoginAsync();
        var target = (await GetAllAsync(client)).First(c => c.Name == "교통");

        var response = await client.PutAsJsonAsync($"/api/categories/{target.Id}", new { name = "교통비" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var after = (await GetAllAsync(client)).Single(c => c.Id == target.Id);
        Assert.Equal("교통비", after.Name);
        Assert.Equal(target.Type, after.Type);
    }

    [Fact]
    public async Task Rename_ToExistingName_Returns400()
    {
        var client = await _server.RegisterAndLoginAsync();
        var target = (await GetAllAsync(client)).First(c => c.Name == "교통");

        var response = await client.PutAsJsonAsync($"/api/categories/{target.Id}", new { name = "식비" });

        await AssertFieldErrorAsync(response, "Name");
    }

    [Fact]
    public async Task Rename_ToSameName_IsAllowed()
    {
        var client = await _server.RegisterAndLoginAsync();
        var target = (await GetAllAsync(client)).First(c => c.Name == "교통");

        // 자기 자신과는 중복으로 보지 않는다.
        var response = await client.PutAsJsonAsync($"/api/categories/{target.Id}", new { name = "교통" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesCategory()
    {
        var client = await _server.RegisterAndLoginAsync();
        var target = (await GetAllAsync(client)).First(c => c.Name == "경조사");

        var response = await client.DeleteAsync($"/api/categories/{target.Id}");
        var again = await client.DeleteAsync($"/api/categories/{target.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.DoesNotContain(await GetAllAsync(client), c => c.Id == target.Id);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode); // 이미 지운 것은 404
    }

    // ---- 테스트 도우미 ----

    private static async Task<List<CategoryDto>> GetAllAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!;

    private static async Task AssertFieldErrorAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out _), $"errors에 {field}가 없습니다: {body}");
    }
}
