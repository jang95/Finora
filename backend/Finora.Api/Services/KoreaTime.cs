namespace Finora.Api.Services;

// "오늘"을 한국 시간 기준 날짜로 구한다. (결정 D18)
// 배포 서버는 UTC일 수 있다. UTC로 날짜를 정하면 한국 1일 오전 0~9시가 아직 전달로 계산된다.
// 지금은 화면이 기간(from/to)을 정해서 보내므로 쓰는 곳이 없다. Phase 8 월말 스냅샷처럼
// 서버가 스스로 "오늘"을 정해야 할 때 쓴다. (KoreaTimeTests에서 계산을 확인해 둔다)
public static class KoreaTime
{
    // IANA 시간대 이름. .NET 6부터 Windows·Linux 모두 이 이름을 알아본다.
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");

    // TimeProvider: "지금 시각"을 알려주는 .NET 기본 클래스. DateTime.UtcNow를 직접 부르는 대신 이것을 받으면,
    // 테스트에서 원하는 시각(예: 월말 자정 직전)으로 바꿔 끼울 수 있다. 실제 서버는 TimeProvider.System(진짜 시계)을 쓴다.
    public static DateOnly Today(TimeProvider time) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), Zone).DateTime);
}
