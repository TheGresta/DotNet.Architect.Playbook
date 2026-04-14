using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.Entities;

/// <summary>
/// Represents a single claim asserted about a user.
///
/// Design decisions:
/// - IsEncrypted flag allows high-sensitivity claims (SSN, DOB) to be stored
///   as AES-256 ciphertext. The Application layer handles encrypt/decrypt —
///   the domain just tracks the flag.
/// - Source distinguishes local claims (set by admins) from federated claims
///   (asserted by an external IdP) and computed claims (derived by business logic).
/// - ExpiresAt supports time-bound attribute assertions (e.g. a verified age claim
///   that expires annually).
/// - Issuer captures the authority that asserted this claim (for multi-IdP setups).
/// </summary>
public sealed class UserClaim : Entity<UserClaimId>
{
    public UserId UserId { get; private set; } = null!;
    public string Type { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public ValueTypes ValueType { get; private set; } = ValueTypes.String;
    public ClaimSource Source { get; private set; } = ClaimSource.Local;

    /// <summary>
    /// The authority that issued the claim (e.g., "https://idp.example.com",
    /// "google", "admin"). Null for system-generated claims.
    /// </summary>
    public string? Issuer { get; private set; }

    /// <summary>True when <see cref="Value"/> contains AES-256 ciphertext.</summary>
    public bool IsEncrypted { get; private set; }

    public DateTime IssuedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public enum ValueTypes { String, Int, Long, Decimal, Boolean, Json, DateTime }
    public enum ClaimSource { Local, Federated, Computed, System }

    // ORM constructor
    private UserClaim() { }

    public UserClaim(
        UserId userId,
        string type,
        string value,
        ValueTypes valueType = ValueTypes.String,
        ClaimSource source = ClaimSource.Local,
        string? issuer = null,
        bool isEncrypted = false,
        DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new DomainException("Claim type cannot be empty.", "INVALID_CLAIM_TYPE");

        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Claim value cannot be empty.", "INVALID_CLAIM_VALUE");

        if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
            throw new DomainException("Claim expiry must be in the future.", "INVALID_CLAIM_EXPIRY");

        Id = UserClaimId.New();
        UserId = userId;
        Type = type.ToLowerInvariant().Trim();
        Value = value;
        ValueType = valueType;
        Source = source;
        Issuer = issuer?.Trim();
        IsEncrypted = isEncrypted;
        IssuedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    public bool IsExpired() =>
        ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    public bool StillActive() => !IsExpired();
}
