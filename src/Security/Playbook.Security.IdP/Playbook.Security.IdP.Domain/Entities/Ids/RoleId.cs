using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.Entities.Ids;

public sealed record RoleId : StronglyTypedId<Guid>
{
    private RoleId(Guid value) : base(value) { }
    public static RoleId New() => new(Guid.NewGuid());
    public static RoleId FromGuid(Guid value) => new(value);
}
