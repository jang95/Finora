using Finora.Api.Data.Entities;

namespace Finora.Api.Services;

// 저금 하나의 계산 결과 (결정 D24). 값이 의미 없는 종류에서는 null (예: 자유저금의 예상 이자).
// - PaidPrincipal: 지금까지 넣은 원금 = 기존 금액 + 입금 − 출금 (이자 제외)
// - ExpectedPrincipal / ExpectedInterest / ExpectedTax / ExpectedReceive: 만기까지 계획대로 넣었을 때의 예상 원금·세전 이자·세금·세후 수령액.
//   이자·세금은 원 미만 버림. 화면에는 "예상"으로 표시한다 (은행마다 계산 방식이 조금씩 달라 실제와 몇 원~몇십 원 다를 수 있다)
// - AmountProgress: 금액 진행률(%) — 자유저금은 현재 금액 ÷ 목표 금액, 정액·26주형 적금은 넣은 원금 ÷ 만기 원금
// - TimeProgress: 기간 진행률(%) — 시작일~만기일(목표일) 중 오늘까지 지난 비율
// - MonthlyRequired: 월 필요액 — 자유저금이 목표일까지 목표 금액을 채우려면 남은 달마다 넣어야 하는 돈
// - PlannedToDate / PlanDifference: 오늘까지 계획대로라면 넣었을 원금과, 실제와의 차이 (양수 = 앞섬, 음수 = 뒤처짐)
public record SavingCalculation(
    decimal PaidPrincipal,
    decimal? ExpectedPrincipal,
    decimal? ExpectedInterest,
    decimal? ExpectedTax,
    decimal? ExpectedReceive,
    decimal? AmountProgress,
    decimal? TimeProgress,
    decimal? MonthlyRequired,
    decimal? PlannedToDate,
    decimal? PlanDifference);

// 저금 계산 (예상 이자·세금·진행률·월 필요액·계획 대비). DB를 쓰지 않는 순수 계산이라 단위 테스트로 바로 확인한다 (SavingCalculatorTests).
// static class: 상태(필드)가 없고 입력 → 출력만 하는 함수 모음. new 없이 SavingCalculator.Calculate(...)로 부른다.
// (TypeScript로 치면 클래스 없이 export function만 모아 둔 파일과 같다)
//
// 금액 계산은 모두 decimal (CLAUDE.md 규칙, 결정 D4). double을 쓰면 0.1 같은 값이 2진수로 정확히 저장되지 않아 원 단위가 틀어질 수 있다.
//
// 이자 계산 방법 — 돈 한 덩어리(A)가 넣은 날(d)부터 만기일(m)까지 붙는 이자를 "꽉 찬 달 수 n + 남은 날 수 rest"로 나눠 계산한다.
//   단리:   A × 연이율 × n/12  +  A × 연이율 × rest/365
//   월복리: A × (1 + 연이율/12)^n − A  +  (달마다 불어난 금액) × 연이율 × rest/365
// 예금은 원금 한 덩어리, 적금은 납입할 때마다 한 덩어리씩 각자 남은 기간만큼 이자가 붙는다 (적금 이자가 생각보다 적은 이유).
public static class SavingCalculator
{
    public static SavingCalculation Calculate(Saving saving, IReadOnlyList<SavingEntry> entries, DateOnly today)
    {
        var paid = PaidPrincipal(entries);
        var balance = Balance(entries);

        // 만기 원금과 그 원금이 들어가는 날짜 목록 (계획). null이면 예상 이자를 계산하지 않는다
        var payments = PlannedPayments(saving, entries);
        decimal? expectedPrincipal = null, expectedInterest = null, expectedTax = null, expectedReceive = null;
        if (payments is not null && saving.MaturityDate is { } maturity && saving.InterestRate is { } rate)
        {
            expectedPrincipal = payments.Sum(p => p.Amount);
            // 이자는 덩어리마다 계산해 더한 다음 마지막에 한 번 원 미만을 버린다
            var interest = Floor(payments.Sum(p => Interest(p.Amount, p.Date, maturity, rate, saving.InterestMethod)));
            var tax = Floor(interest * saving.TaxRate / 100m);
            expectedInterest = interest;
            expectedTax = tax;
            expectedReceive = expectedPrincipal + interest - tax;
        }

        var planned = PlannedToDate(saving, today);

        return new SavingCalculation(
            PaidPrincipal: paid,
            ExpectedPrincipal: expectedPrincipal,
            ExpectedInterest: expectedInterest,
            ExpectedTax: expectedTax,
            ExpectedReceive: expectedReceive,
            AmountProgress: AmountProgress(saving, balance, paid, expectedPrincipal),
            TimeProgress: TimeProgress(saving, today),
            MonthlyRequired: MonthlyRequired(saving, balance, today),
            PlannedToDate: planned,
            // ?. 대신 삼항 연산자: planned가 null이면 차이도 null
            PlanDifference: planned is null ? null : paid - planned);
    }

