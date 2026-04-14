namespace Playbook.Security.IdP.Application.Abstractions.Security;

public interface IIdempotencyService
{
    Task<bool> TryReserveAsync(Guid requestId, TimeSpan expiry);
    Task<TResponse?> GetResponseAsync<TResponse>(Guid requestId);
    Task CompleteAsync<TResponse>(Guid requestId, TResponse response, TimeSpan expiry);
}
