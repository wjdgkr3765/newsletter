using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NewsletterApp.Data;
using NewsletterApp.Data.Entities;
using NewsletterApp.Services;
using NewsletterApp.Services.Contents;

namespace NewsletterApp.Tests;

// 테스트용 시계. 시각을 직접 정한다.
public sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 11, 12, 7, 0, 0, DateTimeKind.Utc);

    public DateTime KstNow => KstClock.ToKst(UtcNow);

    public DateOnly KstToday => DateOnly.FromDateTime(KstNow);

    public void Advance(TimeSpan time) => UtcNow = UtcNow.Add(time);
}

// 테스트용 메일. 실제로 보내지 않고 기록만 한다. FailAddresses에 있는 주소는 실패한다.
public sealed class FakeMailService : IMailService
{
    public List<(string To, string Subject, string Html)> Sent { get; } = [];

    public HashSet<string> FailAddresses { get; } = [];

    public bool IsConnectFailing { get; set; }

    public Task<bool> TrySendAsync(string to, string subject, string html, CancellationToken cancellationToken)
    {
        if (IsConnectFailing || FailAddresses.Contains(to))
        {
            return Task.FromResult(false);
        }
        Sent.Add((to, subject, html));
        return Task.FromResult(true);
    }

    public Task<IMailBatch> OpenBatchAsync(CancellationToken cancellationToken)
    {
        if (IsConnectFailing)
        {
            throw new InvalidOperationException("연결 실패");
        }
        return Task.FromResult<IMailBatch>(new Batch(this));
    }

    private sealed class Batch(FakeMailService owner) : IMailBatch
    {
        public Task<bool> TrySendAsync(string to, string subject, string html, CancellationToken cancellationToken) =>
            owner.TrySendAsync(to, subject, html, cancellationToken);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

// 외부 데이터를 받지 못하는 본문 생성기
public sealed class FailingContentBuilder(string gubunName) : INewsletterContentBuilder
{
    public string GubunName { get; } = gubunName;

    public Task<string?> BuildAsync(string gubunName, DateOnly kstDay, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(null);
}

// 테스트마다 새 SQLite 메모리 DB와 서비스를 만든다.
public sealed class TestScope : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestScope()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        Db = CreateDb();
        Db.Database.EnsureCreated();
        Cache = new MemoryCache(new MemoryCacheOptions());
        Codes = new VerificationCodeService(Cache, Clock);
        LoginAttempts = new LoginAttemptService(Cache, Clock);
        Tokens = new UnsubscribeTokenService(new EphemeralDataProtectionProvider());
        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["App:BaseUrl"] = "https://localhost:7105" })
            .Build();
    }

    public AppDbContext Db { get; }

    public FakeClock Clock { get; } = new();

    public FakeMailService Mail { get; } = new();

    public MemoryCache Cache { get; }

    public VerificationCodeService Codes { get; }

    public LoginAttemptService LoginAttempts { get; }

    public UnsubscribeTokenService Tokens { get; }

    public FakePasswordHasher Hasher { get; } = new();

    public IConfiguration Configuration { get; }

    public SendLockService SendLocks { get; } = new();

    public List<INewsletterContentBuilder> ContentBuilders { get; } = [];

    // 같은 DB를 쓰는 새 DbContext (변경 추적 없이 확인할 때)
    public AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    public AuthService AuthService() => new(Db, Hasher, Codes, LoginAttempts, Mail, Clock);

    public MeService MeService() => new(Db, Hasher, Codes, Mail);

    public SubscriptionService SubscriptionService() => new(Db, Tokens, Clock);

    public AdminUserService AdminUserService() => new(Db);

    public GubunService GubunService() => new(Db);

    public NewsletterQueryService QueryService() => new(Db);

    public NewsletterSendService SendService() => new(
        Db, Mail, Tokens, SendLocks,
        new ContentBuilderResolver(ContentBuilders, new DefaultContentBuilder()),
        Clock, Configuration, NullLogger<NewsletterSendService>.Instance);

    public User AddUser(string loginId, string password = "password1!", bool isAdmin = false, bool isSuspended = false)
    {
        var user = new User
        {
            LoginId = loginId,
            PasswordHash = Hasher.Hash(password),
            Email = $"{loginId}@example.com",
            Name = $"이름{loginId}",
            IsAdmin = isAdmin,
            IsSuspended = isSuspended,
            CreateDate = Clock.UtcNow
        };
        Db.Users.Add(user);
        Db.SaveChanges();
        return user;
    }

    public Gubun AddGubun(string name, TimeOnly? sendTime = null, bool isUse = false)
    {
        var gubun = new Gubun { Name = name };
        if (sendTime is not null)
        {
            gubun.Config = new NewsletterConfig { SendTime = sendTime.Value, IsUse = isUse };
        }
        Db.Gubuns.Add(gubun);
        Db.SaveChanges();
        return gubun;
    }

    public void Subscribe(User user, Gubun gubun)
    {
        Db.Subscriptions.Add(new Subscription { UserId = user.Id, GubunId = gubun.Id, CreateDate = Clock.UtcNow });
        Db.SaveChanges();
    }

    public void Dispose()
    {
        Db.Dispose();
        Cache.Dispose();
        _connection.Dispose();
    }
}

// BCrypt는 느리므로 테스트에서는 단순 해시를 쓴다. (BCrypt 자체는 PasswordHasherTests에서 확인)
public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hash:" + password;

    public bool Verify(string password, string hash) => hash == "hash:" + password;
}
