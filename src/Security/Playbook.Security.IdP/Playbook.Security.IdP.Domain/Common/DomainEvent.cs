namespace Playbook.Security.IdP.Domain.Common;

/// <summary>
/// Base record to reduce boilerplate for every event.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
