using System.Linq.Expressions;

using Playbook.Security.IdP.Domain.Common;

using Playbook.Security.IdP.Domain.Entities;

namespace Playbook.Security.IdP.Domain.Specifications;

/// <summary>
/// Specification to identify AuthenticationSessions that have passed their TTL 
/// or have been manually moved to an expired state.
/// </summary>
public sealed class ExpiredSessionSpecification : ISpecification<AuthenticationSession>
{
    private readonly DateTime _utcNow;

    public ExpiredSessionSpecification(DateTime? utcNow = null)
    {
        _utcNow = utcNow ?? DateTime.UtcNow;
    }

    public Expression<Func<AuthenticationSession, bool>> ToExpression()
    {
        return session =>
            session.Status == AuthenticationSession.AuthStep.Expired ||
            session.ExpiresAt <= _utcNow;
    }

    public bool IsSatisfiedBy(AuthenticationSession entity)
    {
        return entity.Status == AuthenticationSession.AuthStep.Expired ||
               entity.ExpiresAt <= _utcNow;
    }
}
