using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace NewsletterApp.Services;

public enum CodeCheckStatus
{
    Ok,
    Invalid,
    Expired
}

public sealed record CodeCheckResult(CodeCheckStatus Status, int RemainingAttempts);

// 인증 코드: 6자리 숫자, 10분 유효, 5번 틀리면 사용 불가. 확인하면 30분 동안 인증 완료 상태.
public interface IVerificationCodeService
{
    string Issue(string key);

    CodeCheckResult Verify(string key, string code);

    bool IsVerified(string key);

    void Consume(string key);
}

public sealed class VerificationCodeService(IMemoryCache cache, IClock clock) : IVerificationCodeService
{
    public const int ExpiresInSeconds = 600;
    public const int MaxAttempts = 5;
    private const string KeyPrefix = "code:";
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromSeconds(ExpiresInSeconds);
    private static readonly TimeSpan VerifiedLifetime = TimeSpan.FromMinutes(30);

    public static string SignupKey(string email) => $"signup:{email}";

    public static string PasswordResetKey(long userId) => $"password-reset:{userId}";

    public static string EmailChangeKey(long userId, string email) => $"email-change:{userId}:{email}";

    public string Issue(string key)
    {
        string code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var entry = new CodeEntry(code, clock.UtcNow.Add(CodeLifetime));
        cache.Set(KeyPrefix + key, entry, CodeLifetime + VerifiedLifetime);
        return code;
    }

    public CodeCheckResult Verify(string key, string code)
    {
        if (!cache.TryGetValue(KeyPrefix + key, out CodeEntry? entry) || entry is null)
        {
            return new CodeCheckResult(CodeCheckStatus.Expired, 0);
        }
        lock (entry)
        {
            DateTime now = clock.UtcNow;
            if (entry.VerifiedUntil > now)
            {
                return new CodeCheckResult(CodeCheckStatus.Ok, 0);
            }
            if (entry.ExpiresAt <= now || entry.FailCount >= MaxAttempts)
            {
                return new CodeCheckResult(CodeCheckStatus.Expired, 0);
            }
            bool isMatch = CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(entry.Code), Encoding.ASCII.GetBytes(code));
            if (!isMatch)
            {
                entry.FailCount++;
                return new CodeCheckResult(CodeCheckStatus.Invalid, MaxAttempts - entry.FailCount);
            }
            entry.VerifiedUntil = now.Add(VerifiedLifetime);
            return new CodeCheckResult(CodeCheckStatus.Ok, 0);
        }
    }

    public bool IsVerified(string key)
    {
        if (!cache.TryGetValue(KeyPrefix + key, out CodeEntry? entry) || entry is null)
        {
            return false;
        }
        lock (entry)
        {
            return entry.VerifiedUntil > clock.UtcNow;
        }
    }

    public void Consume(string key) => cache.Remove(KeyPrefix + key);

    private sealed class CodeEntry(string code, DateTime expiresAt)
    {
        public string Code { get; } = code;

        public DateTime ExpiresAt { get; } = expiresAt;

        public int FailCount { get; set; }

        public DateTime VerifiedUntil { get; set; } = DateTime.MinValue;
    }
}
