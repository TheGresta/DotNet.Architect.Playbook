using System.Collections.Frozen;

namespace Playbook.Exceptions.Constants;

/// <summary>
/// Maintains security-related metadata and collections used for data protection tasks,
/// such as sanitizing logs or filtering sensitive response fields.
/// </summary>
public static class SecurityConstants
{
    /// <summary>
    /// A high-performance, immutable set of keys identifying sensitive information.
    /// Used primarily to redact PII or credentials during logging or serialization.
    /// </summary>
    // Initialized as a FrozenSet for O(1) lookup performance and thread-safe read-only access.
    public static readonly FrozenSet<string> SensitiveKeys =
        new[] { "Password", "ConfirmPassword", "PasswordHash", "CreditCard", "CVV", "CardNumber", "SSN", "Token", "AccessToken", "RefreshToken", "Secret" }
        .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
}
