using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.Entities.Base;

/// <summary>
/// Base abstraction for all domain entities, enforcing strongly-typed identifiers.
/// </summary>
/// <typeparam name="TId">The specific strongly-typed ID for the entity.</typeparam>
public abstract class Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets the unique identifier for the entity.
    /// </summary>
    public TId Id { get; init; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether the entity is logically active.
    /// Supports "Soft Delete" patterns at the domain level.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Infrastructure for Domain Events (Gold Standard for CQRS)
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    // Standard equality override for entities based on ID
    public override bool Equals(object? obj) =>
        obj is Entity<TId> entity && Id.Equals(entity.Id);

    public override int GetHashCode() => Id.GetHashCode();
}
