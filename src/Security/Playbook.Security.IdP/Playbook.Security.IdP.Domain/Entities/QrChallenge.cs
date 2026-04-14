using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Events;
using Playbook.Security.IdP.Domain.Exceptions;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Entities;

/// <summary>
/// Models a QR-code based cross-device login handshake.
///
/// Design decisions:
/// - SecretCodeHash: The challenge secret is stored as a SHA-256 hash, not plaintext.
///   The hash is used for lookup; the plaintext is embedded in the QR code only.
///   This prevents a DB breach from yielding live challenge codes.
/// - BindingToken: A secondary random token bound to the requesting browser session
///   (stored in a HttpOnly cookie). Prevents QR relay attacks where an attacker
///   forwards a QR code to a victim and then uses the authorized session themselves.
/// - ConsumedBySessionId: Closes the loop between the QR challenge and the
///   AuthenticationSession that was created from it.
/// - TTL is 120 seconds, enforced at the domain level with a constant.
/// </summary>
public sealed class QrChallenge : Entity<QrChallengeId>
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromSeconds(120);

    // ── Handshake Metadata ────────────────────────────────────────────────────
    /// <summary>
    /// SHA-256 hash of the secret code embedded in the QR image.
    /// Used for secure lookup — the plaintext code is never stored.
    /// </summary>
    public string SecretCodeHash { get; private set; } = string.Empty;

    /// <summary>
    /// HMAC token bound to the initiating browser session (stored in HttpOnly cookie).
    /// The consuming endpoint must validate this to prevent relay attacks.
    /// </summary>
    public string BindingToken { get; private set; } = string.Empty;

    /// <summary>SignalR / WebSocket connection ID for real-time push to the waiting browser.</summary>
    public string? ConnectionId { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    // ── State Machine ─────────────────────────────────────────────────────────
    public ChallengeStatus Status { get; private set; }
    public UserId? AuthorizedBy { get; private set; }
    public DateTime? AuthorizedAt { get; private set; }

    /// <summary>The AuthenticationSession created after this challenge was consumed.</summary>
    public AuthenticationSessionId? ConsumedBySessionId { get; private set; }

    // ── Contextual Security ───────────────────────────────────────────────────
    public IpAddress RequestingIp { get; private set; } = null!;
    public DeviceMetadata? RequestingDeviceMetadata { get; private set; }

    public enum ChallengeStatus { Pending, Authorized, Consumed, Expired, Revoked }

    // ── ORM constructor ───────────────────────────────────────────────────────
    private QrChallenge() { }

    // ── Private constructor ───────────────────────────────────────────────────
    private QrChallenge(
        QrChallengeId id,
        string secretCodeHash,
        string bindingToken,
        IpAddress requestingIp,
        DeviceMetadata? metadata,
        TimeSpan ttl)
    {
        Id = id;
        SecretCodeHash = secretCodeHash;
        BindingToken = bindingToken;
        RequestingIp = requestingIp;
        RequestingDeviceMetadata = metadata;
        Status = ChallengeStatus.Pending;
        ExpiresAt = DateTime.UtcNow.Add(ttl);

        // Note: domain event carries the hash, not the plaintext.
        // The plaintext is returned from the factory and must be discarded after QR generation.
        AddDomainEvent(new QrChallengeCreatedEvent(Id, secretCodeHash));
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new QR challenge.
    /// Returns both the entity (to be persisted) and the plaintext secret
    /// (to be embedded in the QR code, then discarded).
    /// </summary>
    public static (QrChallenge Challenge, string PlaintextSecret, string PlaintextBindingToken)
        Create(string ipAddress, DeviceMetadata? metadata)
    {
        var plaintextSecret = ChallengeSecret.Generate();
        var plaintextBindingToken = GenerateBindingToken();

        // Hash both values before storing.
        var secretHash = ComputeSha256(plaintextSecret.Value);
        var bindingHash = ComputeSha256(plaintextBindingToken);

        var challenge = new QrChallenge(
            QrChallengeId.New(),
            secretHash,
            bindingHash,
            IpAddress.Create(ipAddress),
            metadata,
            DefaultTtl);

        return (challenge, plaintextSecret.Value, plaintextBindingToken);
    }

    // ── Domain Behaviours ─────────────────────────────────────────────────────

    public void Authorize(UserId authorizerId)
    {
        EnsureNotExpired();

        if (Status != ChallengeStatus.Pending)
            throw new DomainException(
                $"Challenge cannot be authorized from '{Status}' state.", "INVALID_STATE");

        AuthorizedBy = authorizerId;
        AuthorizedAt = DateTime.UtcNow;
        Status = ChallengeStatus.Authorized;

        AddDomainEvent(new QrChallengeAuthorizedEvent(Id, AuthorizedBy));
    }

    public void Consume(AuthenticationSessionId sessionId)
    {
        EnsureNotExpired();

        if (Status != ChallengeStatus.Authorized)
            throw new DomainException(
                "Challenge must be authorized before it can be consumed.", "NOT_AUTHORIZED");

        Status = ChallengeStatus.Consumed;
        ConsumedBySessionId = sessionId;

        AddDomainEvent(new QrChallengeConsumedEvent(Id, AuthorizedBy!));
    }

    public void Revoke(string reason)
    {
        // Terminal states cannot be revoked.
        if (Status is ChallengeStatus.Consumed or ChallengeStatus.Expired) return;

        Status = ChallengeStatus.Revoked;
        AddDomainEvent(new QrChallengeRevokedEvent(Id, reason));
    }

    public void UpdateConnectionId(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId)) return;
        ConnectionId = connectionId;
    }

    /// <summary>
    /// Validates the binding token presented by the browser against the stored hash.
    /// Must be called before authorizing the challenge.
    /// </summary>
    public bool ValidateBindingToken(string plaintextBindingToken)
    {
        var hash = ComputeSha256(plaintextBindingToken);
        return string.Equals(BindingToken, hash, StringComparison.OrdinalIgnoreCase);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void EnsureNotExpired()
    {
        if (DateTime.UtcNow > ExpiresAt)
        {
            Status = ChallengeStatus.Expired;
            throw new DomainException(
                "The QR code has expired. Please refresh and scan again.", "QR_EXPIRED");
        }
    }

    private static string GenerateBindingToken() =>
        Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    private static string ComputeSha256(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        return Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
