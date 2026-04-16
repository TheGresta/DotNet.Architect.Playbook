using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Exceptions;

public sealed class MfaRequiredException : DomainException
{
    public UserId UserId { get; }
    public string CorrelationId { get; }

    public MfaRequiredException(UserId userId, string correlationId)
        : base("Multi-Factor Authentication is required to complete this request.", "MFA_REQUIRED")
    {
        UserId = userId;
        CorrelationId = correlationId;
    }
}
