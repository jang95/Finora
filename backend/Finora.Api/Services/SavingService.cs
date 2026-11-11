using Finora.Api.Auth;
using Finora.Api.Data;
using Finora.Api.Data.Entities;
using Finora.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Finora.Api.Services;

// 저금(자유저금 / 적금 / 예금) 등록·수정·삭제·상태 변경, 기록 입력, 만기 수령·중도해지 (결정 D24).
// db.Savings / db.SavingEntries / db.Categories는 Global Query Filter로 항상 "현재 사용자 것"만 보인다.
// → 다른 사람의 저금 Id는 404, 다른 사람의 카테고리 Id는 "찾을 수 없음"이 된다. (TransactionService와 같은 방식)
//
// 종류별 기록 규칙
// - 자유저금: 입금·출금·이자를 자유롭게 (출금은 현재 금액까지)
// - 적금: 입금·이자만. 출금 대신 만기 수령·중도해지(CloseAsync)
// - 예금: 원금 1번(기존 금액 또는 입금 한 건) + 이자. 출금 대신 만기 수령·중도해지
// 입금·출금은 가계부 거래가 아니다. 이자만 가계부 수입으로 함께 기록하고 연결한다(SavingEntry.TransactionId).
public class SavingService(FinoraDbContext db, CurrentUser currentUser)
{
    // 이자를 가계부 수입으로 기록할 때 기본으로 쓰는 카테고리 (DefaultCategories, Migration AddSavings가 만든 수입 대분류)
    private const string InterestCategoryName = "이자";
    private const decimal DefaultTaxRate = 15.4m;
    private const string OpeningMessage = "기존 금액은 저금 등록·수정에서 입력하세요.";
    private const string DepositOnceMessage = "예금은 원금을 한 번만 넣을 수 있습니다.";

    // ---- 저금 ----

