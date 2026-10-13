using Microsoft.AspNetCore.Mvc;

namespace Finora.Api.Controllers;

// 여러 Controller가 함께 쓰는 응답 도우미.
// 확장 메서드(this ControllerBase): ControllerBase에 원래 있던 메서드처럼 this.ValidationErrors(...)로 부를 수 있다.
public static class ControllerExtensions
{
    // 입력칸별 오류를 [ApiController]의 자동 입력 검사와 같은 400 형식(ValidationProblemDetails)으로 응답한다.
    // Frontend는 어느 쪽 오류든 errors만 읽으면 된다.
    public static ActionResult ValidationErrors(this ControllerBase controller, IReadOnlyDictionary<string, string[]> errors)
    {
        foreach (var (field, messages) in errors)
            foreach (var message in messages)
                controller.ModelState.AddModelError(field, message);
        return controller.ValidationProblem(controller.ModelState);
    }
}
