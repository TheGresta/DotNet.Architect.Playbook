using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.Entities.Ids;

public sealed record DeviceId : StronglyTypedId<Guid>
{
    private DeviceId(Guid value) : base(value) { }
    public static DeviceId New() => new(Guid.NewGuid());
    public static DeviceId FromGuid(Guid value) => new(value);
}
