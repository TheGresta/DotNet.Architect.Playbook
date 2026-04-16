using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Events;

// ── Authentication Session Events ─────────────────────────────────────────────

public record AuthSessionStartedEvent(AuthenticationSessionId SessionId, UserId UserId, string CorrelationId) : DomainEvent;
public record MfaChallengedEvent(AuthenticationSessionId SessionId, UserId UserId) : DomainEvent;
public record AuthSessionVerifiedEvent(AuthenticationSessionId SessionId, UserId UserId, DeviceId DeviceId) : DomainEvent;
public record AuthSessionAbortedEvent(AuthenticationSessionId SessionId, UserId UserId, string Reason) : DomainEvent;
