using System.Security.Cryptography;
using Finora.Api.Data;
using Finora.Api.Data.Entities;

namespace Finora.Api.Auth;

// 초대코드 발급 명령어 (결정 D14). 웹 API가 아니라 서버에서 직접 실행한다.
//   dotnet run --project Finora.Api -- invite create
// "--" 뒤의 값(invite create)이 Program.cs의 args로 들어온다.
public static class InviteCommand
{
    // 헷갈리는 글자(0/O, 1/I/L)를 뺀 32자. 사람이 보고 옮겨 적기 쉽게 한다.
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    // static = 객체를 만들지 않고 InviteCommand.RunAsync(...)로 바로 호출하는 메서드
    public static async Task<int> RunAsync(IServiceProvider services, string[] args)
    {
        if (args is not ["invite", "create"])
        {
            Console.WriteLine("사용법: dotnet run --project Finora.Api -- invite create");
            return 1;
        }

        // Controller 밖에서는 DbContext를 자동으로 받지 못하므로, scope(요청 하나와 같은 범위)를 직접 만들어 꺼낸다.
        // using: 블록이 끝나면 scope와 그 안의 DbContext를 정리(Dispose)한다.
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinoraDbContext>();

        var invite = new InviteCode { Code = Generate(), CreatedAt = DateTime.UtcNow };
        db.InviteCodes.Add(invite);
        await db.SaveChangesAsync();

        // 코드는 화면에만 출력한다. 파일·로그·저장소에 남기지 않는다. (Public 저장소)
        Console.WriteLine($"초대코드: {invite.Code}");
        return 0;
    }

    // 예: K7QM-X2PD-R9TB (12자 + 구분용 하이픈)
    // RandomNumberGenerator = 예측할 수 없는 보안용 난수. (Random은 예측 가능해서 코드·비밀번호 생성에 쓰면 안 된다)
    private static string Generate()
    {
        var raw = RandomNumberGenerator.GetString(Alphabet, 12);
        return $"{raw[..4]}-{raw[4..8]}-{raw[8..]}";
    }
}
