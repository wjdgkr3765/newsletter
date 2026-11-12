using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace NewsletterApp.Services;

// 로그인 세션을 서버 메모리에 둔다. 쿠키에는 세션 키만 있고, 로그아웃하면 바로 무효가 된다.
public sealed class MemoryTicketStore(IMemoryCache cache) : ITicketStore
{
    private const string KeyPrefix = "session:";
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(60);

    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        string key = Guid.NewGuid().ToString("N");
        Set(key, ticket);
        return Task.FromResult(key);
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        Set(key, ticket);
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        cache.TryGetValue(KeyPrefix + key, out AuthenticationTicket? ticket);
        return Task.FromResult(ticket);
    }

    public Task RemoveAsync(string key)
    {
        cache.Remove(KeyPrefix + key);
        return Task.CompletedTask;
    }

    private void Set(string key, AuthenticationTicket ticket)
    {
        cache.Set(KeyPrefix + key, ticket, new MemoryCacheEntryOptions { SlidingExpiration = SlidingExpiration });
    }
}
