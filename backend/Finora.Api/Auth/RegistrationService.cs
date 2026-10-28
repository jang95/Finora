using Finora.Api.Data;
using Finora.Api.Data.Entities;
using Finora.Api.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Finora.Api.Auth;

// 초대코드 회원가입 (결정 D3).
// UserManager<FinoraUser> = Identity가 제공하는 사용자 관리 도구. 비밀번호 규칙 검사, Hash 저장, 중복 확인을 해 준다.
public class RegistrationService(FinoraDbContext db, UserManager<FinoraUser> userManager)
{
    private const string InvalidInviteMessage = "유효하지 않은 초대코드입니다.";

    // 성공하면 빈 목록, 실패하면 "입력칸 이름 → 오류 문구들"을 돌려준다.
    public async Task<Dictionary<string, string[]>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        // 사용자가 소문자·공백을 섞거나 하이픈(-)을 빼고 입력해도 같은 코드로 본다. (하이픈은 읽기 쉽게 넣은 구분자일 뿐)
        var code = NormalizeCode(request.InviteCode);

        // 먼저 확인해서, 코드가 틀렸을 때는 사용자를 만들지 않는다.
        // 존재하지 않는 코드와 이미 쓴 코드는 같은 문구로 응답한다. (어떤 코드가 있었는지 알려주지 않음)
        // DB의 코드도 하이픈을 빼고 비교한다. string.Replace는 EF Core가 SQL의 replace() 함수로 바꿔서 DB에서 계산한다.
        var inviteAvailable = await db.InviteCodes.AnyAsync(x => x.Code.Replace("-", "") == code && x.UsedAt == null, cancellationToken);
        if (!inviteAvailable)
            return Error(nameof(RegisterRequest.InviteCode), InvalidInviteMessage);

        // 트랜잭션: 아래 작업들을 "전부 성공 또는 전부 취소"로 묶는다.
        // Commit 전에 실패하거나 예외가 나면 await using이 끝날 때 자동으로 취소(Rollback)된다.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var email = request.Email.Trim();
        // 로그인 ID로 이메일을 쓴다. (UserName = Email)
        var user = new FinoraUser { UserName = email, Email = email, CreatedAt = DateTime.UtcNow };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return ToFieldErrors(result.Errors);

        // 초대코드를 "아직 안 쓴 경우에만" 사용 처리한다. 확인과 변경을 UPDATE 한 번으로 해서,
        // 두 사람이 같은 코드로 동시에 가입해도 한 명만 성공한다. (위의 AnyAsync 확인만으로는 막지 못함)
        var now = DateTime.UtcNow;
        var claimed = await db.InviteCodes
            .Where(x => x.Code.Replace("-", "") == code && x.UsedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.UsedAt, now)
                .SetProperty(x => x.UsedByUserId, user.Id), cancellationToken);
        if (claimed == 0)
            return Error(nameof(RegisterRequest.InviteCode), InvalidInviteMessage); // Commit하지 않았으므로 사용자 생성도 취소된다

        // 기본 카테고리 생성. 가입 중에는 아직 로그인 전이라, UserId를 방금 만든 사용자로 직접 지정한다.
        db.Categories.AddRange(DefaultCategories.CreateFor(user.Id));
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return [];
    }

    // "k7qm-x2pd r9tb " → "K7QMX2PDR9TB": 하이픈과 공백을 모두 빼고 대문자로 바꾼다
    private static string NormalizeCode(string input) =>
        new string(input.Where(c => c != '-' && !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

    private static Dictionary<string, string[]> Error(string field, string message) => new() { [field] = [message] };

    // Identity 오류(영어)를 입력칸별 한국어 문구로 바꾼다.
    private static Dictionary<string, string[]> ToFieldErrors(IEnumerable<IdentityError> errors) =>
        errors
            .Select(ToFieldError)
            .Distinct() // DuplicateUserName과 DuplicateEmail이 함께 와도 문구는 한 번만
            .GroupBy(x => x.Field)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray());

    // (string Field, string Message) = 이름 붙은 튜플. 작은 값 두 개를 클래스 없이 묶어 돌려준다.
    // PasswordRules.Message(e) is { } message: 결과가 null이 아니면 message라는 이름으로 받아서 쓴다 (패턴 매칭)
    private static (string Field, string Message) ToFieldError(IdentityError e) => e.Code switch
    {
        _ when PasswordRules.Message(e) is { } message => (nameof(RegisterRequest.Password), message),
        "DuplicateUserName" or "DuplicateEmail" => (nameof(RegisterRequest.Email), "이미 가입된 이메일입니다."),
        "InvalidUserName" or "InvalidEmail" => (nameof(RegisterRequest.Email), "이메일 형식이 올바르지 않습니다."),
        _ => (string.Empty, e.Description),
    };
}
