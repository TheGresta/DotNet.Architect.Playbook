using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.Entities.Ids;

public sealed record UserPasskeyId : StronglyTypedId<Guid>
{
    // Pass the value to the base constructor for validation
    private UserPasskeyId(Guid value) : base(value) { }

    public static UserPasskeyId New() => new(Guid.NewGuid());
    public static UserPasskeyId FromGuid(Guid value) => new(value);
}
