using Microsoft.EntityFrameworkCore;

namespace Finora.Api.Data;

// DB 구조 적용 명령어 (Phase 7). 운영 서버에는 dotnet ef 도구가 없으므로 앱이 직접 Migration을 적용한다.
//   docker compose -f compose.prod.yaml run --rm migrate   (compose가 배포 때 자동으로 먼저 실행한다)
// 웹 서버를 시작할 때 자동 적용하지 않고 명령어로 분리한 이유: 적용이 실패하면 웹 서버가 뜨지 않고 여기서 멈춰서 원인을 바로 볼 수 있다.
public static class MigrateCommand
{
    public static async Task<int> RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinoraDbContext>();

        // 아직 적용하지 않은 Migration 목록. 이미 최신이면 아무것도 하지 않는다 (여러 번 실행해도 안전)
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0)
        {
            Console.WriteLine("DB가 이미 최신입니다.");
            return 0;
        }

        Console.WriteLine($"Migration 적용: {string.Join(", ", pending)}");
        // MigrateAsync: 남은 Migration을 순서대로 적용한다. 하나라도 실패하면 예외가 나서 0이 아닌 종료 코드로 끝난다.
        await db.Database.MigrateAsync();
        Console.WriteLine("완료");
        return 0;
    }
}
