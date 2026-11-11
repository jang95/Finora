using System.Text.Json;
using Finora.Api.Auth;
using Finora.Api.Data;
using Finora.Api.Data.Entities;
using Finora.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Finora.Api.Services;

// 위젯 배치 조회·저장 (결정 D27).
// db.DashboardLayouts는 Global Query Filter 때문에 항상 "현재 사용자 것"만 보인다 (CategoryService 설명 참고).
public class DashboardLayoutService(FinoraDbContext db, CurrentUser currentUser, TimeProvider time)
{
    // 배치를 저장할 수 있는 화면. Frontend 화면이 늘면 여기도 늘린다
    public static readonly IReadOnlySet<string> Pages = new HashSet<string> { "statistics" };

    // 화면별로 "아는 위젯 id" (Frontend types/dashboardLayout.ts의 WIDGET_IDS와 같아야 한다).
    // Frontend에서 새 위젯을 만들면 여기에도 추가한다. 모르는 id를 막아서 이상한 값이 DB에 쌓이지 않게 한다
    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> WidgetIdsByPage = new Dictionary<string, IReadOnlySet<string>>
    {
        ["statistics"] = new HashSet<string> { "summary", "monthlyTotals", "categoryExpenses", "dailyExpense", "cumulativeCompare", "categoryDonut", "categoryMonthly" },
    };

    private static readonly HashSet<string> Sizes = ["half", "full"];

    // JSON ↔ 객체 변환 규칙. Web = 속성 이름을 camelCase("id", "size")로 쓴다 (Frontend·API 응답과 같은 모양)
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // 저장된 배치. 없으면 빈 배열 (Frontend가 기본 배치로 채운다)
    public async Task<ServiceResult<DashboardLayoutResponse>> GetAsync(string page, CancellationToken cancellationToken)
    {
        if (!Pages.Contains(page))
            return ServiceResult<DashboardLayoutResponse>.NotFound();

        var layout = await db.DashboardLayouts.SingleOrDefaultAsync(l => l.Page == page, cancellationToken);
        var items = layout is null ? [] : Deserialize(layout.LayoutJson);
        return ServiceResult<DashboardLayoutResponse>.Ok(new DashboardLayoutResponse(page, items));
    }

    // 배치 전체를 저장한다. 행이 없으면 만들고(insert), 있으면 덮어쓴다(update) — 합쳐서 "upsert"라고 부른다
    public async Task<ServiceResult<DashboardLayoutResponse>> SaveAsync(string page, SaveDashboardLayoutRequest request, CancellationToken cancellationToken)
    {
        if (!Pages.Contains(page))
            return ServiceResult<DashboardLayoutResponse>.NotFound();

        var error = Validate(page, request.Items);
        if (error is not null)
            return ServiceResult<DashboardLayoutResponse>.Invalid(nameof(request.Items), error);

        var now = time.GetUtcNow().UtcDateTime;
        var layout = await db.DashboardLayouts.SingleOrDefaultAsync(l => l.Page == page, cancellationToken);
        if (layout is null)
        {
            layout = new DashboardLayout
            {
                UserId = currentUser.Id ?? throw new InvalidOperationException("로그인한 사용자가 없습니다."),
                Page = page,
            };
            db.DashboardLayouts.Add(layout);
        }
        layout.LayoutJson = JsonSerializer.Serialize(request.Items, JsonOptions);
        layout.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResult<DashboardLayoutResponse>.Ok(new DashboardLayoutResponse(page, request.Items));
    }

    // 문제가 있으면 문구, 없으면 null
    private static string? Validate(string page, List<WidgetLayoutItemDto> items)
    {
        var knownIds = WidgetIdsByPage[page];
        // 아는 위젯보다 많으면 분명 잘못된 요청 (같은 id 반복 등)
        if (items.Count > knownIds.Count)
            return "위젯이 너무 많습니다.";

        var seen = new HashSet<string>();
        foreach (var item in items)
        {
            if (!knownIds.Contains(item.Id))
                return $"알 수 없는 위젯입니다: {item.Id}";
            if (!Sizes.Contains(item.Size))
                return $"알 수 없는 크기입니다: {item.Size}";
            // HashSet.Add는 이미 있던 값이면 false를 돌려준다 → 중복 검사와 추가를 한 번에
            if (!seen.Add(item.Id))
                return $"같은 위젯이 두 번 있습니다: {item.Id}";
        }
        return null;
    }

    // DB의 JSON 문자열 → 객체 목록. 저장할 때 검사한 값이라 보통 실패하지 않지만, 혹시 깨져 있으면 빈 배열(= 기본 배치)로 처리한다
    private static List<WidgetLayoutItemDto> Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<WidgetLayoutItemDto>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
