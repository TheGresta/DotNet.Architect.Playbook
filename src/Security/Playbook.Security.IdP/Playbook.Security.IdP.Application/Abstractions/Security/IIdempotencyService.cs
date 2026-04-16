namespace Playbook.Security.IdP.Application.Abstractions.Security;

public interface IIdempotencyService
{
    Task<bool> TryReserveAsync(Guid requestId, TimeSpan expiry, CancellationToken cancellationToken = default);
    Task ReleaseAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<TResponse?> GetResponseAsync<TResponse>(Guid requestId, CancellationToken cancellationToken = default);
    Task CompleteAsync<TResponse>(Guid requestId, TResponse response, TimeSpan expiry, CancellationToken cancellationToken = default);
}
