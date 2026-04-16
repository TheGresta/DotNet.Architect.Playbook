namespace Playbook.Security.IdP.Application.Abstractions.Messaging;

public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(CancellationToken ct = default);
}
