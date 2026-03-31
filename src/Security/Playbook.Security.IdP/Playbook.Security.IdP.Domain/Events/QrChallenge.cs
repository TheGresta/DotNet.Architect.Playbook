using Playbook.Security.IdP.Domain.ValueObjects.Ids;

namespace Playbook.Security.IdP.Domain.Events;

public record QrChallengeCreatedEvent(QrChallengeId Id, string SecretCode) : IDomainEvent;
public record QrChallengeAuthorizedEvent(QrChallengeId Id, UserId AuthorizedBy) : IDomainEvent;
public record QrChallengeConsumedEvent(QrChallengeId Id, UserId UserId) : IDomainEvent;
public record QrChallengeRevokedEvent(QrChallengeId Id, string Reason) : IDomainEvent;
