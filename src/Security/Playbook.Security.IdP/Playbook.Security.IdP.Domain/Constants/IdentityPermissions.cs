using Playbook.Security.IdP.Domain.Aggregates.PermissionAggregate;

namespace Playbook.Security.IdP.Domain.Constants;

public static class IdentityPermissions
{
    public static readonly PermissionCode UserRead = PermissionCode.Create("idp", "users", "read");
    public static readonly PermissionCode UserWrite = PermissionCode.Create("idp", "users", "write");
    public static readonly PermissionCode RoleAssign = PermissionCode.Create("idp", "roles", "assign");

    public static IReadOnlyList<PermissionCode> All() => [UserRead, UserWrite, RoleAssign];
}
