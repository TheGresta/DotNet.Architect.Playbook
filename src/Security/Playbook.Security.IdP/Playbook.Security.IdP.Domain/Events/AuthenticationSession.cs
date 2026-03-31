using Playbook.Security.IdP.Domain.ValueObjects.Ids;

namespace Playbook.Security.IdP.Domain.Events;

public record AuthSessionStartedEvent(AuthenticationSessionId SessionId, UserId UserId, string CorrelationId) : IDomainEvent;
public record MfaChallengedEvent(AuthenticationSessionId SessionId, UserId UserId) : IDomainEvent;
public record AuthSessionVerifiedEvent(AuthenticationSessionId SessionId, UserId UserId, DeviceId DeviceId) : IDomainEvent;
public record AuthSessionAbortedEvent(AuthenticationSessionId SessionId, UserId UserId, string Reason) : IDomainEvent;
