using System.Collections.Frozen;

namespace Playbook.Exceptions.Constants;

public static class SecurityConstants
{
    public static readonly FrozenSet<string> SensitiveKeys =
        new[] { "Password", "ConfirmPassword", "PasswordHash", "CreditCard", "CVV", "CardNumber", "SSN", "Token", "AccessToken", "RefreshToken", "Secret" }
        .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
}
