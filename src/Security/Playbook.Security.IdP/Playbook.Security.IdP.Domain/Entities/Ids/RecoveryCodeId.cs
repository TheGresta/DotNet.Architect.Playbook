using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.Entities.Ids;

public sealed record RecoveryCodeId : StronglyTypedId<Guid>
{
    // Pass the value to the base constructor for validation
    private RecoveryCodeId(Guid value) : base(value) { }

    public static RecoveryCodeId New() => new(Guid.NewGuid());
    public static RecoveryCodeId FromGuid(Guid value) => new(value);
}
