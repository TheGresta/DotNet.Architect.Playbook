namespace Playbook.Security.IdP.Domain.Common;

/// <summary>
/// Pure Domain abstraction. No MediatR here.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
