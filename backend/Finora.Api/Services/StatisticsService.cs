using Finora.Api.Data;
using Finora.Api.Data.Entities;
using Finora.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Finora.Api.Services;

// 대시보드·통계용 기간 집계. (계획서 Phase 6, 결정 D17)
// db.Transactions는 Global Query Filter로 항상 "현재 사용자 것"만 보이므로 남의 거래가 합계에 섞이지 않는다.
public class StatisticsService(FinoraDbContext db)
{
    // 대시보드에 보여줄 최근 거래 수
    private const int RecentCount = 5;

    // 조회할 수 있는 날짜 범위 (TransactionsController의 year 범위 2000~2100과 맞춤)
    // static readonly: 한 번 만들어 두고 바꾸지 않는 값. const는 숫자·문자열 같은 기본 값만 되고
    // DateOnly처럼 new로 만드는 값은 안 되므로 static readonly를 쓴다.
    private static readonly DateOnly MinDate = new(2000, 1, 1);
    private static readonly DateOnly MaxDate = new(2100, 12, 31);

    // 월별 합계 API로 한 번에 볼 수 있는 최대 개월 수 (그래프에 너무 많은 막대가 그려지지 않게, 3년)
    private const int MaxMonths = 36;

    // 기간 요약: from ~ to (양 끝 날짜 포함). 일간이면 from == to, 주간이면 월~일 7일.
    public async Task<ServiceResult<PeriodSummaryResponse>> GetSummaryAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var error = ValidatePeriod<PeriodSummaryResponse>(from, to);
        if (error is not null) return error;

        // 이 줄은 아직 DB를 조회하지 않는다. "기간 조건"이라는 조회 계획(IQueryable)만 만든다.
        // 아래에서 .Where(...)를 덧붙이고 SumAsync를 부를 때 비로소 SQL 한 번이 실행된다. (지연 실행)
        // 그래서 같은 period를 수입용·지출용으로 나눠 다시 써도 된다.
        var period = db.Transactions.Where(t => t.Date >= from && t.Date <= to);

        var income = await SumAsync(period, TransactionType.Income, cancellationToken);
        var expense = await SumAsync(period, TransactionType.Expense, cancellationToken);
        // 누적 총자산: 처음부터 to 날짜까지
        var totalAssets = await SumTotalAssetsAsync(db.Transactions.Where(t => t.Date <= to), cancellationToken);

