using Playbook.Security.IdP.Domain.Models;

namespace Playbook.Security.IdP.Domain.Services;

/// <summary>
/// The core engine for Adaptive Authentication. 
/// Evaluates the context of a login attempt to determine the required level of assurance.
/// </summary>
public interface ISecurityPolicyEngine
{
    /// <summary>
    /// Evaluates the security posture of an authentication request.
    /// </summary>
    /// <param name="user">The user attempting to authenticate.</param>
    /// <param name="context">The contextual data (IP, Device, Location).</param>
    /// <returns>An evaluation result containing the required next steps.</returns>
    Task<PolicyEvaluationResult> EvaluateAsync(
        User user,
        AuthenticationContext context,
        CancellationToken ct = default);
}