    // 목록: 진행 중 → 완료 → 보관 순, 같은 상태 안에서는 최근 등록이 위
    public async Task<List<SavingResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var savings = await ToResponses(db.Savings).ToListAsync(cancellationToken);
        // 상태 순서(enum 선언 순서 Active, Completed, Archived)로 정렬. DB에는 문자열로 저장돼 있어서
        // DB에서 정렬하면 알파벳순(Active, Archived, Completed)이 된다. 저금은 많아야 수십 개라 C#에서 정렬한다.
        return savings.OrderBy(s => s.Status).ThenByDescending(s => s.Id).ToList();
    }

    // 상세: 저금 + 기록 목록
    public async Task<ServiceResult<SavingDetailResponse>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var saving = await ToResponses(db.Savings.Where(s => s.Id == id)).SingleOrDefaultAsync(cancellationToken);
        if (saving is null) return ServiceResult<SavingDetailResponse>.NotFound();

        // 정렬은 DTO로 바꾸기(Select) 전에 한다. 생성자로 만든 record의 속성은 EF Core가 SQL 컬럼으로 되돌려 읽지 못한다.
        var entries = await ToEntryResponses(db.SavingEntries
                .Where(e => e.SavingId == id)
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id))
            .ToListAsync(cancellationToken);
        return ServiceResult<SavingDetailResponse>.Ok(new SavingDetailResponse(saving, entries));
    }

    public async Task<ServiceResult<SavingResponse>> CreateAsync(SaveSavingRequest request, CancellationToken cancellationToken)
    {
        if (ValidateSaving(request) is { } error)
            return ServiceResult<SavingResponse>.Invalid(error.Field, error.Message);

        var now = DateTime.UtcNow;
        var userId = UserId;
        var saving = new Saving
        {
            // 소유자는 요청 값이 아니라 인증 정보에서 정한다. (CLAUDE.md 규칙)
            UserId = userId,
            Kind = request.Kind!.Value,
            Status = SavingStatus.Active,
            CreatedAt = now,
        };
        Apply(saving, request, now);
        db.Savings.Add(saving);

        // 이미 가입한 상품이면 기존 금액을 기록 1건(Opening)으로. 날짜는 가입일 (결정 D24: 기간별 저금 합계에서는 뺀다)
        if (request.OpeningAmount is > 0)
        {
            // Saving = saving: 아직 Id가 없는 새 저금을 탐색 속성으로 연결한다.
            // SaveChanges 한 번에 EF Core가 저금을 먼저 INSERT하고, 생긴 Id를 기록의 SavingId에 넣어 이어서 INSERT한다.
            // SaveChanges는 한 번의 DB 트랜잭션이라 둘 중 하나만 저장되는 일은 없다.
            db.SavingEntries.Add(new SavingEntry
            {
                UserId = userId,
                Saving = saving,
                Date = saving.StartDate,
                Type = SavingEntryType.Opening,
                Amount = request.OpeningAmount.Value,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<SavingResponse>.Ok(await GetResponseAsync(saving.Id, cancellationToken));
    }

    public async Task<ServiceResult<SavingResponse>> UpdateAsync(int id, SaveSavingRequest request, CancellationToken cancellationToken)
    {
        var saving = await db.Savings.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (saving is null) return ServiceResult<SavingResponse>.NotFound();

        // 종류가 바뀌면 이미 들어간 기록이 규칙에 맞지 않을 수 있다 (예: 자유저금의 출금 기록 → 적금)
        if (request.Kind != saving.Kind)
            return ServiceResult<SavingResponse>.Invalid(nameof(request.Kind), "저금 종류는 바꿀 수 없습니다.");
        if (ValidateSaving(request) is { } error)
            return ServiceResult<SavingResponse>.Invalid(error.Field, error.Message);

        var now = DateTime.UtcNow;
        Apply(saving, request, now);

        // 기존 금액 기록을 요청 값에 맞춘다: 있으면 고치고, 없으면 만들고, 비우면 지운다
        var opening = await db.SavingEntries
            .FirstOrDefaultAsync(e => e.SavingId == id && e.Type == SavingEntryType.Opening, cancellationToken);
        if (request.OpeningAmount is > 0)
        {
            if (opening is null)
            {
                if (saving.Kind == SavingKind.Deposit && await HasEntryAsync(id, SavingEntryType.Deposit, cancellationToken))
                    return ServiceResult<SavingResponse>.Invalid(nameof(request.OpeningAmount), DepositOnceMessage + " 이미 원금 입금 기록이 있습니다.");
                db.SavingEntries.Add(new SavingEntry
                {
                    UserId = saving.UserId, SavingId = id, Type = SavingEntryType.Opening, CreatedAt = now,
                    Date = saving.StartDate, Amount = request.OpeningAmount.Value, UpdatedAt = now,
                });
            }
            else
            {
                opening.Date = saving.StartDate;
                opening.Amount = request.OpeningAmount.Value;
                opening.UpdatedAt = now;
            }
        }
        else if (opening is not null)
        {
            db.SavingEntries.Remove(opening);
        }

        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<SavingResponse>.Ok(await GetResponseAsync(id, cancellationToken));
    }

    // 상태 변경: 진행 중 / 완료(달성) / 보관.
    // 적금·예금은 남은 돈이 있으면 "완료"로 바로 바꿀 수 없다. 만기 수령·중도해지(CloseAsync)로 출금 기록을 남기고 끝낸다.
    public async Task<ServiceResult<SavingResponse>> UpdateStatusAsync(int id, UpdateSavingStatusRequest request, CancellationToken cancellationToken)
    {
        var saving = await db.Savings.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (saving is null) return ServiceResult<SavingResponse>.NotFound();

        var status = request.Status!.Value;
        if (status == SavingStatus.Completed && saving.Kind != SavingKind.Free
            && await BalanceAsync(id, exceptEntryId: null, cancellationToken) > 0)
            return ServiceResult<SavingResponse>.Conflict("적금·예금은 만기 수령 또는 중도해지로 완료하세요.");

        saving.Status = status;
        saving.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<SavingResponse>.Ok(await GetResponseAsync(id, cancellationToken));
    }

    // 저금 삭제. 기록은 DB가 함께 지운다(Cascade). 이자와 연결된 가계부 거래는 여기서 함께 지운다.
    // (기록 → 거래 외래키는 SetNull이라 DB가 거래까지 지우지 않는다. "같이 삭제"는 코드로 처리, 결정 D24)
    // 기록을 남기고 목록에서만 숨기려면 삭제 대신 "보관" 상태를 쓴다.
    public async Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var saving = await db.Savings.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (saving is null) return ServiceResult<bool>.NotFound();

        var linkedTransactions = await db.SavingEntries
            .Where(e => e.SavingId == id && e.Transaction != null)
            .Select(e => e.Transaction!)
            .ToListAsync(cancellationToken);
        db.Transactions.RemoveRange(linkedTransactions);
        db.Savings.Remove(saving);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    // ---- 기록 ----

    public async Task<ServiceResult<SavingEntryResponse>> CreateEntryAsync(int savingId, SaveSavingEntryRequest request, CancellationToken cancellationToken)
    {
        var saving = await db.Savings.FirstOrDefaultAsync(s => s.Id == savingId, cancellationToken);
        if (saving is null) return ServiceResult<SavingEntryResponse>.NotFound();

        var type = request.Type!.Value;
        if (type == SavingEntryType.Opening)
            return ServiceResult<SavingEntryResponse>.Invalid(nameof(request.Type), OpeningMessage);
        if (type == SavingEntryType.Withdrawal && saving.Kind != SavingKind.Free)
            return ServiceResult<SavingEntryResponse>.Invalid(nameof(request.Type), "적금·예금은 출금할 수 없습니다. 만기 수령 또는 중도해지를 사용하세요.");
        if (ValidateEntry(saving, request) is { } error)
            return ServiceResult<SavingEntryResponse>.Invalid(error.Field, error.Message);

        if (saving.Status != SavingStatus.Active)
            return ServiceResult<SavingEntryResponse>.Conflict("진행 중인 저금에만 기록할 수 있습니다.");
        if (type == SavingEntryType.Deposit && saving.Kind == SavingKind.Deposit
            && (await HasEntryAsync(savingId, SavingEntryType.Deposit, cancellationToken)
                || await HasEntryAsync(savingId, SavingEntryType.Opening, cancellationToken)))
            return ServiceResult<SavingEntryResponse>.Conflict(DepositOnceMessage);
        if (type == SavingEntryType.Withdrawal
            && request.Amount!.Value > await BalanceAsync(savingId, exceptEntryId: null, cancellationToken))
            return ServiceResult<SavingEntryResponse>.Invalid(nameof(request.Amount), "현재 금액보다 많이 뺄 수 없습니다.");

        var now = DateTime.UtcNow;
        var entry = new SavingEntry { UserId = saving.UserId, SavingId = savingId, Type = type, CreatedAt = now };
        Apply(entry, request, now);

        if (type == SavingEntryType.Interest && (request.RecordAsIncome ?? true))
        {
            var category = await ResolveIncomeCategoryAsync(request.CategoryId, cancellationToken);
            if (category.Error is not null)
                return ServiceResult<SavingEntryResponse>.Invalid(nameof(request.CategoryId), category.Error);
            // 새 거래를 탐색 속성으로 연결 → SaveChanges 한 번에 거래 INSERT 후 그 Id로 기록 INSERT (CreateAsync의 Opening 설명 참고)
            entry.Transaction = NewIncomeTransaction(saving.UserId, now);
            ApplyIncome(entry.Transaction, entry, saving, category.Id!.Value, now);
        }

        db.SavingEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<SavingEntryResponse>.Ok(await GetEntryResponseAsync(entry.Id, cancellationToken));
    }

    public async Task<ServiceResult<SavingEntryResponse>> UpdateEntryAsync(
        int savingId, int entryId, SaveSavingEntryRequest request, CancellationToken cancellationToken)
    {
        // Include: 연결된 저금·거래를 같은 조회로 함께 가져온다 (JOIN). 아래에서 값을 바꾸면 SaveChanges가 함께 UPDATE한다.
        var entry = await db.SavingEntries
            .Include(e => e.Saving)
            .Include(e => e.Transaction)
            .FirstOrDefaultAsync(e => e.Id == entryId && e.SavingId == savingId, cancellationToken);
        if (entry is null) return ServiceResult<SavingEntryResponse>.NotFound();

        if (entry.Type == SavingEntryType.Opening)
            return ServiceResult<SavingEntryResponse>.Conflict(OpeningMessage);
        if (request.Type != entry.Type)
            return ServiceResult<SavingEntryResponse>.Invalid(nameof(request.Type), "기록 종류는 바꿀 수 없습니다. 지우고 다시 입력하세요.");
        if (ValidateEntry(entry.Saving, request) is { } error)
            return ServiceResult<SavingEntryResponse>.Invalid(error.Field, error.Message);
        if (entry.Type == SavingEntryType.Withdrawal
            && request.Amount!.Value > await BalanceAsync(savingId, exceptEntryId: entryId, cancellationToken))
            return ServiceResult<SavingEntryResponse>.Invalid(nameof(request.Amount), "현재 금액보다 많이 뺄 수 없습니다.");

        var now = DateTime.UtcNow;
        Apply(entry, request, now);

        // 이자 기록은 연결된 가계부 수입도 맞춘다: 켬 → 고치거나 새로 만들기, 끔 → 지우기
        if (entry.Type == SavingEntryType.Interest)
        {
            if (request.RecordAsIncome ?? true)
            {
                // 카테고리를 비우면 지금 연결된 거래의 카테고리를 그대로 쓴다 (없으면 "이자")
                var category = await ResolveIncomeCategoryAsync(request.CategoryId ?? entry.Transaction?.CategoryId, cancellationToken);
                if (category.Error is not null)
                    return ServiceResult<SavingEntryResponse>.Invalid(nameof(request.CategoryId), category.Error);
                entry.Transaction ??= NewIncomeTransaction(entry.UserId, now);
                ApplyIncome(entry.Transaction, entry, entry.Saving, category.Id!.Value, now);
            }
            else if (entry.Transaction is not null)
            {
                db.Transactions.Remove(entry.Transaction);
                entry.Transaction = null;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<SavingEntryResponse>.Ok(await GetEntryResponseAsync(entryId, cancellationToken));
    }

    // 기록 삭제. 이자 기록이면 연결된 가계부 수입도 함께 지운다 (DeleteAsync 설명 참고)
    public async Task<ServiceResult<bool>> DeleteEntryAsync(int savingId, int entryId, CancellationToken cancellationToken)
    {
        var entry = await db.SavingEntries
            .Include(e => e.Transaction)
            .FirstOrDefaultAsync(e => e.Id == entryId && e.SavingId == savingId, cancellationToken);
        if (entry is null) return ServiceResult<bool>.NotFound();
        if (entry.Type == SavingEntryType.Opening)
            return ServiceResult<bool>.Conflict(OpeningMessage);

        if (entry.Transaction is not null) db.Transactions.Remove(entry.Transaction);
        db.SavingEntries.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    // 만기 수령·중도해지 (적금·예금). 한 번에 처리한다:
    //   ① 받은 이자가 있으면 이자 기록 (+ 가계부 수입)
    //   ② 남은 금액 전부를 출금 기록 (메모 "만기 수령" / "중도해지")
    //   ③ 상태를 완료로
    // 모두 SaveChanges 한 번이라 중간에 실패하면 아무것도 저장되지 않는다.
    public async Task<ServiceResult<SavingDetailResponse>> CloseAsync(int id, CloseSavingRequest request, CancellationToken cancellationToken)
    {
        var saving = await db.Savings.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (saving is null) return ServiceResult<SavingDetailResponse>.NotFound();

        var date = request.Date!.Value;
        if (date < saving.StartDate)
            return ServiceResult<SavingDetailResponse>.Invalid(nameof(request.Date), "날짜는 가입일 이후여야 합니다.");
        var interest = request.Interest ?? 0m;
        if (interest < 0)
            return ServiceResult<SavingDetailResponse>.Invalid(nameof(request.Interest), "이자는 0 이상이어야 합니다.");
        if (interest > 0 && AmountError(interest) is { } amountError)
            return ServiceResult<SavingDetailResponse>.Invalid(nameof(request.Interest), amountError);

        if (saving.Kind == SavingKind.Free)
            return ServiceResult<SavingDetailResponse>.Conflict("자유저금은 출금한 뒤 상태를 완료로 바꾸세요.");
        if (saving.Status != SavingStatus.Active)
            return ServiceResult<SavingDetailResponse>.Conflict("진행 중인 저금만 만기 수령·중도해지할 수 있습니다.");

        var now = DateTime.UtcNow;
        var label = request.Type == SavingCloseType.Maturity ? "만기 수령" : "중도해지";
        var balance = await BalanceAsync(id, exceptEntryId: null, cancellationToken);

        if (interest > 0)
        {
            var interestEntry = new SavingEntry
            {
                UserId = saving.UserId, SavingId = id, Type = SavingEntryType.Interest, CreatedAt = now, UpdatedAt = now,
                Date = date, Amount = interest, Memo = $"{label} 이자",
            };
            if (request.RecordInterestAsIncome ?? true)
            {
                var category = await ResolveIncomeCategoryAsync(request.CategoryId, cancellationToken);
                if (category.Error is not null)
                    return ServiceResult<SavingDetailResponse>.Invalid(nameof(request.CategoryId), category.Error);
                interestEntry.Transaction = NewIncomeTransaction(saving.UserId, now);
                ApplyIncome(interestEntry.Transaction, interestEntry, saving, category.Id!.Value, now);
            }
            db.SavingEntries.Add(interestEntry);
            balance += interest;
        }

        // 받은 돈(원금 + 이자)을 모두 꺼냈으므로 현재 금액이 0이 된다
        if (balance > 0)
        {
            db.SavingEntries.Add(new SavingEntry
            {
                UserId = saving.UserId, SavingId = id, Type = SavingEntryType.Withdrawal, CreatedAt = now, UpdatedAt = now,
                Date = date, Amount = balance, Memo = label,
            });
        }

        saving.Status = SavingStatus.Completed;
        saving.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    // ---- 내부 도우미 ----

    private string UserId => currentUser.Id ?? throw new InvalidOperationException("로그인한 사용자가 없습니다.");

    // 저금 → 응답 DTO. 현재 금액(Balance)은 그 저금의 기록 합계를 SQL 하위 쿼리(SELECT SUM ...)로 함께 계산한다.
    // 출금만 빼고 나머지(기존 금액·입금·이자)는 더한다. 기록이 없으면 SUM이 NULL이지만 EF Core가 0으로 바꿔 준다(COALESCE).
    // db.SavingEntries에도 Global Query Filter가 붙으므로 하위 쿼리도 내 기록만 더한다.
    private IQueryable<SavingResponse> ToResponses(IQueryable<Saving> savings) =>
        savings.Select(s => new SavingResponse(
            s.Id, s.Name, s.Kind, s.BankName, s.TargetAmount, s.StartDate, s.MaturityDate, s.InterestRate,
            s.InterestMethod, s.TaxRate, s.PaymentType, s.PlannedAmount, s.Status, s.Memo,
            db.SavingEntries
                .Where(e => e.SavingId == s.Id)
                .Sum(e => e.Type == SavingEntryType.Withdrawal ? -e.Amount : e.Amount)));

    // 기록 → 응답 DTO. 연결된 거래가 있으면 그 카테고리 Id·이름을 JOIN으로 가져온다 (없으면 null)
    private static IQueryable<SavingEntryResponse> ToEntryResponses(IQueryable<SavingEntry> entries) =>
        entries.Select(e => new SavingEntryResponse(
            e.Id, e.Date, e.Type, e.Amount, e.Memo, e.TransactionId,
            e.Transaction!.CategoryId, e.Transaction.Category!.Name));

    private async Task<SavingResponse> GetResponseAsync(int id, CancellationToken cancellationToken) =>
        await ToResponses(db.Savings.Where(s => s.Id == id)).SingleAsync(cancellationToken);

    private async Task<SavingEntryResponse> GetEntryResponseAsync(int id, CancellationToken cancellationToken) =>
        await ToEntryResponses(db.SavingEntries.Where(e => e.Id == id)).SingleAsync(cancellationToken);

    // 현재 금액. 출금 수정 때는 고치는 기록 자신(exceptEntryId)을 빼고 계산한다
    private Task<decimal> BalanceAsync(int savingId, int? exceptEntryId, CancellationToken cancellationToken) =>
        db.SavingEntries
            .Where(e => e.SavingId == savingId && e.Id != exceptEntryId)
            .SumAsync(e => e.Type == SavingEntryType.Withdrawal ? -e.Amount : e.Amount, cancellationToken);

    private Task<bool> HasEntryAsync(int savingId, SavingEntryType type, CancellationToken cancellationToken) =>
        db.SavingEntries.AnyAsync(e => e.SavingId == savingId && e.Type == type, cancellationToken);

    // 이자 수입의 카테고리를 정한다. 요청한 Id가 있으면 "내 수입 카테고리"인지 확인하고, 없으면 내 "이자" 수입 대분류를 쓴다.
    // (Id, Error) 튜플: 둘 중 하나만 채워서 돌려준다
    private async Task<(int? Id, string? Error)> ResolveIncomeCategoryAsync(int? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId is not null)
        {
            // Global Query Filter 때문에 남의 카테고리는 찾지 못한다 → "찾을 수 없음" (TransactionService.ValidateAsync와 같은 확인)
            var type = await db.Categories
                .Where(c => c.Id == categoryId)
                .Select(c => (CategoryType?)c.Type)
                .FirstOrDefaultAsync(cancellationToken);
            if (type is null) return (null, "카테고리를 찾을 수 없습니다.");
            if (type != CategoryType.Income) return (null, "이자는 수입 카테고리를 선택하세요.");
            return (categoryId, null);
        }

        var defaultId = await db.Categories
            .Where(c => c.Type == CategoryType.Income && c.ParentId == null && c.Name == InterestCategoryName)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return defaultId is null
            ? (null, $"\"{InterestCategoryName}\" 수입 카테고리가 없습니다. 수입 카테고리를 선택하세요.")
            : (defaultId, null);
    }

    private static Transaction NewIncomeTransaction(string userId, DateTime now) =>
        new() { UserId = userId, Type = TransactionType.Income, CreatedAt = now };

    // 이자 기록의 날짜·금액을 가계부 수입 거래에 옮긴다. 메모가 없으면 "저금 이름 이자"
    private static void ApplyIncome(Transaction transaction, SavingEntry entry, Saving saving, int categoryId, DateTime now)
    {
        transaction.Date = entry.Date;
        transaction.Amount = entry.Amount;
        transaction.CategoryId = categoryId;
        transaction.Memo = entry.Memo ?? $"{saving.Name} {InterestCategoryName}";
        transaction.UpdatedAt = now;
    }

    // 요청 값을 저금 Entity에 옮긴다 (등록·수정 공통). 종류에 맞지 않는 칸은 null로 비운다.
    private static void Apply(Saving saving, SaveSavingRequest request, DateTime now)
    {
        saving.Name = request.Name.Trim();
        saving.BankName = Clean(request.BankName);
        saving.TargetAmount = saving.Kind == SavingKind.Free ? request.TargetAmount : null;
        saving.StartDate = request.StartDate!.Value;
        saving.MaturityDate = request.MaturityDate;
        saving.InterestRate = request.InterestRate;
        // ??: 왼쪽이 null이면 오른쪽 값. 비우면 단리, 일반과세 15.4%
        saving.InterestMethod = request.InterestMethod ?? InterestMethod.Simple;
        saving.TaxRate = request.TaxRate ?? DefaultTaxRate;
        saving.PaymentType = saving.Kind == SavingKind.Installment ? request.PaymentType : null;
        saving.PlannedAmount = saving.Kind == SavingKind.Deposit ? null : request.PlannedAmount;
        saving.Memo = Clean(request.Memo);
        saving.UpdatedAt = now;
    }

    private static void Apply(SavingEntry entry, SaveSavingEntryRequest request, DateTime now)
    {
        entry.Date = request.Date!.Value;
        entry.Amount = request.Amount!.Value;
        entry.Memo = Clean(request.Memo);
        entry.UpdatedAt = now;
    }

    // 공백만 있는 글자는 저장하지 않는다(null)
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    // 저금 입력값 확인 (종류별 필수 칸 등). 문제가 없으면 null.
    // 반환 타입 (string Field, string Message)?: 이름 붙은 튜플 + "?" → "오류 한 건 또는 없음".
    // 호출하는 쪽은 `if (ValidateSaving(request) is { } error)`로 "null이 아니면 error라는 이름으로 받기"를 한다 (속성 패턴)
    private static (string Field, string Message)? ValidateSaving(SaveSavingRequest request)
    {
        if (request.Name.Trim() == "")
            return (nameof(request.Name), "저금 이름을 입력하세요.");

        var kind = request.Kind!.Value;
        var start = request.StartDate!.Value;

        if (kind == SavingKind.Free)
        {
            if (request.MaturityDate is { } targetDate && targetDate <= start)
                return (nameof(request.MaturityDate), "목표일은 시작일보다 뒤여야 합니다.");
        }
        else
        {
            if (request.MaturityDate is null)
                return (nameof(request.MaturityDate), "만기일을 입력하세요.");
            if (request.MaturityDate <= start)
                return (nameof(request.MaturityDate), "만기일은 가입일보다 뒤여야 합니다.");
            if (request.InterestRate is null)
                return (nameof(request.InterestRate), "연이율을 입력하세요.");
        }

        if (request.InterestRate is { } rate && (rate < 0 || rate > 100 || rate != decimal.Round(rate, 3)))
            return (nameof(request.InterestRate), "연이율은 0~100 사이, 소수 셋째 자리까지 입력하세요.");
        if (request.TaxRate is { } tax && (tax < 0 || tax > 100 || tax != decimal.Round(tax, 2)))
            return (nameof(request.TaxRate), "세율은 0~100 사이, 소수 둘째 자리까지 입력하세요.");

        if (kind == SavingKind.Installment)
        {
            if (request.PaymentType is null)
                return (nameof(request.PaymentType), "납입 방식을 선택하세요.");
            // 정액·26주형은 계획 금액(월 납입액·첫 주 금액)이 있어야 일정을 계산할 수 있다. 자유적립은 선택
            if (request.PaymentType != SavingPaymentType.Flexible && request.PlannedAmount is null)
                return (nameof(request.PlannedAmount),
                    request.PaymentType == SavingPaymentType.Fixed ? "월 납입액을 입력하세요." : "첫 주 금액을 입력하세요.");
        }

        if (kind == SavingKind.Free && request.TargetAmount is { } target && AmountError(target) is { } targetError)
            return (nameof(request.TargetAmount), targetError);
        if (kind != SavingKind.Deposit && request.PlannedAmount is { } planned && AmountError(planned) is { } plannedError)
            return (nameof(request.PlannedAmount), plannedError);
        // 기존 금액 0은 "없음"으로 본다. 음수·너무 큰 값·소수 셋째 자리는 거절
        if (request.OpeningAmount is { } opening && opening != 0 && AmountError(opening) is { } openingError)
            return (nameof(request.OpeningAmount), openingError);

        return null;
    }

    // 기록 입력값 확인 (금액·날짜). 종류별 규칙(출금 가능 여부 등)은 호출하는 쪽에서 확인한다
    private static (string Field, string Message)? ValidateEntry(Saving saving, SaveSavingEntryRequest request)
    {
        if (AmountError(request.Amount!.Value) is { } amountError)
            return (nameof(request.Amount), amountError);
        if (request.Date!.Value < saving.StartDate)
            return (nameof(request.Date), "날짜는 시작일(가입일) 이후여야 합니다.");
        return null;
    }

    // 금액 공통 규칙 (TransactionService.ValidateAsync와 같다). 문제가 없으면 null
    private static string? AmountError(decimal amount)
    {
        if (amount <= 0) return "금액은 0보다 커야 합니다.";
        if (amount > TransactionService.MaxAmount) return "금액이 너무 큽니다.";
        if (amount != decimal.Round(amount, 2)) return "금액은 소수 둘째 자리까지 입력할 수 있습니다.";
        return null;
    }
}