    // 돈 A를 from부터 maturity까지 두었을 때의 세전 이자 (버림 전). 만기 이후에 넣은 돈은 이자 0
    public static decimal Interest(decimal amount, DateOnly from, DateOnly maturity, decimal annualRate, InterestMethod method)
    {
        if (from >= maturity) return 0m;
        var rate = annualRate / 100m;
        var months = FullMonths(from, maturity);
        // DayNumber: 0001-01-01부터 센 날 수. 두 날짜의 DayNumber를 빼면 사이의 날 수가 된다
        var restDays = maturity.DayNumber - from.AddMonths(months).DayNumber;

        if (method == InterestMethod.Simple)
            return amount * rate * months / 12m + amount * rate * restDays / 365m;

        // 월복리: 매달 (1 + 월이율)을 곱한다. Math.Pow는 double만 받아서 decimal을 유지하려고 반복문으로 거듭제곱한다
        var grown = amount;
        for (var i = 0; i < months; i++) grown *= 1m + rate / 12m;
        return grown - amount + grown * rate * restDays / 365m;
    }

    // from부터 to까지 꽉 찬 달 수. 1월 15일 → 3월 14일은 1달, 3월 15일이면 2달.
    // AddMonths는 말일을 맞춰 준다 (1월 31일 + 1달 = 2월 28일)
    public static int FullMonths(DateOnly from, DateOnly to)
    {
        if (to <= from) return 0;
        var months = (to.Year - from.Year) * 12 + to.Month - from.Month;
        if (from.AddMonths(months) > to) months--;
        return months;
    }

    // ---- 내부 계산 ----

    // from부터 end까지 "돈을 넣을 수 있는 달" 수 = 꽉 찬 달 수 + 꽉 차지 않은 마지막 달(있으면 1). 최소 1.
    // 1월 1일 → 12월 31일이면 11달 + 남은 30일 = 12번, 9월 15일 → 12월 31일이면 9/15·10/15·11/15·12/15 = 4번
    private static int MonthSlots(DateOnly from, DateOnly end)
    {
        var months = FullMonths(from, end);
        if (from.AddMonths(months) < end) months++;
        return Math.Max(1, months);
    }

