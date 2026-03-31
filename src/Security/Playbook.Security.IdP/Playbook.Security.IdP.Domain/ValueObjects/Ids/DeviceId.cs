using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.ValueObjects.Ids;

public record DeviceId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public static DeviceId New() => new(Guid.NewGuid());
}
