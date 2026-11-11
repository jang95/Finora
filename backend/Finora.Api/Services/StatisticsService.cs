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

    // 일별 합계 API의 최대 날 수 (연간 기간 366일까지. 그보다 긴 직접 지정 기간은 점이 너무 많다)
    private const int MaxDays = 366;

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
        // 누적 총자산의 출발점: 첫 달 전까지의 누적 (그 뒤는 달마다 수입 − 지출을 더해 간다 → 쿼리 한 번으로 끝)
        var totalAssets = await SumTotalAssetsAsync(db.Transactions.Where(t => t.Date < firstMonth), cancellationToken);
        var months = new List<MonthlyTotalResponse>(monthCount);
        for (var i = 0; i < monthCount; i++)
        {
            var m = firstMonth.AddMonths(i);
            totals.TryGetValue((m.Year, m.Month), out var total);
            var income = total?.Income ?? 0m;
            var expense = total?.Expense ?? 0m;
            totalAssets += income - expense;
            // 저축률: 수입이 0이면 나눌 수 없으므로 null. decimal? 는 "값이 없을 수도 있는 decimal"
            decimal? savingRate = income == 0 ? null : Math.Round((income - expense) / income * 100, 1, MidpointRounding.AwayFromZero);
            months.Add(new MonthlyTotalResponse(m.Year, m.Month, income, expense, savingRate, totalAssets));
        }

        return ServiceResult<List<MonthlyTotalResponse>>.Ok(months);
    }

    // 위젯 ⑥ 도넛: 기간 지출을 대분류 상위 top개 + "기타"로 나눈 비중
    public async Task<ServiceResult<CategorySharesResponse>> GetCategorySharesAsync(DateOnly from, DateOnly to, int top, CancellationToken cancellationToken)
    {
        var error = ValidatePeriod<CategorySharesResponse>(from, to) ?? ValidateTop<CategorySharesResponse>(top);
        if (error is not null) return error;

        var (rootNames, sums) = await RootMonthlyExpensesAsync(from, to, cancellationToken);

        // 달 구분 없이 대분류별 합계. GroupBy(묶음 키).Sum: C#의 LINQ도 SQL의 GROUP BY처럼 쓸 수 있다 (여기는 메모리 안)
        var totals = sums
            .GroupBy(s => s.Key.Root)
            .Select(g => (Root: g.Key, Amount: g.Sum(s => s.Value)))
            .OrderByDescending(x => x.Amount)
            .ThenBy(x => rootNames[x.Root])
            .ToList();
        var total = totals.Sum(x => x.Amount);

        var items = totals.Take(top)
            .Select(x => new CategoryShareResponse(x.Root == 0 ? null : x.Root, rootNames[x.Root], x.Amount, Ratio(x.Amount, total)))
            .ToList();
        // 상위에 못 든 나머지는 "기타" 하나로. 0원이면 넣지 않는다 (빈 조각이 생기지 않게)
        var other = totals.Skip(top).Sum(x => x.Amount);
        if (other > 0)
            items.Add(new CategoryShareResponse(null, OtherName, other, Ratio(other, total)));

        return ServiceResult<CategorySharesResponse>.Ok(new CategorySharesResponse(total, items));
    }

    // 위젯 ⑦ 쌓은 막대: 기간에 걸친 달마다 대분류 상위 top개 + "기타"의 지출
    public async Task<ServiceResult<CategoryMonthlyResponse>> GetCategoryMonthlyAsync(DateOnly from, DateOnly to, int top, CancellationToken cancellationToken)
    {
        var error = ValidatePeriod<CategoryMonthlyResponse>(from, to) ?? ValidateTop<CategoryMonthlyResponse>(top);
        if (error is not null) return error;
        var monthCount = (to.Year * 12 + to.Month) - (from.Year * 12 + from.Month) + 1;
        if (monthCount > MaxMonths)
            return ServiceResult<CategoryMonthlyResponse>.Invalid("to", $"최대 {MaxMonths}개월까지 조회할 수 있습니다.");

        var (rootNames, sums) = await RootMonthlyExpensesAsync(from, to, cancellationToken);

        // 어느 대분류를 선으로 둘지는 기간 전체 합계로 정한다 (달마다 상위가 바뀌면 색이 왔다 갔다 해서 읽기 어렵다)
        var ranked = sums
            .GroupBy(s => s.Key.Root)
            .Select(g => (Root: g.Key, Amount: g.Sum(s => s.Value)))
            .OrderByDescending(x => x.Amount)
            .ThenBy(x => rootNames[x.Root])
            .ToList();
        var topRoots = ranked.Take(top).Select(x => x.Root).ToList();
        var otherRoots = ranked.Skip(top).Select(x => x.Root).ToHashSet();
        var hasOther = ranked.Skip(top).Any(x => x.Amount > 0);

        var series = topRoots.Select(r => new CategorySeriesResponse(r == 0 ? null : r, rootNames[r])).ToList();
        if (hasOther) series.Add(new CategorySeriesResponse(null, OtherName));

        var firstMonth = new DateOnly(from.Year, from.Month, 1);
        var months = Enumerable.Range(0, monthCount)
            .Select(i =>
            {
                var m = firstMonth.AddMonths(i);
                var amounts = topRoots.Select(r => sums.GetValueOrDefault((r, m.Year, m.Month))).ToList();
                if (hasOther)
                    amounts.Add(otherRoots.Sum(r => sums.GetValueOrDefault((r, m.Year, m.Month))));
                return new CategoryMonthResponse(m.Year, m.Month, amounts);
            })
            .ToList();

        return ServiceResult<CategoryMonthlyResponse>.Ok(new CategoryMonthlyResponse(series, months));
    }

    // 위젯 ⑧ 요일별 평균 지출. 월~일 순서로 7개
    public async Task<ServiceResult<List<WeekdayAverageResponse>>> GetWeekdayAveragesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var error = ValidatePeriod<List<WeekdayAverageResponse>>(from, to);
        if (error is not null) return error;

        // 요일별 지출 합계. t.Date.DayOfWeek는 EF Core가 SQL(EXTRACT(DOW ...))로 바꿔 준다
        var totals = await db.Transactions
            .Where(t => t.Date >= from && t.Date <= to && t.Type == TransactionType.Expense)
            .GroupBy(t => t.Date.DayOfWeek)
            .Select(g => new { Weekday = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.Weekday, x => x.Total, cancellationToken);

        // 기간 안에 각 요일이 몇 번 있는지. 거래가 없는 날도 평균의 분모에 들어간다 (안 쓴 날도 "그 요일의 하루")
        var dayCounts = new int[7];
        for (var date = from; date <= to; date = date.AddDays(1))
            dayCounts[(int)date.DayOfWeek]++;

        // DayOfWeek는 Sunday = 0 … Saturday = 6. 한국식으로 월요일부터 보여주려고 순서를 직접 정한다
        DayOfWeek[] order = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];
        var result = order
            .Select(weekday =>
            {
                var total = totals.GetValueOrDefault(weekday);
                var count = dayCounts[(int)weekday];
                var average = count == 0 ? 0m : Math.Round(total / count, 0, MidpointRounding.AwayFromZero);
                return new WeekdayAverageResponse(weekday, count, total, average);
            })
            .ToList();

        return ServiceResult<List<WeekdayAverageResponse>>.Ok(result);
    }

    // 위젯 ⑨ 큰 지출 TOP N: 기간 안 금액 큰 순. 같은 금액이면 최근 것이 위
    public async Task<ServiceResult<List<TransactionResponse>>> GetTopExpensesAsync(DateOnly from, DateOnly to, int count, CancellationToken cancellationToken)
    {
        var error = ValidatePeriod<List<TransactionResponse>>(from, to);
        if (error is not null) return error;
        if (count < 1 || count > MaxTopExpenses)
            return ServiceResult<List<TransactionResponse>>.Invalid("count", $"count는 1~{MaxTopExpenses} 사이여야 합니다.");

        var items = await db.Transactions
            .Where(t => t.Date >= from && t.Date <= to && t.Type == TransactionType.Expense)
            .OrderByDescending(t => t.Amount)
            .ThenByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Take(count)
            .Select(TransactionService.ToResponse)
            .ToListAsync(cancellationToken);

        return ServiceResult<List<TransactionResponse>>.Ok(items);
    }

    // 기간의 날마다 수입/지출 + 지출 누적 (위젯 ④ 일별 지출 추이, ⑤ 이전 기간 누적 비교)
    public async Task<ServiceResult<List<DailyTotalResponse>>> GetDailyTotalsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var error = ValidatePeriod<List<DailyTotalResponse>>(from, to);
        if (error is not null) return error;

        // DayNumber: 0001-01-01부터 며칠째인지. 두 날짜의 차 + 1 = 기간의 날 수
        var dayCount = to.DayNumber - from.DayNumber + 1;
        if (dayCount > MaxDays)
            return ServiceResult<List<DailyTotalResponse>>.Invalid("to", $"최대 {MaxDays}일까지 조회할 수 있습니다.");

        // 날짜로 묶어서 합계 (GetMonthlyTotalsAsync와 같은 방식, 키만 날짜)
        var totals = await db.Transactions
            .Where(t => t.Date >= from && t.Date <= to)
            .GroupBy(t => t.Date)
            .Select(g => new
            {
                Date = g.Key,
                Income = g.Sum(t => t.Type == TransactionType.Income ? t.Amount : 0m),
                Expense = g.Sum(t => t.Type == TransactionType.Expense ? t.Amount : 0m),
            })
            .ToDictionaryAsync(x => x.Date, cancellationToken);

        // 거래가 없는 날도 0으로 채우고, 지출 누적을 앞에서부터 더해 간다
        var days = new List<DailyTotalResponse>(dayCount);
        var cumulative = 0m;
        for (var i = 0; i < dayCount; i++)
        {
            var date = from.AddDays(i);
            totals.TryGetValue(date, out var total);
            var expense = total?.Expense ?? 0m;
            cumulative += expense;
            days.Add(new DailyTotalResponse(date, total?.Income ?? 0m, expense, cumulative));
        }

        return ServiceResult<List<DailyTotalResponse>>.Ok(days);
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

    // 상위 N개에 못 든 대분류를 묶는 이름
    private const string OtherName = "기타";

    // 상위 몇 개까지 따로 보여줄지 (너무 많으면 색으로 구분할 수 없다. 차트 색 토큰은 5개)
    private const int MaxTop = 5;

    // 큰 지출 목록의 최대 건수
    private const int MaxTopExpenses = 20;

    private static ServiceResult<T>? ValidateTop<T>(int top) =>
        top < 1 || top > MaxTop ? ServiceResult<T>.Invalid("top", $"top은 1~{MaxTop} 사이여야 합니다.") : null;

    // 비율(%) = 금액 ÷ 전체 × 100, 소수 첫째 자리 반올림 (GetCategoryExpensesAsync와 같은 규칙)
    private static decimal Ratio(decimal amount, decimal total) =>
        total == 0 ? 0m : Math.Round(amount / total * 100, 1, MidpointRounding.AwayFromZero);

    // 기간의 지출을 "대분류 × 연월"로 묶은 합계. 하위 분류의 지출은 자기 대분류에 합친다 (결정 D23).
    // 돌려주는 값: (대분류 Id → 이름, (대분류 Id, 연, 월) → 지출). 미분류(카테고리 없음)는 Id 0.
    // 튜플 (A, B)를 반환 타입으로 쓰면 값 두 개를 클래스 없이 함께 돌려줄 수 있다. 받는 쪽은 var (a, b) = ... 로 푼다
    private async Task<(Dictionary<int, string> RootNames, Dictionary<(int Root, int Year, int Month), decimal> Sums)> RootMonthlyExpensesAsync(
        DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var categories = await db.Categories
            .Select(c => new { c.Id, c.ParentId, c.Name })
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var sums = await db.Transactions
            .Where(t => t.Date >= from && t.Date <= to && t.Type == TransactionType.Expense)
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.CategoryId })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.CategoryId, Amount = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        var rootNames = new Dictionary<int, string> { [0] = "미분류" };
        var result = new Dictionary<(int Root, int Year, int Month), decimal>();
        foreach (var sum in sums)
        {
            var root = RootOf(sum.CategoryId);
            rootNames.TryAdd(root, categories.TryGetValue(root, out var c) ? c.Name : "미분류");
            var key = (root, sum.Year, sum.Month);
            result[key] = result.GetValueOrDefault(key) + sum.Amount;
        }
        return (rootNames, result);

        // 부모를 따라 맨 위(대분류)까지 올라간다. 최대 3단계라 세 번이면 닿는다 (FindBucket과 같은 보호 장치)
        int RootOf(int? categoryId)
        {
            var currentId = categoryId;
            for (var i = 0; i < 3 && currentId is not null && categories.TryGetValue(currentId.Value, out var current); i++)
            {
                if (current.ParentId is null) return current.Id;
                currentId = current.ParentId;
            }
            return 0;
        }
    }

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
