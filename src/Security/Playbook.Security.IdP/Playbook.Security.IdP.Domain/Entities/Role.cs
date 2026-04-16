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

    private Role(string name, string description, int priority, bool isSystemRole, DateTime createdAt, UserId createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Role name cannot be empty.", "INVALID_ROLE_NAME");

        Id = RoleId.New();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Priority = priority;
        IsSystemRole = isSystemRole;
    }

    public static Role Create(
        string name,
        string description,
        DateTimeOffset utcNow,
        UserId createdBy,
        int? priority = null,
        bool? isSystemRole = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Role name cannot be empty.", "INVALID_ROLE_NAME");

        string roleName = name.Trim();
        string roleDescription = description?.Trim() ?? string.Empty;
        int rolePriority = priority ?? 0;
        bool roleIsSystemRole = isSystemRole ?? false;
        var createdAt = utcNow.UtcDateTime;

        return new Role(
            roleName,
            roleDescription,
            rolePriority,
            roleIsSystemRole,
            createdAt,
            createdBy);
    }

    public void UpdateDescription(string description, DateTimeOffset utcNow, UserId updatedBy)
    {
        Description = description?.Trim() ?? string.Empty;
        SetUpdateMetadata(utcNow, updatedBy);
    }

    public void SetPriority(int priority, DateTimeOffset utcNow, UserId updatedBy)
    {
        Priority = priority;
        SetUpdateMetadata(utcNow, updatedBy);
    }

    public void AddPermission(Permission permission, DateTimeOffset utcNow, UserId updatedBy)
    {
        if (_permissions.Any(p => p.Id == permission.Id)) return;
        _permissions.Add(permission);
        SetUpdateMetadata(utcNow, updatedBy);

        AddDomainEvent(new RolePermissionAddedEvent(Id, permission.Id));
    }

    public void RemovePermission(PermissionId permissionId, DateTimeOffset utcNow, UserId updatedBy)
    {
        var permission = _permissions.FirstOrDefault(p => p.Id == permissionId);
        if (permission is null) return;

        _permissions.Remove(permission);
        SetUpdateMetadata(utcNow, updatedBy);

        AddDomainEvent(new RolePermissionRemovedEvent(Id, permissionId));
    }

    public void AddConflict(RoleId conflictingRoleId)
    {
        if (conflictingRoleId == Id)
            throw new DomainException("A role cannot conflict with itself.", "SELF_CONFLICT");

        if (!_conflictsWiths.Contains(conflictingRoleId))
            _conflictsWiths.Add(conflictingRoleId);
    }

    /// <summary>
    /// Registers a one-directional conflict edge on this role.
    /// Call via <see cref="RoleConflictService"/> only, which ensures symmetry.
    /// </summary>
    internal void RegisterConflict(RoleId conflictingRoleId, DateTimeOffset utcNow, UserId updatedBy)
    {
        if (conflictingRoleId == Id)
            throw new DomainException("A role cannot conflict with itself.", "SELF_CONFLICT");

        if (_conflictsWiths.Contains(conflictingRoleId)) return;

        _conflictsWiths.Add(conflictingRoleId);
        SetUpdateMetadata(utcNow, updatedBy);
        AddDomainEvent(new RoleConflictAddedEvent(Id, conflictingRoleId));
    }

    /// <summary>
    /// Removes a one-directional conflict edge from this role.
    /// Call via <see cref="RoleConflictService"/> only.
    /// </summary>
    internal void DeregisterConflict(RoleId conflictingRoleId, DateTimeOffset utcNow, UserId updatedBy)
    {
        if (!_conflictsWiths.Remove(conflictingRoleId)) return;

        SetUpdateMetadata(utcNow, updatedBy);
        AddDomainEvent(new RoleConflictRemovedEvent(Id, conflictingRoleId));
    }

    public void RemoveConflict(RoleId conflictingRoleId) =>
        _conflictsWiths.Remove(conflictingRoleId);

    public bool ConflictsWith(RoleId roleId) =>
        _conflictsWiths.Contains(roleId);

    /// <summary>Returns all permission codes this role grants, for cache flattening.</summary>
    public IEnumerable<string> GetFlattenedPermissionCodes() =>
        _permissions.Select(p => p.Name);
}
