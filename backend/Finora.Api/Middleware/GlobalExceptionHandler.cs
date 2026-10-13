using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Finora.Api.Middleware;

// 처리되지 않은 예외를 한곳에서 잡아 같은 형식(ProblemDetails JSON)의 500 응답으로 바꾼다.
// Program.cs의 app.UseExceptionHandler()가 예외 발생 시 이 클래스를 호출한다.
//
// 응답 예:
//   { "type": "...", "title": "서버 오류가 발생했습니다.", "status": 500, "traceId": "00-ab12..." }
// 예외 메시지·스택은 로그에만 남기고 응답에는 넣지 않는다. (내부 정보 노출 방지)
// 사용자가 traceId를 알려 주면 로그에서 같은 traceId로 원인을 찾을 수 있다.
public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    // true를 돌려주면 "이 예외는 처리했다"는 뜻이다.
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception: {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // ProblemDetails 형식으로 쓴다. traceId는 AddProblemDetails()가 자동으로 붙인다.
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "서버 오류가 발생했습니다.",
            },
        });
    }
}
