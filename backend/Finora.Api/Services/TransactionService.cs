using Finora.Api.Auth;
using Finora.Api.Data;
using Finora.Api.Data.Entities;
using Finora.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Finora.Api.Services;

// 가계부 거래 등록·수정·삭제와 월별·연간 조회.
// db.Transactions / db.Categories는 Global Query Filter로 항상 "현재 사용자 것"만 보인다.
// → 다른 사람의 거래 Id는 404, 다른 사람의 카테고리 Id는 "찾을 수 없음"이 된다. (계획서 9장)
public class TransactionService(FinoraDbContext db, CurrentUser currentUser)
{
    // 금액 상한 1조 원. numeric(18,2)보다 훨씬 작게 잡아 입력 실수(0을 몇 개 더 누름)도 막는다.
    private const decimal MaxAmount = 1_000_000_000_000m;

    public async Task<ServiceResult<TransactionResponse>> CreateAsync(SaveTransactionRequest request, CancellationToken cancellationToken)
    {
        var error = await ValidateAsync(request, cancellationToken);
        if (error is not null) return error;

        var now = DateTime.UtcNow;
        var transaction = new Transaction
        {
            // 소유자는 요청 값이 아니라 인증 정보에서 정한다. (CLAUDE.md 규칙)
            UserId = currentUser.Id ?? throw new InvalidOperationException("로그인한 사용자가 없습니다."),
            CreatedAt = now,
        };
        Apply(transaction, request, now);
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResult<TransactionResponse>.Ok(await GetResponseAsync(transaction.Id, cancellationToken));
    }

    public async Task<ServiceResult<TransactionResponse>> UpdateAsync(int id, SaveTransactionRequest request, CancellationToken cancellationToken)
    {
        // 먼저 "내 거래인지" 확인한다. 남의 거래면 입력값이 맞든 틀리든 404 (존재 여부를 알려주지 않음)
        var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (transaction is null) return ServiceResult<TransactionResponse>.NotFound();

        var error = await ValidateAsync(request, cancellationToken);
        if (error is not null) return error;

        Apply(transaction, request, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResult<TransactionResponse>.Ok(await GetResponseAsync(id, cancellationToken));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (transaction is null) return ServiceResult<bool>.NotFound();

        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    // 한 달치: 월 요약 + 날짜별 묶음
    public async Task<MonthlyLedgerResponse> GetMonthlyAsync(int year, int month, CancellationToken cancellationToken)
    {
        // 그달 1일 ~ 다음 달 1일 "전"까지. (말일을 따로 계산하지 않아도 되고, 2월·윤년도 자동으로 맞는다)
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1);

        var transactions = await db.Transactions
            .Where(t => t.Date >= from && t.Date < to)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id) // 같은 날은 나중에 등록한 것이 위
            .Select(ToResponse)
            .ToListAsync(cancellationToken);

        // 날짜별로 묶는다. 이미 최신 날짜순이라 GroupBy 결과도 최신 날짜순이다.
        // (한 달치 거래는 많아야 수백 건이라 DB에서 가져온 뒤 C#에서 묶는다)
        var days = transactions
            .GroupBy(t => t.Date)
            .Select(g => new DailyTransactionsResponse(g.Key, SumOf(g, TransactionType.Income), SumOf(g, TransactionType.Expense), g.ToList()))
            .ToList();

        var income = days.Sum(d => d.Income);
        var expense = days.Sum(d => d.Expense);
        return new MonthlyLedgerResponse(income, expense, income - expense, days);
    }

    // 한 해: 1~12월 각각의 합계. 합계는 DB(PostgreSQL)에서 계산해서 12줄만 받아 온다.
    public async Task<YearlyLedgerResponse> GetYearlyAsync(int year, CancellationToken cancellationToken)
    {
        var from = new DateOnly(year, 1, 1);
        var to = from.AddYears(1);

        // GroupBy + Sum → SQL의 GROUP BY + SUM(CASE WHEN ...)으로 바뀐다.
        var totals = await db.Transactions
            .Where(t => t.Date >= from && t.Date < to)
            .GroupBy(t => t.Date.Month)
            .Select(g => new
            {
                Month = g.Key,
                Income = g.Sum(t => t.Type == TransactionType.Income ? t.Amount : 0m),
                Expense = g.Sum(t => t.Type == TransactionType.Expense ? t.Amount : 0m),
            })
            .ToDictionaryAsync(x => x.Month, cancellationToken);

        // 거래가 없는 달도 0으로 채워서 항상 12개를 돌려준다.
        var months = Enumerable.Range(1, 12)
            .Select(m =>
            {
                // TryGetValue: 키가 있으면 out 변수에 값을 넣는다. 없으면 null로 남는다.
                totals.TryGetValue(m, out var total);
                var income = total?.Income ?? 0m;
                var expense = total?.Expense ?? 0m;
                return new MonthlyBalanceResponse(m, income, expense, income - expense);
            })
            .ToList();

        var totalIncome = months.Sum(m => m.Income);
        var totalExpense = months.Sum(m => m.Expense);
        return new YearlyLedgerResponse(totalIncome, totalExpense, totalIncome - totalExpense, months);
    }

