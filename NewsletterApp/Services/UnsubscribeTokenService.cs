using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace NewsletterApp.Services;

public sealed record UnsubscribeTarget(long UserId, long GubunId);

// 수신 거부 링크 토큰. Data Protection으로 "회원 id:종류 id"를 보호한다. 만료 없음.
public interface IUnsubscribeTokenService
{
    string Create(long userId, long gubunId);

    UnsubscribeTarget? Read(string token);
}

public sealed class UnsubscribeTokenService(IDataProtectionProvider provider) : IUnsubscribeTokenService
{
    private const string Purpose = "Newsletter.Unsubscribe.v1";
    private readonly IDataProtector _protector = provider.CreateProtector(Purpose);

    public string Create(long userId, long gubunId) => _protector.Protect($"{userId}:{gubunId}");

    public UnsubscribeTarget? Read(string token)
    {
        string value;
        try
        {
            value = _protector.Unprotect(token);
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            return null;
        }
        string[] parts = value.Split(':');
        if (parts.Length != 2
            || !long.TryParse(parts[0], out long userId)
            || !long.TryParse(parts[1], out long gubunId))
        {
            return null;
        }
        return new UnsubscribeTarget(userId, gubunId);
    }
}
