using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Entities;

public sealed class Role : AuditableEntity<RoleId>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private readonly List<Permission> _permissions = [];
    public IReadOnlyCollection<Permission> Permissions => _permissions.AsReadOnly();

    private Role() { } // EF Core

    public Role(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public void AddPermission(Permission permission)
    {
        if (_permissions.Any(p => p.Id == permission.Id)) return;
        _permissions.Add(permission);
    }

    public void RemovePermission(PermissionId permissionId)
    {
        var permission = _permissions.FirstOrDefault(p => p.Id == permissionId);
        if (permission is not null) _permissions.Remove(permission);
    }
}
