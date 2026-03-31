using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.ValueObjects.Ids;

public record UserId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public static UserId Empty => new(Guid.Empty);
}
