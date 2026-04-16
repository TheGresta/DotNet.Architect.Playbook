using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Events;

// ── User Events ───────────────────────────────────────────────────────────────

public record UserCreatedEvent(UserId UserId, string Username, string Email) : DomainEvent;
public record UserLoggedInEvent(UserId UserId, DateTime LoginTime) : DomainEvent;
public record UserLockedOutEvent(UserId UserId, DateTime Until) : DomainEvent;
public record UserUnlockedEvent(UserId UserId) : DomainEvent;
public record UserPasswordChangedEvent(UserId UserId) : DomainEvent;
public record UserEmailConfirmedEvent(UserId UserId, string Email) : DomainEvent;
public record UserEmailChangedEvent(UserId UserId, string NewEmail) : DomainEvent;
public record UserMfaEnabledEvent(UserId UserId) : DomainEvent;
public record UserMfaDisabledEvent(UserId UserId) : DomainEvent;
public record UserRecoveryCodesGeneratedEvent(UserId UserId) : DomainEvent;
public record UserRecoveryCodeConsumedEvent(UserId UserId) : DomainEvent;
public record UserPasskeyRegisteredEvent(UserId UserId, string CredentialId, string DeviceName) : DomainEvent;
public record UserPasskeyRemovedEvent(UserId UserId, string CredentialId) : DomainEvent;
public record UserExternalLoginLinkedEvent(UserId UserId, string Provider, string ProviderSubjectId) : DomainEvent;
public record UserSuspendedEvent(UserId UserId, string Reason) : DomainEvent;
public record UserReinstatedEvent(UserId UserId) : DomainEvent;
public record UserRoleAssignedEvent(UserId UserId, RoleId RoleId, UserId GrantedBy) : DomainEvent;
public record UserRoleRevokedEvent(UserId UserId, RoleId RoleId) : DomainEvent;
