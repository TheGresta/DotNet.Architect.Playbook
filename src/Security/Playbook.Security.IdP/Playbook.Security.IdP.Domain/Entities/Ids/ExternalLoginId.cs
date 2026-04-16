using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.Entities.Ids;

public sealed record ExternalLoginId : StronglyTypedId<Guid>
{
    private ExternalLoginId(Guid value) : base(value) { }
    public static ExternalLoginId New() => new(Guid.NewGuid());
    public static ExternalLoginId FromGuid(Guid value) => new(value);
}
