using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Events;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.Entities;

/// <summary>
/// The Role Aggregate Root. A named collection of permissions that can be assigned to users.
///
/// Design decisions:
/// - Priority: When a user has multiple roles, the priority determines which role's
///   settings win for conflicting policies (higher number = higher priority).
/// - ConflictsWith: Separation of Duties (SoD) enforcement. A user cannot hold
///   two roles that conflict (e.g. "Approver" and "Requester" for the same resource).
/// - IsSystemRole: System roles (e.g., "SuperAdmin") cannot be deleted or renamed.
/// - MaxAssignments: Optional cap on how many users can hold a role simultaneously
///   (useful for licenced or privileged roles).
/// </summary>
public sealed class Role : AuditableEntity<RoleId>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Priority { get; private set; }
    public bool IsSystemRole { get; private set; }
    public int? MaxAssignments { get; private set; }

    private readonly List<Permission> _permissions = [];
    public IReadOnlyCollection<Permission> Permissions => _permissions.AsReadOnly();

    /// <summary>RoleIds that conflict with this role (SoD constraints).</summary>
    private readonly List<RoleId> _conflictsWiths = [];
    public IReadOnlyCollection<RoleId> ConflictsWiths => _conflictsWiths.AsReadOnly();

    // ORM constructor
    private Role() { }

    public Role(string name, string description, int priority = 0, bool isSystemRole = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Role name cannot be empty.", "INVALID_ROLE_NAME");

        Id = RoleId.New();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Priority = priority;
        IsSystemRole = isSystemRole;
    }

    public void UpdateDescription(string description)
    {
        Description = description?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPriority(int priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddPermission(Permission permission)
    {
        if (_permissions.Any(p => p.Id == permission.Id)) return;
        _permissions.Add(permission);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new RolePermissionAddedEvent(Id, permission.Id));
    }

    public void RemovePermission(PermissionId permissionId)
    {
        var permission = _permissions.FirstOrDefault(p => p.Id == permissionId);
        if (permission is null) return;

        _permissions.Remove(permission);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new RolePermissionRemovedEvent(Id, permissionId));
    }

    public void AddConflict(RoleId conflictingRoleId)
    {
        if (conflictingRoleId == Id)
            throw new DomainException("A role cannot conflict with itself.", "SELF_CONFLICT");

        if (!_conflictsWiths.Contains(conflictingRoleId))
            _conflictsWiths.Add(conflictingRoleId);
    }

    public void RemoveConflict(RoleId conflictingRoleId) =>
        _conflictsWiths.Remove(conflictingRoleId);

    public bool ConflictsWith(RoleId roleId) =>
        _conflictsWiths.Contains(roleId);

    /// <summary>Returns all permission codes this role grants, for cache flattening.</summary>
    public IEnumerable<string> GetFlattenedPermissionCodes() =>
        _permissions.Select(p => p.Name);
}
