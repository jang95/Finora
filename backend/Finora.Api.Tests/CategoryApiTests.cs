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

    private record CategoryDto(int Id, string Name, string Type, int? ParentId);

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

        // 지출에 있는 "식비"를 수입 쪽에 만드는 것은 허용 (고유 조건은 종류+상위 카테고리+이름)
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

    // ---- 하위 분류 (결정 D23) ----

    [Fact]
    public async Task CreateChild_FollowsParentType_AndIsAddedUnderParent()
    {
        var client = await _server.RegisterAndLoginAsync();
        var transport = (await GetAllAsync(client)).Single(c => c.Name == "교통" && c.ParentId == null);

        // 지출 대분류 아래에 type = Income을 보내도 상위와 같은 Expense로 만든다.
        var first = await CreateAsync(client, "대중교통", transport.Id, type: "Income");
        var second = await CreateAsync(client, "택시", transport.Id);

        Assert.Equal(transport.Id, first.ParentId);
        Assert.Equal("Expense", first.Type);
        // 같은 부모 아래에서 추가한 순서대로 나온다.
        var children = (await GetAllAsync(client)).Where(c => c.ParentId == transport.Id).Select(c => c.Name);
        Assert.Equal(["대중교통", "택시"], children);
        Assert.Equal(transport.Id, second.ParentId);
    }

    [Fact]
    public async Task CreateChild_UpToThreeLevels_FourthReturns400()
    {
        var client = await _server.RegisterAndLoginAsync();
        var level1 = (await GetAllAsync(client)).Single(c => c.Name == "교통" && c.ParentId == null);
        var level2 = await CreateAsync(client, "대중교통", level1.Id);
        var level3 = await CreateAsync(client, "버스", level2.Id);

        var level4 = await client.PostAsJsonAsync("/api/categories", new { name = "마을버스", parentId = level3.Id });

        Assert.Equal(level2.Id, level3.ParentId);
        await AssertFieldErrorAsync(level4, "ParentId");
    }

    [Fact]
    public async Task CreateChild_WithUnknownParent_Returns400()
    {
        var client = await _server.RegisterAndLoginAsync();

        var response = await client.PostAsJsonAsync("/api/categories", new { name = "버스", parentId = 999999 });

        await AssertFieldErrorAsync(response, "ParentId");
    }

    [Fact]
    public async Task CreateChild_DuplicateNameUnderSameParent_Returns400_ButOtherParentIsAllowed()
    {
        var client = await _server.RegisterAndLoginAsync();
        var all = await GetAllAsync(client);
        var transport = all.Single(c => c.Name == "교통");
        var culture = all.Single(c => c.Name == "문화·여가");
        await CreateAsync(client, "기차", transport.Id);

        var sameParent = await client.PostAsJsonAsync("/api/categories", new { name = "기차", parentId = transport.Id });
        var otherParent = await client.PostAsJsonAsync("/api/categories", new { name = "기차", parentId = culture.Id });
        // 대분류 이름과 같은 이름의 하위 분류도 위치가 다르므로 허용한다.
        var sameAsRoot = await client.PostAsJsonAsync("/api/categories", new { name = "식비", parentId = transport.Id });

        await AssertFieldErrorAsync(sameParent, "Name");
        Assert.Equal(HttpStatusCode.Created, otherParent.StatusCode);
        Assert.Equal(HttpStatusCode.Created, sameAsRoot.StatusCode);
    }

    [Fact]
    public async Task RenameChild_ToSiblingName_Returns400()
    {
        var client = await _server.RegisterAndLoginAsync();
        var transport = (await GetAllAsync(client)).Single(c => c.Name == "교통");
        await CreateAsync(client, "택시", transport.Id);
        var bus = await CreateAsync(client, "버스", transport.Id);

        var response = await client.PutAsJsonAsync($"/api/categories/{bus.Id}", new { name = "택시" });

        await AssertFieldErrorAsync(response, "Name");
    }

    [Fact]
    public async Task Delete_WithChildren_Returns409_UntilChildrenAreDeleted()
    {
        var client = await _server.RegisterAndLoginAsync();
        var transport = (await GetAllAsync(client)).Single(c => c.Name == "교통");
        var child = await CreateAsync(client, "택시", transport.Id);

        var blocked = await client.DeleteAsync($"/api/categories/{transport.Id}");
        var deleteChild = await client.DeleteAsync($"/api/categories/{child.Id}");
        var deleteParent = await client.DeleteAsync($"/api/categories/{transport.Id}");

        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteChild.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteParent.StatusCode);
    }

    // ---- 테스트 도우미 ----

    private static async Task<CategoryDto> CreateAsync(HttpClient client, string name, int parentId, string? type = null)
    {
        var response = await client.PostAsJsonAsync("/api/categories", new { name, type, parentId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    private static async Task<List<CategoryDto>> GetAllAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!;

    private static async Task AssertFieldErrorAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out _), $"errors에 {field}가 없습니다: {body}");
    }
}
