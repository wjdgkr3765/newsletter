using System.Collections.Concurrent;

namespace NewsletterApp.Services;

// 종류별 발송 잠금. 같은 종류를 동시에 보내지 않는다. (자동 발송, 직접 발송, 다시 보내기 공통)
public interface ISendLockService
{
    // 잠금을 잡으면 풀 때 Dispose할 객체, 이미 잡혀 있으면 null
    IDisposable? TryAcquire(long gubunId);

    bool IsLocked(long gubunId);
}

public sealed class SendLockService : ISendLockService
{
    private readonly ConcurrentDictionary<long, byte> _locked = new();

    public IDisposable? TryAcquire(long gubunId) =>
        _locked.TryAdd(gubunId, 0) ? new Releaser(this, gubunId) : null;

    public bool IsLocked(long gubunId) => _locked.ContainsKey(gubunId);

    private sealed class Releaser(SendLockService owner, long gubunId) : IDisposable
    {
        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            owner._locked.TryRemove(gubunId, out _);
        }
    }
}
