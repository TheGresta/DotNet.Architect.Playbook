using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Entities;

public sealed class Permission : Entity<PermissionId>
{
    // The unique string code (e.g., "identity.users.read")
    public string Name { get; private set; } = string.Empty;

    // Friendly description for the UI
    public string Description { get; private set; } = string.Empty;

    private Permission() { } // EF Core

    public Permission(PermissionId id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }
}
