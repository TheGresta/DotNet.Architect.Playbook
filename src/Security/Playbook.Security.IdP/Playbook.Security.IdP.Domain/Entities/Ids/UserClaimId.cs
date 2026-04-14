using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.Entities.Ids;

public sealed record UserClaimId : StronglyTypedId<Guid>
{
    // Pass the value to the base constructor for validation
    private UserClaimId(Guid value) : base(value) { }

    public static UserClaimId New() => new(Guid.NewGuid());
    public static UserClaimId FromGuid(Guid value) => new(value);
}
