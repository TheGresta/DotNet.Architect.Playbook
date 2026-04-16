using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Events;

// ── QR Challenge Events ───────────────────────────────────────────────────────

public record QrChallengeCreatedEvent(QrChallengeId Id, string SecretCodeHash) : DomainEvent;
public record QrChallengeAuthorizedEvent(QrChallengeId Id, UserId AuthorizedBy) : DomainEvent;
public record QrChallengeConsumedEvent(QrChallengeId Id, UserId UserId) : DomainEvent;
public record QrChallengeRevokedEvent(QrChallengeId Id, string Reason) : DomainEvent;
