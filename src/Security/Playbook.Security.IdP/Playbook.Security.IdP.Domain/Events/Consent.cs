using Playbook.Security.IdP.Domain.ValueObjects.Ids;

namespace Playbook.Security.IdP.Domain.Events;

internal class Consent
public record UserConsentGrantedEvent(UserId UserId, string ClientId, List<string> Scopes) : IDomainEvent;
public record UserConsentRevokedEvent(UserId UserId, string ClientId) : IDomainEvent;
