using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Finora.Api.Tests;

// WebApplicationFactory = 테스트 안에서 Backend(Program.cs)를 실제 포트 없이 메모리에서 띄워 주는 도구.
// factory.CreateClient()로 받은 HttpClient로 요청하면 진짜 요청과 똑같이 Middleware·Controller를 거친다.
public class FinoraApiFactory : WebApplicationFactory<Program>
{
    // 아무것도 없는 포트(1)를 가리키는 가짜 접속 문자열.
    // User Secrets나 Docker 실행 여부와 상관없이 항상 같은 결과가 나오게 한다. (DB 연결은 항상 실패)
    // 실제 DB가 필요한 테스트(사용자 분리 등)는 Phase 4에서 별도로 준비한다.
    private const string UnreachableDb = "Host=127.0.0.1;Port=1;Database=test;Username=test;Password=test;Timeout=1";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Finora", UnreachableDb);
    }
}
