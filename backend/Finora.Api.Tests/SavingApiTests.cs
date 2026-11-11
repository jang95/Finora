using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Finora.Api.Tests;

// 저금 API (결정 D24): 등록·수정·상태, 기존 금액(Opening), 종류별 기록 규칙, 만기 수령·중도해지, 이자 → 가계부 수입 연결.
// 다른 사용자 데이터 접근은 UserIsolationTests에서 확인한다.
public class SavingApiTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private readonly WebApplicationFactory<Program> _server =
        factory.WithWebHostBuilder(b => b.UseSetting("Auth:LoginPermitPerMinute", "1000"));

    private record CategoryDto(int Id, string Name, string Type);

    private record SavingDto(
        int Id, string Name, string Kind, decimal? TargetAmount, string StartDate, string? MaturityDate, decimal? InterestRate,
        string InterestMethod, decimal TaxRate, string? PaymentType, decimal? PlannedAmount, string Status, decimal Balance);

    private record EntryDto(int Id, string Date, string Type, decimal Amount, string? Memo, int? TransactionId, int? CategoryId, string? CategoryName);

    private record DetailDto(SavingDto Saving, List<EntryDto> Entries);

    // 종류별 최소 입력 (가짜 데이터)
    private static object Free(decimal? openingAmount = null) =>
        new { name = "여행 자금", kind = "Free", startDate = "2026-01-01", targetAmount = 3000000, openingAmount };

    private static object Installment(decimal? openingAmount = null) => new
    {
        name = "테스트 적금", kind = "Installment", startDate = "2026-01-10", maturityDate = "2027-01-10",
        interestRate = 4.5, paymentType = "Fixed", plannedAmount = 300000, openingAmount,
    };

    private static object Deposit(decimal? openingAmount = null) => new
    {
        name = "테스트 예금", kind = "Deposit", startDate = "2026-02-01", maturityDate = "2027-02-01",
        interestRate = 3.2, plannedAmount = 999, paymentType = "Fixed", openingAmount,
    };

    // ---- 저금 등록·수정·상태 ----

    [Fact]
    public async Task Create_AppliesDefaultsAndDropsFieldsOfOtherKinds()
    {
        var client = await _server.RegisterAndLoginAsync();

        var saving = await CreateSavingAsync(client, Deposit());

        Assert.Equal("Active", saving.Status);
        Assert.Equal("Simple", saving.InterestMethod);
        Assert.Equal(15.4m, saving.TaxRate);
        // 예금에는 납입 방식·계획 금액이 없다 → 보내도 저장하지 않는다
        Assert.Null(saving.PaymentType);
        Assert.Null(saving.PlannedAmount);
        Assert.Equal(0m, saving.Balance);
    }

    [Fact]
    public async Task Create_WithOpeningAmount_AddsOneOpeningEntryOnStartDate()
    {
        var client = await _server.RegisterAndLoginAsync();

        var saving = await CreateSavingAsync(client, Installment(openingAmount: 1500000));
        var detail = await DetailAsync(client, saving.Id);

        Assert.Equal(1500000m, saving.Balance);
        var opening = Assert.Single(detail.Entries);
        Assert.Equal("Opening", opening.Type);
        Assert.Equal("2026-01-10", opening.Date);
    }

    [Fact]
    public async Task Update_SyncsOpeningEntry()
    {
        var client = await _server.RegisterAndLoginAsync();
        var saving = await CreateSavingAsync(client, Free(openingAmount: 100000));

        var changed = await client.PutAsJsonAsync($"/api/savings/{saving.Id}", Free(openingAmount: 250000));
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(250000m, (await DetailAsync(client, saving.Id)).Saving.Balance);

        // 기존 금액을 비우면 Opening 기록이 지워진다
        var removed = await client.PutAsJsonAsync($"/api/savings/{saving.Id}", Free());
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        Assert.Empty((await DetailAsync(client, saving.Id)).Entries);
    }

    [Fact]
    public async Task Update_CannotChangeKind()
    {
        var client = await _server.RegisterAndLoginAsync();
        var saving = await CreateSavingAsync(client, Free());

        var response = await client.PutAsJsonAsync($"/api/savings/{saving.Id}", Installment());

        await AssertFieldErrorAsync(response, "Kind");
    }

    [Fact]
    public async Task Create_ValidatesKindSpecificFields()
    {
        var client = await _server.RegisterAndLoginAsync();

        var noMaturity = await client.PostAsJsonAsync("/api/savings",
            new { name = "예금", kind = "Deposit", startDate = "2026-01-01", interestRate = 3 });
        var maturityBeforeStart = await client.PostAsJsonAsync("/api/savings",
            new { name = "예금", kind = "Deposit", startDate = "2026-01-01", maturityDate = "2025-12-31", interestRate = 3 });
        var noRate = await client.PostAsJsonAsync("/api/savings",
            new { name = "예금", kind = "Deposit", startDate = "2026-01-01", maturityDate = "2027-01-01" });
        var noPaymentType = await client.PostAsJsonAsync("/api/savings",
            new { name = "적금", kind = "Installment", startDate = "2026-01-01", maturityDate = "2027-01-01", interestRate = 3 });
        var fixedWithoutAmount = await client.PostAsJsonAsync("/api/savings",
            new { name = "적금", kind = "Installment", startDate = "2026-01-01", maturityDate = "2027-01-01", interestRate = 3, paymentType = "Fixed" });
        var badOpening = await client.PostAsJsonAsync("/api/savings", Free(openingAmount: -1));
        var missing = await client.PostAsJsonAsync("/api/savings", new { name = "이름만" });

        await AssertFieldErrorAsync(noMaturity, "MaturityDate");
        await AssertFieldErrorAsync(maturityBeforeStart, "MaturityDate");
        await AssertFieldErrorAsync(noRate, "InterestRate");
        await AssertFieldErrorAsync(noPaymentType, "PaymentType");
        await AssertFieldErrorAsync(fixedWithoutAmount, "PlannedAmount");
        await AssertFieldErrorAsync(badOpening, "OpeningAmount");
        await AssertFieldErrorAsync(missing, "Kind");
    }

    [Fact]
    public async Task UpdateStatus_InstallmentWithMoneyCannotBeCompletedDirectly()
    {
        var client = await _server.RegisterAndLoginAsync();
        var installment = await CreateSavingAsync(client, Installment(openingAmount: 300000));
        var free = await CreateSavingAsync(client, Free(openingAmount: 300000));

        var installmentDone = await client.PutAsJsonAsync($"/api/savings/{installment.Id}/status", new { status = "Completed" });
        var installmentArchived = await client.PutAsJsonAsync($"/api/savings/{installment.Id}/status", new { status = "Archived" });
        var freeDone = await client.PutAsJsonAsync($"/api/savings/{free.Id}/status", new { status = "Completed" });

        Assert.Equal(HttpStatusCode.Conflict, installmentDone.StatusCode);
        Assert.Equal("Archived", (await installmentArchived.Content.ReadFromJsonAsync<SavingDto>())!.Status);
        Assert.Equal("Completed", (await freeDone.Content.ReadFromJsonAsync<SavingDto>())!.Status);
    }

    [Fact]
    public async Task GetAll_ListsActiveFirstWithBalance()
    {
        var client = await _server.RegisterAndLoginAsync();
        var archived = await CreateSavingAsync(client, Free(openingAmount: 1000));
        await client.PutAsJsonAsync($"/api/savings/{archived.Id}/status", new { status = "Archived" });
        var active = await CreateSavingAsync(client, Free(openingAmount: 2000));

        var list = (await client.GetFromJsonAsync<List<SavingDto>>("/api/savings"))!;

        Assert.Equal([active.Id, archived.Id], list.Select(s => s.Id));
        Assert.Equal(2000m, list[0].Balance);
    }

    // ---- 기록: 종류별 규칙 ----

    [Fact]
    public async Task FreeSaving_DepositAndWithdrawUpToBalance()
    {
        var client = await _server.RegisterAndLoginAsync();
        var saving = await CreateSavingAsync(client, Free());

        await AddEntryAsync(client, saving.Id, new { date = "2026-03-01", type = "Deposit", amount = 50000 }, HttpStatusCode.Created);
        await AddEntryAsync(client, saving.Id, new { date = "2026-03-05", type = "Withdrawal", amount = 20000 }, HttpStatusCode.Created);
        var tooMuch = await client.PostAsJsonAsync($"/api/savings/{saving.Id}/entries",
            new { date = "2026-03-06", type = "Withdrawal", amount = 30001 });

        await AssertFieldErrorAsync(tooMuch, "Amount");
        Assert.Equal(30000m, (await DetailAsync(client, saving.Id)).Saving.Balance);
    }

    [Fact]
    public async Task Entries_RejectOpeningTypeAndDateBeforeStart()
    {
        var client = await _server.RegisterAndLoginAsync();
        var saving = await CreateSavingAsync(client, Free());

        var opening = await client.PostAsJsonAsync($"/api/savings/{saving.Id}/entries", new { date = "2026-03-01", type = "Opening", amount = 1000 });
        var early = await client.PostAsJsonAsync($"/api/savings/{saving.Id}/entries", new { date = "2025-12-31", type = "Deposit", amount = 1000 });
        var badAmount = await client.PostAsJsonAsync($"/api/savings/{saving.Id}/entries", new { date = "2026-03-01", type = "Deposit", amount = 10.555 });

        await AssertFieldErrorAsync(opening, "Type");
        await AssertFieldErrorAsync(early, "Date");
        await AssertFieldErrorAsync(badAmount, "Amount");
    }

    [Fact]
    public async Task Installment_CannotWithdraw()
    {
        var client = await _server.RegisterAndLoginAsync();
        var saving = await CreateSavingAsync(client, Installment(openingAmount: 600000));

        await AddEntryAsync(client, saving.Id, new { date = "2026-03-10", type = "Deposit", amount = 300000 }, HttpStatusCode.Created);
        var withdraw = await client.PostAsJsonAsync($"/api/savings/{saving.Id}/entries", new { date = "2026-03-11", type = "Withdrawal", amount = 1000 });

        await AssertFieldErrorAsync(withdraw, "Type");
    }

    [Fact]
    public async Task Deposit_AllowsOnlyOnePrincipal()
    {
        var client = await _server.RegisterAndLoginAsync();
        var fresh = await CreateSavingAsync(client, Deposit());
        var joined = await CreateSavingAsync(client, Deposit(openingAmount: 5000000));

        await AddEntryAsync(client, fresh.Id, new { date = "2026-02-01", type = "Deposit", amount = 5000000 }, HttpStatusCode.Created);
        await AddEntryAsync(client, fresh.Id, new { date = "2026-02-02", type = "Deposit", amount = 1000 }, HttpStatusCode.Conflict);
        // 기존 금액(Opening)이 원금이면 입금을 더 할 수 없다
        await AddEntryAsync(client, joined.Id, new { date = "2026-02-02", type = "Deposit", amount = 1000 }, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Entries_CannotBeAddedToArchivedSaving()
    {
        var client = await _server.RegisterAndLoginAsync();
        var saving = await CreateSavingAsync(client, Free());
        await client.PutAsJsonAsync($"/api/savings/{saving.Id}/status", new { status = "Archived" });

        await AddEntryAsync(client, saving.Id, new { date = "2026-03-01", type = "Deposit", amount = 1000 }, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task OpeningEntry_CannotBeEditedOrDeletedThroughEntries()
    {
        var client = await _server.RegisterAndLoginAsync();
        var saving = await CreateSavingAsync(client, Free(openingAmount: 1000));
        var opening = (await DetailAsync(client, saving.Id)).Entries.Single();

        var update = await client.PutAsJsonAsync($"/api/savings/{saving.Id}/entries/{opening.Id}", new { date = "2026-01-01", type = "Opening", amount = 5 });
        var delete = await client.DeleteAsync($"/api/savings/{saving.Id}/entries/{opening.Id}");

        Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task Deposits_DoNotAffectLedger()
    {
        var client = await _server.RegisterAndLoginAsync();
        var saving = await CreateSavingAsync(client, Free(openingAmount: 100000));

        await AddEntryAsync(client, saving.Id, new { date = "2026-03-01", type = "Deposit", amount = 50000 }, HttpStatusCode.Created);
        await AddEntryAsync(client, saving.Id, new { date = "2026-03-02", type = "Withdrawal", amount = 10000 }, HttpStatusCode.Created);

        // 입금·출금은 이미 가진 돈을 옮기는 것이라 가계부 수입·지출이 아니다 (결정 D24)
        var monthly = await client.GetFromJsonAsync<JsonElement>("/api/transactions/monthly?year=2026&month=3");
        Assert.Equal(0, monthly.GetProperty("days").GetArrayLength());
    }

    // ---- 이자 → 가계부 수입 ----

    [Fact]
    public async Task Interest_ByDefaultCreatesLinkedIncomeInInterestCategory()
    {
        var client = await _server.RegisterAndLoginAsync();
        var saving = await CreateSavingAsync(client, Free(openingAmount: 100000));

        var entry = await AddEntryAsync(client, saving.Id, new { date = "2026-03-31", type = "Interest", amount = 1234 }, HttpStatusCode.Created);

        Assert.NotNull(entry!.TransactionId);
        Assert.Equal("이자", entry.CategoryName);
        var monthly = await client.GetFromJsonAsync<JsonElement>("/api/transactions/monthly?year=2026&month=3");
        Assert.Equal(1234m, monthly.GetProperty("income").GetDecimal());
        var transaction = monthly.GetProperty("days")[0].GetProperty("transactions")[0];
        Assert.Equal(entry.TransactionId, transaction.GetProperty("id").GetInt32());
        Assert.Equal("여행 자금 이자", transaction.GetProperty("memo").GetString());
        // 이자는 저금 현재 금액에도 더해진다
        Assert.Equal(101234m, (await DetailAsync(client, saving.Id)).Saving.Balance);
    }

    [Fact]
    public async Task Interest_WithoutIncome_CreatesNoTransaction()
    {
        var client = await _server.RegisterAndLoginAsync();
        var saving = await CreateSavingAsync(client, Free());

        var entry = await AddEntryAsync(client, saving.Id,
            new { date = "2026-03-31", type = "Interest", amount = 500, recordAsIncome = false }, HttpStatusCode.Created);

        Assert.Null(entry!.TransactionId);
        var monthly = await client.GetFromJsonAsync<JsonElement>("/api/transactions/monthly?year=2026&month=3");
        Assert.Equal(0m, monthly.GetProperty("income").GetDecimal());
    }

    [Fact]
    public async Task Interest_RejectsExpenseCategory()
    {
        var client = await _server.RegisterAndLoginAsync();
        var food = (await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!.Single(c => c.Name == "식비").Id;
        var saving = await CreateSavingAsync(client, Free());

        var response = await client.PostAsJsonAsync($"/api/savings/{saving.Id}/entries",
            new { date = "2026-03-31", type = "Interest", amount = 500, categoryId = food });

        await AssertFieldErrorAsync(response, "CategoryId");
    }

    [Fact]
    public async Task Interest_UpdateAndDelete_ChangeLinkedTransactionTogether()
    {
        var client = await _server.RegisterAndLoginAsync();
        var salary = (await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!.Single(c => c.Name == "급여").Id;
        var saving = await CreateSavingAsync(client, Free());
        var entry = await AddEntryAsync(client, saving.Id, new { date = "2026-03-31", type = "Interest", amount = 1000 }, HttpStatusCode.Created);

        // 수정: 날짜·금액·카테고리가 거래에도 반영된다
        var updated = await client.PutAsJsonAsync($"/api/savings/{saving.Id}/entries/{entry!.Id}",
            new { date = "2026-04-01", type = "Interest", amount = 2000, categoryId = salary, memo = "4월 이자" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(0m, (await client.GetFromJsonAsync<JsonElement>("/api/transactions/monthly?year=2026&month=3")).GetProperty("income").GetDecimal());
        var april = await client.GetFromJsonAsync<JsonElement>("/api/transactions/monthly?year=2026&month=4");
        Assert.Equal(2000m, april.GetProperty("income").GetDecimal());
        var transaction = april.GetProperty("days")[0].GetProperty("transactions")[0];
        Assert.Equal("급여", transaction.GetProperty("categoryName").GetString());
        Assert.Equal("4월 이자", transaction.GetProperty("memo").GetString());

        // 가계부 기록을 끄면 거래가 지워지고, 다시 켜면 새로 만들어진다
        var off = await client.PutAsJsonAsync($"/api/savings/{saving.Id}/entries/{entry.Id}",
            new { date = "2026-04-01", type = "Interest", amount = 2000, recordAsIncome = false });
        Assert.Null((await off.Content.ReadFromJsonAsync<EntryDto>())!.TransactionId);
        Assert.Equal(0m, (await client.GetFromJsonAsync<JsonElement>("/api/transactions/monthly?year=2026&month=4")).GetProperty("income").GetDecimal());
        var on = await client.PutAsJsonAsync($"/api/savings/{saving.Id}/entries/{entry.Id}",
            new { date = "2026-04-01", type = "Interest", amount = 2000 });
        Assert.Equal("이자", (await on.Content.ReadFromJsonAsync<EntryDto>())!.CategoryName);

        // 삭제: 연결된 거래도 함께 지워진다
        var delete = await client.DeleteAsync($"/api/savings/{saving.Id}/entries/{entry.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(0m, (await client.GetFromJsonAsync<JsonElement>("/api/transactions/monthly?year=2026&month=4")).GetProperty("income").GetDecimal());
    }

    [Fact]
    public async Task DeleteSaving_AlsoDeletesLinkedInterestTransactions()
    {
        var client = await _server.RegisterAndLoginAsync();
        var saving = await CreateSavingAsync(client, Free(openingAmount: 1000));
        await AddEntryAsync(client, saving.Id, new { date = "2026-03-31", type = "Interest", amount = 100 }, HttpStatusCode.Created);

        var delete = await client.DeleteAsync($"/api/savings/{saving.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/savings/{saving.Id}")).StatusCode);
        Assert.Equal(0m, (await client.GetFromJsonAsync<JsonElement>("/api/transactions/monthly?year=2026&month=3")).GetProperty("income").GetDecimal());
    }

    // ---- 만기 수령·중도해지 ----

    [Fact]
    public async Task Close_Maturity_AddsInterestAndWithdrawsEverything()
    {
        var client = await _server.RegisterAndLoginAsync();
        var saving = await CreateSavingAsync(client, Installment(openingAmount: 3000000));
        await AddEntryAsync(client, saving.Id, new { date = "2026-11-10", type = "Deposit", amount = 300000 }, HttpStatusCode.Created);

        var response = await client.PostAsJsonAsync($"/api/savings/{saving.Id}/close",
            new { type = "Maturity", date = "2027-01-10", interest = 50000 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = (await response.Content.ReadFromJsonAsync<DetailDto>())!;
        Assert.Equal("Completed", detail.Saving.Status);
        Assert.Equal(0m, detail.Saving.Balance);
        var withdrawal = detail.Entries.Single(e => e.Type == "Withdrawal");
        Assert.Equal(3350000m, withdrawal.Amount); // 원금 330만 + 이자 5만
        Assert.Equal("만기 수령", withdrawal.Memo);
        var interest = detail.Entries.Single(e => e.Type == "Interest");
        Assert.NotNull(interest.TransactionId);
        var january = await client.GetFromJsonAsync<JsonElement>("/api/transactions/monthly?year=2027&month=1");
        Assert.Equal(50000m, january.GetProperty("income").GetDecimal());

        // 끝난 저금은 다시 해지하거나 기록을 더할 수 없다
        var again = await client.PostAsJsonAsync($"/api/savings/{saving.Id}/close", new { type = "Maturity", date = "2027-01-10" });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Close_CancelWithoutInterest_OnlyWithdraws()
    {
        var client = await _server.RegisterAndLoginAsync();
        var saving = await CreateSavingAsync(client, Deposit(openingAmount: 5000000));

        var response = await client.PostAsJsonAsync($"/api/savings/{saving.Id}/close", new { type = "Cancel", date = "2026-06-01" });

        var detail = (await response.Content.ReadFromJsonAsync<DetailDto>())!;
        Assert.DoesNotContain(detail.Entries, e => e.Type == "Interest");
        Assert.Equal("중도해지", detail.Entries.Single(e => e.Type == "Withdrawal").Memo);
        Assert.Equal(0m, detail.Saving.Balance);
    }

    [Fact]
    public async Task Close_FreeSaving_Returns409()
    {
        var client = await _server.RegisterAndLoginAsync();
        var saving = await CreateSavingAsync(client, Free(openingAmount: 1000));

        var response = await client.PostAsJsonAsync($"/api/savings/{saving.Id}/close", new { type = "Maturity", date = "2026-06-01" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ---- 테스트 도우미 ----

    private static async Task<SavingDto> CreateSavingAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/savings", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SavingDto>())!;
    }

    private static async Task<DetailDto> DetailAsync(HttpClient client, int id) =>
        (await client.GetFromJsonAsync<DetailDto>($"/api/savings/{id}"))!;

    // 기록을 추가하고 상태 코드를 확인한다. 성공(201)이면 저장된 기록을 돌려준다
    private static async Task<EntryDto?> AddEntryAsync(HttpClient client, int savingId, object body, HttpStatusCode expected)
    {
        var response = await client.PostAsJsonAsync($"/api/savings/{savingId}/entries", body);
        Assert.Equal(expected, response.StatusCode);
        return expected == HttpStatusCode.Created ? await response.Content.ReadFromJsonAsync<EntryDto>() : null;
    }

    private static async Task AssertFieldErrorAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out _), $"errors에 {field}가 없습니다: {body}");
    }
}
