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

    // 기간 요약: from ~ to (양 끝 날짜 포함). 일간이면 from == to, 주간이면 월~일 7일.
    public async Task<ServiceResult<PeriodSummaryResponse>> GetSummaryAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        // 오류 키는 주소의 이름(from/to) 그대로. ModelState는 대소문자를 구분하지 않아서,
        // "To"로 넣어도 이미 있는 "to" 항목(주소 값을 읽을 때 생김)에 합쳐진다.
        if (from < MinDate || from > MaxDate)
            return ServiceResult<PeriodSummaryResponse>.Invalid("from", "조회할 수 없는 날짜입니다.");
        if (to < MinDate || to > MaxDate)
            return ServiceResult<PeriodSummaryResponse>.Invalid("to", "조회할 수 없는 날짜입니다.");
        if (from > to)
            return ServiceResult<PeriodSummaryResponse>.Invalid("to", "끝 날짜는 시작 날짜보다 빠를 수 없습니다.");

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
