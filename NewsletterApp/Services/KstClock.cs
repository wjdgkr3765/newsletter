namespace NewsletterApp.Services;

// 현재 시각. 테스트에서 바꿔 끼울 수 있도록 인터페이스로 쓴다.
public interface IClock
{
    DateTime UtcNow { get; }

    DateTime KstNow { get; }

    DateOnly KstToday { get; }
}

public sealed class KstClock : IClock
{
    private static readonly TimeZoneInfo Kst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime KstNow => ToKst(UtcNow);

    public DateOnly KstToday => DateOnly.FromDateTime(KstNow);

    public static DateTime ToKst(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Kst);
}
