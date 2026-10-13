namespace Finora.Api.Services;

// Service 작업 결과: 성공(값) / 없음(404) / 입력 오류(400, 입력칸별 문구) 중 하나.
// 예외(throw) 대신 결과로 돌려주면, Controller가 if 몇 줄로 알맞은 HTTP 응답을 고를 수 있다.
// <T>: 성공했을 때 돌려줄 값의 타입 (제네릭)
public class ServiceResult<T>
{
    public T? Value { get; private init; }
    public bool IsNotFound { get; private init; }
    // null이 아니면 입력 오류. 키는 요청 DTO의 속성 이름(Name 등) — Frontend가 입력칸 옆에 보여준다.
    public Dictionary<string, string[]>? Errors { get; private init; }

    // static 메서드로만 만들게 해서, 세 경우 중 하나만 채워진 결과가 되도록 한다.
    public static ServiceResult<T> Ok(T value) => new() { Value = value };
    public static ServiceResult<T> NotFound() => new() { IsNotFound = true };
    public static ServiceResult<T> Invalid(string field, string message) => new() { Errors = new() { [field] = [message] } };
}
