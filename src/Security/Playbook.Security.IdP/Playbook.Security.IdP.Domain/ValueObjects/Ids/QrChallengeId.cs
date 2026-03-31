using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.ValueObjects.Ids;

public record QrChallengeId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public static QrChallengeId New() => new(Guid.NewGuid());
}
