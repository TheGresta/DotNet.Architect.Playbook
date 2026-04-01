using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Events;

public record UserCreatedEvent(UserId UserId, string Username, string Email) : DomainEvent;
public record UserLoggedInEvent(UserId UserId, DateTime LoginTime) : DomainEvent;
public record UserLockedOutEvent(UserId UserId, DateTime Until) : DomainEvent;
public record UserPasswordChangedEvent(UserId UserId) : DomainEvent;
public record UserMfaEnabledEvent(UserId UserId) : DomainEvent;
