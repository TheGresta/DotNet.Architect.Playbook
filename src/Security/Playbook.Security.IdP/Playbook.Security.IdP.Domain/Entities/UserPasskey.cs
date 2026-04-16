using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.Entities;

/// <summary>
/// Represents a WebAuthn/FIDO2 passkey credential registered by a user.
///
/// Design decisions:
/// - PublicKeyJwk stores the credential public key as JWK (JSON Web Key).
///   The matching private key never leaves the authenticator hardware.
/// - Aaguid identifies the authenticator model, enabling enterprise policy
///   enforcement (e.g., "only YubiKey 5 NFC allowed").
/// - SignCount protects against cloned authenticators: each authentication
///   must present a count strictly greater than the stored value.
/// - LastUsedAt enables age-based revocation policies.
/// </summary>
public sealed class UserPasskey : Entity<UserPasskeyId>
{
    public UserId UserId { get; private set; } = null!;
    public string CredentialId { get; private set; } = string.Empty;

    /// <summary>JWK-encoded public key from the authenticator.</summary>
    public string PublicKeyJwk { get; private set; } = string.Empty;

    /// <summary>Authenticator Attestation GUID — identifies the authenticator model.</summary>
    public string Aaguid { get; private set; } = string.Empty;

    public string DeviceName { get; private set; } = string.Empty;
    public uint SignCount { get; private set; }
    public DateTime RegisteredAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public bool IsBackedUp { get; private set; }

    // ORM constructor
    private UserPasskey() { }

    internal UserPasskey(
        UserId userId,
        string credentialId,
        string publicKeyJwk,
        string aaguid,
        string deviceName,
        bool isBackedUp = false)
    {
        ArgumentNullException.ThrowIfNull(userId);

        if (string.IsNullOrWhiteSpace(credentialId))
            throw new DomainException("Passkey credential ID cannot be empty.", "INVALID_CREDENTIAL_ID");

        if (string.IsNullOrWhiteSpace(publicKeyJwk))
            throw new DomainException("Passkey public key cannot be empty.", "INVALID_PUBLIC_KEY");

        Id = UserPasskeyId.New();
        UserId = userId;
        CredentialId = credentialId;
        PublicKeyJwk = publicKeyJwk;
        Aaguid = aaguid?.Trim() ?? string.Empty;
        DeviceName = deviceName?.Trim() ?? "Unknown authenticator";
        SignCount = 0;
        RegisteredAt = DateTime.UtcNow;
        IsBackedUp = isBackedUp;
    }

    /// <summary>
    /// Updates the sign count after a successful assertion.
    /// Throws if the presented count is not greater than stored (clone detection).
    /// </summary>
    public void RecordSuccessfulAssertion(uint newSignCount)
    {
        // Per WebAuthn spec §6.1: count of 0 means the authenticator doesn't
        // support counters — accept it but don't update.
        if (newSignCount != 0 && newSignCount <= SignCount)
            throw new DomainException(
                "Passkey sign count is not greater than stored value. Possible cloned authenticator.",
                "PASSKEY_CLONE_DETECTED");

        SignCount = newSignCount;
        LastUsedAt = DateTime.UtcNow;
    }
}
