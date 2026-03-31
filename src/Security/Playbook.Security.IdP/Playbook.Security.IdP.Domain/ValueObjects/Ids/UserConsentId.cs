using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.ValueObjects.Ids;

public record UserConsentId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public static UserConsentId New() => new(Guid.NewGuid());
}
