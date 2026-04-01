using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.Entities.Ids;

public sealed record AuthenticationSessionId : StronglyTypedId<Guid>
{
    private AuthenticationSessionId(Guid value) : base(value) { }
    public static AuthenticationSessionId New() => new(Guid.NewGuid());
    public static AuthenticationSessionId FromGuid(Guid value) => new(value);
}
