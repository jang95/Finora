// Program.cs = Backend의 시작점. 두 부분으로 나뉜다.
//   1) builder.Services.Add...  : 앱이 쓸 기능(서비스)을 등록한다. (의존성 주입 컨테이너)
//   2) app.Use... / app.Map...  : 요청이 들어왔을 때 거쳐 갈 순서(파이프라인)를 정한다.
using Finora.Api.Auth;
using Finora.Api.Data;
using Finora.Api.Data.Entities;
using Finora.Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DB 연결. 접속 문자열은 코드/appsettings에 두지 않는다. (Public 저장소)
//   개발: User Secrets — dotnet user-secrets set "ConnectionStrings:Finora" "..."
//   운영: 환경 변수 ConnectionStrings__Finora
var connectionString = builder.Configuration.GetConnectionString("Finora")
    ?? throw new InvalidOperationException("ConnectionStrings:Finora 설정이 없습니다. User Secrets에 접속 문자열을 등록하세요.");
// AddDbContext: 요청마다 FinoraDbContext를 하나씩 만들어 Controller 생성자에 넣어 준다.
builder.Services.AddDbContext<FinoraDbContext>(options => options.UseNpgsql(connectionString));

// ASP.NET Core Identity: 비밀번호 Hash, 로그인 실패 잠금 등을 제공한다. (결정 D1)
// AddIdentityCore는 화면(Razor Pages) 없이 핵심 기능만 등록한다. 화면은 React가 담당한다.
builder.Services.AddIdentityCore<FinoraUser>(options =>
    {
        // 비밀번호: 복잡한 조합 규칙보다 길이가 보안에 더 효과적이라 길이만 강하게 요구한다.
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = true;

        // 로그인 5회 실패 시 15분 잠금 (계획서 9장)
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<FinoraDbContext>() // 사용자 정보를 FinoraDbContext(AspNetUsers 테이블)에 저장
    .AddSignInManager();                         // 로그인/로그아웃 처리(SignInManager)

// 인증 방식 = Identity Cookie. 로그인하면 브라우저에 인증 Cookie가 저장되고, 이후 요청마다 자동으로 함께 전송된다.
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "finora.auth";
    options.Cookie.HttpOnly = true;                           // JavaScript에서 Cookie를 읽을 수 없다 (XSS로 탈취 방지)
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS로만 전송 (브라우저는 localhost는 예외로 허용)
    options.Cookie.SameSite = SameSiteMode.Strict;            // 다른 사이트에서 시작된 요청에는 Cookie를 보내지 않는다 (CSRF 방지)

    // 기본 동작은 로그인 페이지로 이동(302)이지만, API는 상태 코드로만 알려 준다.
    // 화면 이동은 React가 401을 받고 처리한다.
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// 기본값을 "로그인 필요"로 둔다 (fallback policy).
// 새 API에 [Authorize]를 깜빡해도 자동으로 막힌다. 로그인 없이 열어야 하는 곳만 [AllowAnonymous]를 붙인다.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Controllers/ 폴더의 Controller 클래스들을 사용한다.
builder.Services.AddControllers();
// API 명세(OpenAPI 문서)를 만든다. 개발 중 /openapi/v1.json 에서 확인할 수 있다.
builder.Services.AddOpenApi();

// 오류 응답을 모두 ProblemDetails(표준 JSON 오류 형식)로 통일한다.
// traceId가 자동으로 붙어서, 로그에서 같은 요청을 찾을 수 있다.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// 명령어 모드: "dotnet run --project Finora.Api -- invite create" 처럼 실행하면 웹 서버를 띄우지 않고 명령만 처리하고 끝낸다.
if (args is ["invite", ..])
{
    Environment.ExitCode = await InviteCommand.RunAsync(app.Services, args);
    return;
}

// 파이프라인 맨 앞에 둔다. 뒤 단계 어디서 예외가 나도 여기서 잡힌다.
app.UseExceptionHandler();
// 404처럼 본문 없는 오류 응답에도 ProblemDetails 본문을 채운다.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // 개발용 API 문서는 로그인 없이 볼 수 있게 연다.
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

// 순서가 중요하다: 먼저 Cookie로 "누구인지" 확인(Authentication)하고, 그다음 "허용되는지" 판단(Authorization)한다.
app.UseAuthentication();
app.UseAuthorization();

// 각 Controller의 [Route] 주소로 요청을 연결한다.
app.MapControllers();

app.Run();
