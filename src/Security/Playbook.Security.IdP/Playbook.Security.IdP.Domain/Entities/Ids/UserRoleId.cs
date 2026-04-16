using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.Entities.Ids;

public sealed record UserRoleId : StronglyTypedId<Guid>
{
    // Pass the value to the base constructor for validation
    private UserRoleId(Guid value) : base(value) { }

    public static UserRoleId New() => new(Guid.NewGuid());
    public static UserRoleId FromGuid(Guid value) => new(value);
}