        return ServiceResult<PeriodSummaryResponse>.Ok(
            new PeriodSummaryResponse(from, to, income, expense, income - expense, totalAssets));
    }

    // 기간의 카테고리별 지출 (금액 큰 순). 한 번에 한 단계만 돌려준다. (결정 D23)
    // parentId == null: 대분류별. 하위 분류의 지출도 자기 대분류에 합친다.
    // parentId == 5: 5번 카테고리 바로 아래 단계별 + 5번에 직접 기록한 지출("세부 없음") 묶음.
    public async Task<ServiceResult<List<CategoryExpenseResponse>>> GetCategoryExpensesAsync(
        DateOnly from, DateOnly to, int? parentId, CancellationToken cancellationToken)
    {
        var error = ValidatePeriod<List<CategoryExpenseResponse>>(from, to);
        if (error is not null) return error;

        // 내 카테고리 전체. 사용자당 수십 개라 한 번에 불러와서 메모리에서 계층을 따라간다.
        // Global Query Filter 때문에 남의 카테고리 Id를 parentId로 보내도 여기에 없어서 "찾을 수 없음"이 된다.
        var categories = await db.Categories
            .Select(c => new { c.Id, c.ParentId, c.Name })
            .ToDictionaryAsync(c => c.Id, cancellationToken);
        if (parentId is not null && !categories.ContainsKey(parentId.Value))
            return ServiceResult<List<CategoryExpenseResponse>>.Invalid("parentId", "카테고리를 찾을 수 없습니다.");
        // 하위 분류가 있는 카테고리 Id 모음 (눌러서 펼칠 수 있는지 표시용)
        var parentIds = categories.Values.Where(c => c.ParentId is not null).Select(c => c.ParentId!.Value).ToHashSet();

        // 1) 카테고리별 지출 합계는 DB에서 GROUP BY로 계산한다. (금액 계산은 decimal, 결정 D4)
        var sums = await db.Transactions
            .Where(t => t.Date >= from && t.Date <= to && t.Type == TransactionType.Expense)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Amount = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        // 2) 각 합계를 "이 단계의 어느 묶음에 들어가는지" 찾아서 더한다.
        //    키: (묶음 카테고리 Id, 직접 기록 묶음인지). 같은 Id라도 "교통 (세부 없음)"과 하위 묶음은 다르다.
        var buckets = new Dictionary<(int Id, bool IsDirect), decimal>();
        foreach (var sum in sums)
        {
            var key = FindBucket(sum.CategoryId);
            if (key is null) continue; // 이 단계 아래가 아닌 카테고리
            buckets[key.Value] = buckets.GetValueOrDefault(key.Value) + sum.Amount;
        }

        // 비율 = 묶음 금액 ÷ 이 단계 전체 × 100. decimal로 계산하고 소수 첫째 자리까지 반올림한다.
        // MidpointRounding.AwayFromZero: 0.05처럼 딱 중간이면 올린다 (학교에서 배운 반올림).
        //   C# 기본값은 "짝수 쪽으로"(0.25 → 0.2)라서 직접 정해 준다.
        var total = buckets.Values.Sum();
        var expenses = buckets
            .Select(b => new CategoryExpenseResponse(
                b.Key.Id,
                // 지출은 항상 카테고리가 있다 (TransactionService가 확인). 혹시 없으면 Id 0 / "미분류"
                categories.TryGetValue(b.Key.Id, out var c) ? c.Name : "미분류",
                b.Value,
                total == 0 ? 0m : Math.Round(b.Value / total * 100, 1, MidpointRounding.AwayFromZero),
                HasChildren: !b.Key.IsDirect && parentIds.Contains(b.Key.Id),
                b.Key.IsDirect))
            .OrderByDescending(x => x.Amount)
            .ThenBy(x => x.CategoryName) // 금액이 같으면 이름순 (매번 같은 순서로 보이게)
            .ToList();

        return ServiceResult<List<CategoryExpenseResponse>>.Ok(expenses);

        // 거래의 카테고리에서 부모 쪽으로 올라가다가, "부모가 parentId인 카테고리"를 만나면 그것이 이 단계의 묶음이다.
        //   예) parentId = 교통: 버스 → 대중교통(부모가 교통) → 묶음 = 대중교통
        //   예) parentId = null: 버스 → 대중교통 → 교통(부모 없음) → 묶음 = 교통
        // 거래 카테고리가 parentId 자신이면 "직접 기록" 묶음. 끝까지 올라가도 못 만나면 이 단계 아래가 아니다(null).
        (int Id, bool IsDirect)? FindBucket(int? categoryId)
        {
            if (categoryId is null)
                return parentId is null ? (0, false) : null; // 미분류는 대분류 단계에서만 보여준다
            if (categoryId == parentId)
                return (categoryId.Value, true);
            // 최대 3단계라 세 번이면 맨 위에 닿는다. 잘못된 데이터로 끝없이 돌지 않게 횟수를 제한한다.
            var currentId = categoryId;
            for (var i = 0; i < 3 && currentId is not null && categories.TryGetValue(currentId.Value, out var current); i++)
            {
                if (current.ParentId == parentId)
                    return (current.Id, false);
                currentId = current.ParentId;
            }
            return null;
        }
    }

    // 기간에 걸친 달마다의 수입/지출. 기간이 달 중간에서 시작·끝나면 그 달은 기간 안의 거래만 더한다.
    public async Task<ServiceResult<List<MonthlyTotalResponse>>> GetMonthlyTotalsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var error = ValidatePeriod<List<MonthlyTotalResponse>>(from, to);
        if (error is not null) return error;

        // 달 수: 연·월을 "0년 1월부터 몇 번째 달"로 바꿔서 뺀다. 예) 2026-05 ~ 2026-10 → 6개월
        var monthCount = (to.Year * 12 + to.Month) - (from.Year * 12 + from.Month) + 1;
        if (monthCount > MaxMonths)
            return ServiceResult<List<MonthlyTotalResponse>>.Invalid("to", $"최대 {MaxMonths}개월까지 조회할 수 있습니다.");

        // 연·월로 묶어서 합계 (GetYearlyAsync와 같은 방식. 그룹 키에 연도도 넣어서 해를 넘는 기간도 된다)
        var totals = await db.Transactions
            .Where(t => t.Date >= from && t.Date <= to)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Income = g.Sum(t => t.Type == TransactionType.Income ? t.Amount : 0m),
                Expense = g.Sum(t => t.Type == TransactionType.Expense ? t.Amount : 0m),
            })
            // (x.Year, x.Month): 값 두 개를 괄호로 묶은 튜플. 연·월 한 쌍을 Dictionary의 키 하나로 쓴다.
            .ToDictionaryAsync(x => (x.Year, x.Month), cancellationToken);

        // 거래가 없는 달도 0으로 채워서, from이 있는 달부터 to가 있는 달까지 빠짐없이 돌려준다.
        var firstMonth = new DateOnly(from.Year, from.Month, 1);
        var months = Enumerable.Range(0, monthCount)
            .Select(i =>
            {
                var m = firstMonth.AddMonths(i);
                totals.TryGetValue((m.Year, m.Month), out var total);
                return new MonthlyTotalResponse(m.Year, m.Month, total?.Income ?? 0m, total?.Expense ?? 0m);
            })
            .ToList();

        return ServiceResult<List<MonthlyTotalResponse>>.Ok(months);
    }

    // 대시보드: 최근 거래 (기간별 숫자는 GetSummaryAsync)
    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken)
    {
        // Take(n) → SQL의 LIMIT n. 정렬한 뒤 앞에서 n건만 가져온다.
        var recent = await db.Transactions
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id) // 같은 날은 나중에 등록한 것이 위
            .Take(RecentCount)
            .Select(TransactionService.ToResponse)
            .ToListAsync(cancellationToken);

        return new DashboardResponse(recent);
    }

    // ---- 내부 도우미 ----

    // 기간 날짜 검사. 문제가 없으면 null.
    // <T>를 메서드에 붙이면 제네릭 메서드: 부르는 쪽이 정한 결과 타입(ServiceResult<T>)으로 오류를 돌려준다.
    // 그래서 요약·카테고리·월별 API가 이 검사 하나를 같이 쓴다.
    // 오류 키는 주소의 이름(from/to) 그대로. ModelState는 대소문자를 구분하지 않아서,
    // "To"로 넣어도 이미 있는 "to" 항목(주소 값을 읽을 때 생김)에 합쳐진다.
    private static ServiceResult<T>? ValidatePeriod<T>(DateOnly from, DateOnly to)
    {
        if (from < MinDate || from > MaxDate)
            return ServiceResult<T>.Invalid("from", "조회할 수 없는 날짜입니다.");
        if (to < MinDate || to > MaxDate)
            return ServiceResult<T>.Invalid("to", "조회할 수 없는 날짜입니다.");
        if (from > to)
            return ServiceResult<T>.Invalid("to", "끝 날짜는 시작 날짜보다 빠를 수 없습니다.");
        return null;
    }

    // 한 종류(수입 또는 지출)의 금액 합계.
    // SumAsync → SQL의 SUM(...). 거래가 하나도 없으면 EF Core가 0을 돌려준다 (COALESCE).
    private static Task<decimal> SumAsync(IQueryable<Transaction> transactions, TransactionType type, CancellationToken cancellationToken) =>
        transactions.Where(t => t.Type == type).SumAsync(t => t.Amount, cancellationToken);

    // 총자산(가계부 기준): 수입은 +, 지출은 −로 더한다. 이체(Transfer)는 0.
    // 조건 ? 값1 : 값2 (삼항 연산자)를 이어 쓰면 "if / else if / else"와 같다.
    // EF Core가 SQL의 SUM(CASE WHEN ... THEN ... ELSE ... END)로 바꿔서 DB가 한 번에 계산한다.
    // 0m: 숫자 뒤의 m은 decimal 값이라는 표시 (없으면 int로 본다)
    private static Task<decimal> SumTotalAssetsAsync(IQueryable<Transaction> transactions, CancellationToken cancellationToken) =>
        transactions.SumAsync(t => t.Type == TransactionType.Income ? t.Amount
                                 : t.Type == TransactionType.Expense ? -t.Amount
                                 : 0m, cancellationToken);
}
