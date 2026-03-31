using System.Linq.Expressions;

using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities;

namespace Playbook.Security.IdP.Domain.Specifications;

/// <summary>
/// Encapsulates the business definition of a 'Safe and Trusted' device.
/// A device is considered safe if it is explicitly trusted, not suspended, 
/// and has been seen within the security grace period.
/// </summary>
public sealed class TrustedDeviceSpecification : ISpecification<UserDevice>
{
    private readonly TimeSpan _maxInactivityPeriod = TimeSpan.FromDays(90);

    public bool IsSatisfiedBy(UserDevice device)
    {
        return device.IsActive &&
               device.IsTrusted &&
               !device.IsSuspended &&
               device.LastUsedAt >= DateTime.UtcNow.Subtract(_maxInactivityPeriod) &&
               device.FailureCount < 3;
    }

    public Expression<Func<UserDevice, bool>> ToExpression()
    {
        var cutoff = DateTime.UtcNow.Subtract(_maxInactivityPeriod);

        return device => device.IsActive &&
                         device.IsTrusted &&
                         !device.IsSuspended &&
                         device.LastUsedAt >= cutoff &&
                         device.FailureCount < 3;
    }
}
