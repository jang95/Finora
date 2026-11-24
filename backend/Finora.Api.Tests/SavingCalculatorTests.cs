using Finora.Api.Data.Entities;
using Finora.Api.Services;

namespace Finora.Api.Tests;

// 저금 계산 (결정 D24). 서버·DB 없이 SavingCalculator만 확인한다 (KoreaTimeTests와 같은 단위 테스트).
// 기대값은 손으로 계산한 값이다. 테스트 코드 안에서 같은 공식으로 다시 계산하면, 공식이 틀려도 테스트가 통과해 버린다.
public class SavingCalculatorTests
{
    private static readonly DateOnly Jan1 = new(2026, 1, 1);

    [Fact]
    public void Deposit_Simple_TwelveMonths()
    {
        // 1천만 원 × 3.5% × 1년 = 350,000 / 세금 15.4% = 53,900 / 수령 10,296,100
        var saving = NewSaving(SavingKind.Deposit, maturity: new DateOnly(2027, 1, 1), rate: 3.5m);
        var entries = new[] { Entry(SavingEntryType.Opening, Jan1, 10_000_000m) };

        var result = SavingCalculator.Calculate(saving, entries, today: new DateOnly(2026, 7, 2));

        Assert.Equal(10_000_000m, result.ExpectedPrincipal);
        Assert.Equal(350_000m, result.ExpectedInterest);
        Assert.Equal(53_900m, result.ExpectedTax);
        Assert.Equal(10_296_100m, result.ExpectedReceive);
        Assert.Null(result.AmountProgress);
        Assert.Null(result.PlannedToDate);
        // 365일 중 182일 지남 → 49.9%
        Assert.Equal(49.9m, result.TimeProgress);
    }

    [Fact]
    public void Deposit_MonthlyCompound_TwelveMonths()
    {
        // 1천만 원 × (1 + 0.036/12)^12 = 10,365,999.80… → 이자 365,999 (원 미만 버림), 세금 56,363 (버림)
        var saving = NewSaving(SavingKind.Deposit, maturity: new DateOnly(2027, 1, 1), rate: 3.6m, method: InterestMethod.MonthlyCompound);
        var entries = new[] { Entry(SavingEntryType.Deposit, Jan1, 10_000_000m) };

        var result = SavingCalculator.Calculate(saving, entries, Jan1);

        Assert.Equal(365_999m, result.ExpectedInterest);
        Assert.Equal(56_363m, result.ExpectedTax);
        Assert.Equal(10_309_636m, result.ExpectedReceive);
    }

    [Fact]
    public void Deposit_WithoutPrincipal_HasNoExpectation()
    {
        var saving = NewSaving(SavingKind.Deposit, maturity: new DateOnly(2027, 1, 1), rate: 3.5m);

        var result = SavingCalculator.Calculate(saving, [], Jan1);

        Assert.Null(result.ExpectedInterest);
        Assert.Equal(0m, result.PaidPrincipal);
    }

    [Fact]
    public void FixedInstallment_Simple_EachPaymentEarnsRemainingMonths()
    {
        // 월 10만 원 × 12회, 연 4% 단리. 1회차는 12달, 12회차는 1달 이자 → 10만 × 0.04/12 × (12+11+…+1 = 78) = 26,000
        // 세금 4,004 / 수령 1,200,000 + 26,000 − 4,004 = 1,221,996
        var saving = NewSaving(SavingKind.Installment, maturity: new DateOnly(2027, 1, 1), rate: 4m,
            paymentType: SavingPaymentType.Fixed, planned: 100_000m);

        var result = SavingCalculator.Calculate(saving, [Entry(SavingEntryType.Deposit, Jan1, 100_000m)], Jan1);

        Assert.Equal(1_200_000m, result.ExpectedPrincipal);
        Assert.Equal(26_000m, result.ExpectedInterest);
        Assert.Equal(4_004m, result.ExpectedTax);
        Assert.Equal(1_221_996m, result.ExpectedReceive);
        // 120만 원 중 10만 원 → 8.3%
        Assert.Equal(8.3m, result.AmountProgress);
    }

    [Fact]
    public void FixedInstallment_PlanComparison_CountsDueRounds()
    {
        // 1월 10일 가입. 3월 9일은 아직 3회차(3월 10일) 전 → 2회분 20만 원이 계획. 기존 금액 10만 원뿐이면 10만 원 뒤처짐
        var saving = NewSaving(SavingKind.Installment, start: new DateOnly(2026, 1, 10), maturity: new DateOnly(2027, 1, 10), rate: 4m,
            paymentType: SavingPaymentType.Fixed, planned: 100_000m);
        var entries = new[] { Entry(SavingEntryType.Opening, new DateOnly(2026, 1, 10), 100_000m) };

        var result = SavingCalculator.Calculate(saving, entries, today: new DateOnly(2026, 3, 9));

        Assert.Equal(200_000m, result.PlannedToDate);
        Assert.Equal(-100_000m, result.PlanDifference);
    }

    [Fact]
    public void WeeklyIncreasing_TwentySixWeeks()
    {
        // 첫 주 1,000원, 182일(26주). 원금 = 1,000 × (1+2+…+26 = 351) = 351,000
        var start = new DateOnly(2026, 1, 5);
        var saving = NewSaving(SavingKind.Installment, start: start, maturity: start.AddDays(182), rate: 7m,
            paymentType: SavingPaymentType.WeeklyIncreasing, planned: 1_000m);

        // 3주차 날(15일째)까지 계획 = 1,000 + 2,000 + 3,000 = 6,000
        var result = SavingCalculator.Calculate(saving, [], today: start.AddDays(14));

        Assert.Equal(351_000m, result.ExpectedPrincipal);
        Assert.True(result.ExpectedInterest > 0);
        Assert.Equal(6_000m, result.PlannedToDate);
        Assert.Equal(-6_000m, result.PlanDifference);
    }

