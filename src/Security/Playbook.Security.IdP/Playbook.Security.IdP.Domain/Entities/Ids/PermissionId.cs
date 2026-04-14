using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.Entities.Ids;

public sealed record PermissionId : StronglyTypedId<Guid>
{
    private PermissionId(Guid value) : base(value) { }
    public static PermissionId New() => new(Guid.NewGuid());
    public static PermissionId FromGuid(Guid value) => new(value);
}
