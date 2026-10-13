// Program.cs = Backend의 시작점. 두 부분으로 나뉜다.
//   1) builder.Services.Add...  : 앱이 쓸 기능(서비스)을 등록한다. (의존성 주입 컨테이너)
//   2) app.Use... / app.Map...  : 요청이 들어왔을 때 거쳐 갈 순서(파이프라인)를 정한다.
using Finora.Api.Data;
using Finora.Api.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DB 연결. 접속 문자열은 코드/appsettings에 두지 않는다. (Public 저장소)
//   개발: User Secrets — dotnet user-secrets set "ConnectionStrings:Finora" "..."
//   운영: 환경 변수 ConnectionStrings__Finora
var connectionString = builder.Configuration.GetConnectionString("Finora")
    ?? throw new InvalidOperationException("ConnectionStrings:Finora 설정이 없습니다. User Secrets에 접속 문자열을 등록하세요.");
// AddDbContext: 요청마다 FinoraDbContext를 하나씩 만들어 Controller 생성자에 넣어 준다.
builder.Services.AddDbContext<FinoraDbContext>(options => options.UseNpgsql(connectionString));

// Controllers/ 폴더의 Controller 클래스들을 사용한다.
builder.Services.AddControllers();
// API 명세(OpenAPI 문서)를 만든다. 개발 중 /openapi/v1.json 에서 확인할 수 있다.
builder.Services.AddOpenApi();

// 오류 응답을 모두 ProblemDetails(표준 JSON 오류 형식)로 통일한다.
// traceId가 자동으로 붙어서, 로그에서 같은 요청을 찾을 수 있다.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// 파이프라인 맨 앞에 둔다. 뒤 단계 어디서 예외가 나도 여기서 잡힌다.
app.UseExceptionHandler();
// 404처럼 본문 없는 오류 응답에도 ProblemDetails 본문을 채운다.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// 각 Controller의 [Route] 주소로 요청을 연결한다.
app.MapControllers();

app.Run();