    [Fact]
    public void FlexibleInstallment_UsesActualDeposits()
    {
        // 기존 금액 50만 원은 가입일에 넣은 것으로 → 12달 이자 15,000. 7월 1일 20만 원 → 6달 이자 3,000. 합계 18,000
        var saving = NewSaving(SavingKind.Installment, maturity: new DateOnly(2027, 1, 1), rate: 3m,
            paymentType: SavingPaymentType.Flexible);
        var entries = new[]
        {
            Entry(SavingEntryType.Opening, Jan1, 500_000m),
            Entry(SavingEntryType.Deposit, new DateOnly(2026, 7, 1), 200_000m),
            // 실제 받은 이자는 원금이 아니다 → 예상 원금·넣은 원금에 들어가지 않는다
            Entry(SavingEntryType.Interest, new DateOnly(2026, 8, 1), 500m),
        };

        var result = SavingCalculator.Calculate(saving, entries, new DateOnly(2026, 8, 1));

        Assert.Equal(700_000m, result.ExpectedPrincipal);
        Assert.Equal(700_000m, result.PaidPrincipal);
        Assert.Equal(18_000m, result.ExpectedInterest);
        // 자유적립은 금액 진행률이 없고, 계획 금액도 없으면 계획 대비도 없다
        Assert.Null(result.AmountProgress);
        Assert.Null(result.PlannedToDate);
    }

    [Fact]
    public void Free_TargetProgressAndMonthlyRequired()
    {
        // 목표 100만 원, 목표일 12월 31일, 현재 40만 원, 오늘 9월 15일
        var saving = NewSaving(SavingKind.Free, maturity: new DateOnly(2026, 12, 31), planned: 100_000m);
        saving.TargetAmount = 1_000_000m;
        var entries = new[]
        {
            Entry(SavingEntryType.Deposit, Jan1, 500_000m),
            Entry(SavingEntryType.Withdrawal, new DateOnly(2026, 5, 1), 100_000m),
        };

        var result = SavingCalculator.Calculate(saving, entries, today: new DateOnly(2026, 9, 15));

        // 자유저금은 예상 이자 없음
        Assert.Null(result.ExpectedInterest);
        Assert.Equal(40.0m, result.AmountProgress);
        // 남은 60만 원 ÷ 남은 달 4번(9/15, 10/15, 11/15, 12/15) = 15만 원
        Assert.Equal(150_000m, result.MonthlyRequired);
        // 1월~9월 9달 × 10만 원 = 90만 원 계획, 실제 40만 원 → 50만 원 뒤처짐
        Assert.Equal(900_000m, result.PlannedToDate);
        Assert.Equal(-500_000m, result.PlanDifference);
        // 364일 중 257일 → 70.6%
        Assert.Equal(70.6m, result.TimeProgress);
    }

    [Fact]
    public void Free_TargetReached_NoMonthlyRequired()
    {
        var saving = NewSaving(SavingKind.Free, maturity: new DateOnly(2026, 12, 31));
        saving.TargetAmount = 100_000m;

        var result = SavingCalculator.Calculate(saving, [Entry(SavingEntryType.Deposit, Jan1, 120_000m)], new DateOnly(2026, 3, 1));

        Assert.Null(result.MonthlyRequired);
        Assert.Equal(120.0m, result.AmountProgress);
    }

    [Theory]
    [InlineData("2026-01-15", "2026-03-14", 1)]
    [InlineData("2026-01-15", "2026-03-15", 2)]
    // 1월 31일 + 1달 = 2월 28일 (말일 맞춤)
    [InlineData("2026-01-31", "2026-02-28", 1)]
    [InlineData("2026-03-01", "2026-02-01", 0)]
    public void FullMonths_CountsCompleteMonths(string from, string to, int expected)
    {
        Assert.Equal(expected, SavingCalculator.FullMonths(DateOnly.Parse(from), DateOnly.Parse(to)));
    }

    [Fact]
    public void Interest_SplitsMonthsAndRemainingDays()
    {
        // 100만 원, 연 3.65% 단리, 1월 1일 → 2월 11일 = 1달 + 10일
        // 1달: 100만 × 0.0365 / 12 = 3,041.666… / 10일: 100만 × 0.0365 × 10/365 = 1,000
        var interest = SavingCalculator.Interest(1_000_000m, Jan1, new DateOnly(2026, 2, 11), 3.65m, InterestMethod.Simple);

        Assert.Equal(4_041.6667m, Math.Round(interest, 4));
    }

    private static Saving NewSaving(
        SavingKind kind, DateOnly? maturity = null, decimal? rate = null, DateOnly? start = null,
        InterestMethod method = InterestMethod.Simple, SavingPaymentType? paymentType = null, decimal? planned = null) =>
        new()
        {
            UserId = "test", Name = "테스트 저금", Kind = kind, StartDate = start ?? Jan1, MaturityDate = maturity,
            InterestRate = rate, InterestMethod = method, TaxRate = 15.4m, PaymentType = paymentType, PlannedAmount = planned,
        };

    private static SavingEntry Entry(SavingEntryType type, DateOnly date, decimal amount) =>
        new() { UserId = "test", Type = type, Date = date, Amount = amount };
}
