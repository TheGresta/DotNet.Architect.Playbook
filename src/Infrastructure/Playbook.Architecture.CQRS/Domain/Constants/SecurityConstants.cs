using System.Collections.Frozen;

namespace Playbook.Architecture.CQRS.Domain.Constants;

public static class SecurityConstants
{
    // Initialized as a FrozenSet for O(1) lookup performance and thread-safe read-only access.
    public static readonly FrozenSet<string> SensitiveKeys =
        new[] { "Password", "ConfirmPassword", "PasswordHash", "CreditCard", "CVV", "CardNumber", "SSN", "Token", "AccessToken", "RefreshToken", "Secret" }
        .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
}
