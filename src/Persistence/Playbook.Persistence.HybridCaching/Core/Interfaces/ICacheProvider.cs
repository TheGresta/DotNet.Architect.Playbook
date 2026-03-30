namespace Playbook.Persistence.HybridCaching.Core.Interfaces;

public interface ICacheProvider
{
    Task<T> GetOrAddAsync<T>(
        Func<CancellationToken, ValueTask<T>> factory,
        string? identifier = null,
        CancellationToken ct = default)
        where T : class;

    Task NotifyInvalidationAsync<T>(CancellationToken ct) where T : class;
}
