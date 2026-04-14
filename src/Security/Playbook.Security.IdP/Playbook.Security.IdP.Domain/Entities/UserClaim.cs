using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Entities;

public sealed class UserClaim : Entity<UserClaimId>
{
    public UserId UserId { get; private set; } = null!;

    // The Type of claim (e.g., "department", "spending_limit")
    public string Type { get; private set; } = string.Empty;

    // The Value of the claim (e.g., "Engineering", "5000")
    public string Value { get; private set; } = string.Empty;

    // Optional: Claim Value Type (string, int, bool) for better parsing
    public ValueTypes ValueType { get; private set; } = ValueTypes.String;

    public enum ValueTypes { String, Int, Long, Decimal }

    private UserClaim() { }

    public UserClaim(UserId userId, string type, string value, ValueTypes valueType = ValueTypes.String)
    {
        UserId = userId;
        Type = type;
        Value = value;
        ValueType = valueType;
    }
}
