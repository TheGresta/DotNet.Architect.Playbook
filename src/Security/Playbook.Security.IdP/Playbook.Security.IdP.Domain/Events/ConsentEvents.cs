using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Events;

public record UserConsentGrantedEvent(UserId UserId, string ClientId, List<string> Scopes) : DomainEvent;
public record UserConsentRevokedEvent(UserId UserId, string ClientId) : DomainEvent;
