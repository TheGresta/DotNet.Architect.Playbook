using Playbook.Security.IdP.Domain.Aggregates.AuditAggregate;

namespace Playbook.Security.IdP.Application.Abstractions.Data;

public interface IAuditRepository
{
    /// <summary>
    /// Persists an immutable audit record to the store.
    /// </summary>
    Task AddAsync(AuditLog log, CancellationToken cancellationToken = default);
}
