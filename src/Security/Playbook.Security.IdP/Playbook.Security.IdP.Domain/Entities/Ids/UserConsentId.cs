using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.Entities.Ids;

public sealed record UserConsentId : StronglyTypedId<Guid>
{
    private UserConsentId(Guid value) : base(value) { }
    public static UserConsentId New() => new(Guid.NewGuid());
    public static UserConsentId FromGuid(Guid value) => new(value);
}
