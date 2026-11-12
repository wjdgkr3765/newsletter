using Microsoft.Extensions.Caching.Memory;

namespace NewsletterApp.Services;

// 같은 아이디 문자열로 5번 연속 실패하면 10분 동안 로그인을 막는다. (security.md 2번)
public interface ILoginAttemptService
{
    bool IsLocked(string loginId);

    void RecordFailure(string loginId);

    void Reset(string loginId);
}

public sealed class LoginAttemptService(IMemoryCache cache, IClock clock) : ILoginAttemptService
{
    public const int MaxFailures = 5;
    private const string KeyPrefix = "login-fail:";
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan EntryLifetime = TimeSpan.FromHours(1);
    private readonly Lock _lock = new();

    public bool IsLocked(string loginId)
    {
        lock (_lock)
        {
            return cache.TryGetValue(KeyPrefix + loginId, out Attempt? attempt)
                && attempt is not null
                && attempt.LockedUntil > clock.UtcNow;
        }
    }

    public void RecordFailure(string loginId)
    {
        lock (_lock)
        {
            if (!cache.TryGetValue(KeyPrefix + loginId, out Attempt? attempt) || attempt is null)
            {
                attempt = new Attempt();
            }
            attempt.FailCount++;
            if (attempt.FailCount >= MaxFailures)
            {
                attempt.LockedUntil = clock.UtcNow.Add(LockDuration);
                attempt.FailCount = 0;
            }
            cache.Set(KeyPrefix + loginId, attempt, EntryLifetime);
        }
    }

    public void Reset(string loginId)
    {
        lock (_lock)
        {
            cache.Remove(KeyPrefix + loginId);
        }
    }

    private sealed class Attempt
    {
        public int FailCount { get; set; }

        public DateTime LockedUntil { get; set; } = DateTime.MinValue;
    }
}
