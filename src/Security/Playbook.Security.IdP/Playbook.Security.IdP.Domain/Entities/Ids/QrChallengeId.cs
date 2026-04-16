using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.Entities.Ids;

public sealed record QrChallengeId : StronglyTypedId<Guid>
{
    private QrChallengeId(Guid value) : base(value) { }
    public static QrChallengeId New() => new(Guid.NewGuid());
    public static QrChallengeId FromGuid(Guid value) => new(value);
}
