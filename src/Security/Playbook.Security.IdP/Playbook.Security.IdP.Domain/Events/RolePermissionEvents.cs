using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Events;

// ── Role & Permission Events ──────────────────────────────────────────────────

public record RolePermissionAddedEvent(RoleId RoleId, PermissionId PermissionId) : DomainEvent;
public record RolePermissionRemovedEvent(RoleId RoleId, PermissionId PermissionId) : DomainEvent;