    // 이 저금에 들어갈 원금 덩어리들(날짜, 금액). 종류별로 다르다:
    // - 예금: 기존 금액 또는 원금 입금 한 건 (아직 없으면 null)
    // - 정액 적금: 가입일부터 매달 같은 날 월 납입액, 만기 전까지 꽉 찬 달 수만큼
    // - 26주형 적금: 가입일부터 7일마다 첫 주 금액 × 주차 (1주 1만 원 → 2주 2만 원 …), 만기 전까지 꽉 찬 주 수만큼
    // - 자유적립 적금: 실제로 넣은 기록 (기존 금액은 가입일에 넣은 것으로 본다 — 결정 D24의 근사)
    // - 자유저금: 예상 이자 없음 (null). 파킹통장처럼 금리가 자주 바뀌어 만기 개념이 없다
    private static List<(DateOnly Date, decimal Amount)>? PlannedPayments(Saving saving, IReadOnlyList<SavingEntry> entries)
    {
        if (saving.MaturityDate is not { } maturity) return null;

        switch (saving.Kind)
        {
            case SavingKind.Deposit:
                var principal = entries
                    .Where(e => e.Type is SavingEntryType.Opening or SavingEntryType.Deposit)
                    .Select(e => (e.Type == SavingEntryType.Opening ? saving.StartDate : e.Date, e.Amount))
                    .ToList();
                return principal.Count == 0 ? null : principal;

            case SavingKind.Installment when saving.PaymentType == SavingPaymentType.Fixed && saving.PlannedAmount is { } monthly:
                // Enumerable.Range(0, n): 0, 1, …, n−1. 회차마다 (가입일 + k달, 월 납입액)
                return Enumerable.Range(0, FixedCount(saving.StartDate, maturity))
                    .Select(k => (saving.StartDate.AddMonths(k), monthly))
                    .ToList();

            case SavingKind.Installment when saving.PaymentType == SavingPaymentType.WeeklyIncreasing && saving.PlannedAmount is { } firstWeek:
                return Enumerable.Range(0, WeeklyCount(saving.StartDate, maturity))
                    .Select(k => (saving.StartDate.AddDays(7 * k), firstWeek * (k + 1)))
                    .ToList();

            case SavingKind.Installment when saving.PaymentType == SavingPaymentType.Flexible:
                return entries
                    .Where(e => e.Type is SavingEntryType.Opening or SavingEntryType.Deposit)
                    .Select(e => (e.Type == SavingEntryType.Opening ? saving.StartDate : e.Date, e.Amount))
                    .ToList();

            default:
                return null;
        }
    }

    // 정액 적금 납입 횟수 = 가입일~만기일의 꽉 찬 달 수 (12개월 상품이면 12회). 최소 1회
    private static int FixedCount(DateOnly start, DateOnly maturity) => Math.Max(1, FullMonths(start, maturity));

    // 26주형 납입 횟수 = 가입일~만기일의 꽉 찬 주 수 (182일이면 26주). 최소 1회
    private static int WeeklyCount(DateOnly start, DateOnly maturity) => Math.Max(1, (maturity.DayNumber - start.DayNumber) / 7);

    // 오늘까지 계획대로라면 넣었을 원금. 계획이 없는 종류(예금, 계획 금액 없는 자유저금·자유적립)는 null
    private static decimal? PlannedToDate(Saving saving, DateOnly today)
    {
        // 예금은 PlannedAmount가 늘 null(SavingService.Apply)이라 여기서 null이 된다
        if (saving.PlannedAmount is not { } planned) return null;
        if (today < saving.StartDate) return 0m;

        switch (saving.Kind)
        {
            case SavingKind.Installment when saving.PaymentType == SavingPaymentType.Fixed && saving.MaturityDate is { } maturity:
                // 오늘까지 돌아온 회차 수 (가입일이 1회차) — 전체 회차를 넘지 않는다
                var dueCount = Math.Min(FullMonths(saving.StartDate, today) + 1, FixedCount(saving.StartDate, maturity));
                return planned * dueCount;

            case SavingKind.Installment when saving.PaymentType == SavingPaymentType.WeeklyIncreasing && saving.MaturityDate is { } maturity:
                // 1주차 + 2주차 + … + w주차 = 첫 주 금액 × w(w+1)/2 (1부터 w까지의 합 공식)
                var weeks = Math.Min((today.DayNumber - saving.StartDate.DayNumber) / 7 + 1, WeeklyCount(saving.StartDate, maturity));
                return planned * weeks * (weeks + 1) / 2;

            case SavingKind.Free:
            case SavingKind.Installment when saving.PaymentType == SavingPaymentType.Flexible:
                // 월 계획 금액 × 시작 달부터 이번 달까지의 달 수. 만기일(목표일)이 있으면 그 기간의 달 수를 넘지 않는다
                var monthCount = FullMonths(saving.StartDate, today) + 1;
                if (saving.MaturityDate is { } end) monthCount = Math.Min(monthCount, MonthSlots(saving.StartDate, end));
                var result = planned * monthCount;
                // 자유저금은 목표 금액보다 많이 계획할 필요가 없다
                return saving.TargetAmount is { } target ? Math.Min(result, target) : result;

            default:
                return null;
        }
    }