    // ---- 내부 도우미 ----

    // Entity → 응답 DTO. 식(Expression)으로 두면 EF Core가 SQL로 바꿔서 필요한 컬럼만 조회한다.
    // t.Category!.Name: 카테고리 이름을 JOIN으로 가져온다. (카테고리가 없으면 null)
    // internal: 같은 프로젝트(Finora.Api) 안에서만 쓸 수 있다. 대시보드의 최근 거래(StatisticsService)도 같은 변환을 쓴다.
    internal static readonly System.Linq.Expressions.Expression<Func<Transaction, TransactionResponse>> ToResponse =
        t => new TransactionResponse(t.Id, t.Date, t.Type, t.CategoryId, t.Category!.Name, t.Amount, t.Memo);

    private async Task<TransactionResponse> GetResponseAsync(int id, CancellationToken cancellationToken) =>
        await db.Transactions.Where(t => t.Id == id).Select(ToResponse).SingleAsync(cancellationToken);

    private static decimal SumOf(IEnumerable<TransactionResponse> items, TransactionType type) =>
        items.Where(t => t.Type == type).Sum(t => t.Amount);

    // 요청 값을 Entity에 옮긴다. (등록·수정 공통) 값 검사는 ValidateAsync에서 이미 끝났다.
    private static void Apply(Transaction transaction, SaveTransactionRequest request, DateTime now)
    {
        transaction.Date = request.Date!.Value;
        transaction.Type = request.Type!.Value;
        transaction.CategoryId = request.CategoryId;
        transaction.Amount = request.Amount!.Value;
        // 공백만 있는 메모는 저장하지 않는다(null)
        transaction.Memo = string.IsNullOrWhiteSpace(request.Memo) ? null : request.Memo.Trim();
        transaction.UpdatedAt = now;
    }

    // [Required] 같은 표시로 검사할 수 없는 규칙을 확인한다. 문제가 없으면 null.
    private async Task<ServiceResult<TransactionResponse>?> ValidateAsync(SaveTransactionRequest request, CancellationToken cancellationToken)
    {
        if (request.Type == TransactionType.Transfer)
            return Invalid(nameof(request.Type), "이체는 계좌 기능이 추가된 뒤에 사용할 수 있습니다.");

        var amount = request.Amount!.Value;
        if (amount <= 0)
            return Invalid(nameof(request.Amount), "금액은 0보다 커야 합니다.");
        if (amount > MaxAmount)
            return Invalid(nameof(request.Amount), "금액이 너무 큽니다.");
        // 소수 셋째 자리 이하가 있으면 거절한다 (DB는 소수 2자리까지 저장)
        if (amount != decimal.Round(amount, 2))
            return Invalid(nameof(request.Amount), "금액은 소수 둘째 자리까지 입력할 수 있습니다.");

        if (request.CategoryId is null)
            return Invalid(nameof(request.CategoryId), "카테고리를 선택하세요.");
        // 요청의 카테고리 Id가 "내 카테고리"인지 확인한다. Global Query Filter 때문에 남의 카테고리는 찾지 못한다.
        // 이 확인이 없으면 남의 카테고리 Id를 넣어 내 거래에 붙일 수 있다.
        var categoryType = await db.Categories
            .Where(c => c.Id == request.CategoryId)
            .Select(c => (CategoryType?)c.Type)
            .FirstOrDefaultAsync(cancellationToken);
        if (categoryType is null)
            return Invalid(nameof(request.CategoryId), "카테고리를 찾을 수 없습니다.");
        // 지출 거래에는 지출 카테고리, 수입 거래에는 수입 카테고리
        var expected = request.Type == TransactionType.Income ? CategoryType.Income : CategoryType.Expense;
        if (categoryType != expected)
            return Invalid(nameof(request.CategoryId), "수입/지출 종류에 맞는 카테고리를 선택하세요.");

        return null;
    }

    private static ServiceResult<TransactionResponse> Invalid(string field, string message) =>
        ServiceResult<TransactionResponse>.Invalid(field, message);
}
