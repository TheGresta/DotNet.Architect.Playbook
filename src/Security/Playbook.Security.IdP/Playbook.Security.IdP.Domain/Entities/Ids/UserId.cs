using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.Entities.Ids;

public sealed record UserId : StronglyTypedId<Guid>
{
    // Pass the value to the base constructor for validation
    private UserId(Guid value) : base(value) { }

    public static UserId New() => new(Guid.NewGuid());
    public static UserId FromGuid(Guid value) => new(value);
}
