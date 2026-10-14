using Finora.Api.Services;

namespace Finora.Api.Tests;

// "오늘"은 한국 시간 기준 (결정 D18). 서버·DB 없이 계산만 확인한다.
public class KoreaTimeTests
{
    [Theory]
    // UTC 10월 31일 14:59 = 한국 10월 31일 23:59 → 아직 10월 31일
    [InlineData("2026-10-31T14:59:00Z", "2026-10-31")]
    // UTC 10월 31일 15:00 = 한국 11월 1일 00:00 → 11월 1일 (UTC로는 아직 10월이지만 한국은 다음 달)
    [InlineData("2026-10-31T15:00:00Z", "2026-11-01")]
    // UTC 12월 31일 15:30 = 한국 1월 1일 → 해도 바뀐다
    [InlineData("2026-12-31T15:30:00Z", "2027-01-01")]
    public void Today_UsesKoreaTime(string utcNow, string expected)
    {
        var clock = new TestClock(DateTimeOffset.Parse(utcNow));

        Assert.Equal(DateOnly.Parse(expected), KoreaTime.Today(clock));
    }

    // TimeProvider를 상속해서 "지금"만 바꾼다. override: 부모 클래스의 동작을 이 클래스에서 다시 정의한다.
    private class TestClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
