namespace Playbook.Exceptions.Constants;

public static class SecurityConstants
{
    public static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "ConfirmPassword", "PasswordHash",
        "CreditCard", "CVV", "CardNumber", "SSN",
        "Token", "AccessToken", "RefreshToken", "Secret"
    };
}