    private static decimal? AmountProgress(Saving saving, decimal balance, decimal paid, decimal? expectedPrincipal)
    {
        if (saving.Kind == SavingKind.Free)
            return saving.TargetAmount is { } target && target > 0 ? Percent(balance, target) : null;
        // 정액·26주형만: 만기 원금이 정해져 있다. 자유적립·예금은 "넣은 만큼이 곧 계획"이라 금액 진행률이 의미 없다
        if (saving.Kind == SavingKind.Installment && saving.PaymentType != SavingPaymentType.Flexible
            && expectedPrincipal is { } total && total > 0)
            return Percent(paid, total);
        return null;
    }

    // 기간 진행률. 0~100으로 자른다 (시작 전 0, 만기 후 100)
    private static decimal? TimeProgress(Saving saving, DateOnly today)
    {
        if (saving.MaturityDate is not { } end) return null;
        var total = end.DayNumber - saving.StartDate.DayNumber;
        var passed = Math.Clamp(today.DayNumber - saving.StartDate.DayNumber, 0, total);
        return Percent(passed, total);
    }

    // 자유저금 월 필요액 = 남은 금액 ÷ 남은 달 수 (원 미만 올림 — 조금이라도 모자라지 않게).
    // 남은 달 수는 이번 달을 포함해 센다 (목표일이 이번 달 안이면 1). 목표가 없거나, 이미 채웠거나, 목표일이 지났으면 null
    private static decimal? MonthlyRequired(Saving saving, decimal balance, DateOnly today)
    {
        if (saving.Kind != SavingKind.Free || saving.TargetAmount is not { } target || saving.MaturityDate is not { } end) return null;
        if (balance >= target || today >= end) return null;

        var from = today < saving.StartDate ? saving.StartDate : today;
        return Math.Ceiling((target - balance) / MonthSlots(from, end));
    }

    // 넣은 원금 = 기존 금액 + 입금 − 출금 (이자 제외). 만기 수령·해지의 출금에는 이자도 섞여 있어 음수가 되면 0으로 본다
    private static decimal PaidPrincipal(IReadOnlyList<SavingEntry> entries) =>
        Math.Max(0m, entries.Sum(e => e.Type switch
        {
            SavingEntryType.Opening or SavingEntryType.Deposit => e.Amount,
            SavingEntryType.Withdrawal => -e.Amount,
            _ => 0m,
        }));

    // 현재 금액 = 출금만 빼고 모두 더한다 (SavingService.ToResponses의 SQL 합계와 같은 규칙)
    private static decimal Balance(IReadOnlyList<SavingEntry> entries) =>
        entries.Sum(e => e.Type == SavingEntryType.Withdrawal ? -e.Amount : e.Amount);

    // 원 미만 버림. 금액은 항상 0 이상이라 Floor(내림) = 버림
    private static decimal Floor(decimal value) => Math.Floor(value);

    // 백분율, 소수 첫째 자리까지 (MidpointRounding.AwayFromZero: 0.05 → 0.1 처럼 "반올림"). 기본값은 은행가 반올림이라 지정한다
    private static decimal Percent(decimal part, decimal total) =>
        Math.Round(part / total * 100m, 1, MidpointRounding.AwayFromZero);
}
