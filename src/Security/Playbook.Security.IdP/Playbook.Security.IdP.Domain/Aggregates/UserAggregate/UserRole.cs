using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Aggregates.UserAggregate;

public sealed class UserRole
{
    public UserId UserId { get; private set; } = null!;
    public RoleId RoleId { get; private set; }

    private UserRole() { }

    internal UserRole(UserId userId, RoleId roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}
