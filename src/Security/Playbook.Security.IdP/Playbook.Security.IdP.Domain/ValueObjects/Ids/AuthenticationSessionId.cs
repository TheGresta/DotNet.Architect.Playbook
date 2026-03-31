using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.ValueObjects.Ids;

public record AuthenticationSessionId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public static AuthenticationSessionId New() => new(Guid.NewGuid());
}
