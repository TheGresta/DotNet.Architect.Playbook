using Playbook.Security.IdP.Domain.Entities.Base;
using Playbook.Security.IdP.Domain.Events;
using Playbook.Security.IdP.Domain.ValueObjects;
using Playbook.Security.IdP.Domain.ValueObjects.Ids;

namespace Playbook.Security.IdP.Domain.Entities;

/// <summary>
/// Governs the lifecycle of a cross-device authentication handshake.
/// Facilitates the secure link between an unauthenticated 'Requester' 
/// and an authenticated 'Authorizer'.
/// </summary>
public sealed class QrChallenge : Entity<QrChallengeId>
{
    // --- Handshake Metadata ---
    public string SecretCode { get; private set; } // The high-entropy value encoded in the QR
    public string? ConnectionId { get; private set; } // Optional: For SignalR real-time push
    public DateTime ExpiresAt { get; private set; }

    // --- State Machine ---
    public ChallengeStatus Status { get; private set; }
    public UserId? AuthorizedBy { get; private set; }
    public DateTime? AuthorizedAt { get; private set; }

    // --- Contextual Security ---
    public string RequestingIp { get; private set; }
    public DeviceMetadata? RequestingDeviceMetadata { get; private set; }

    public enum ChallengeStatus { Pending, Authorized, Consumed, Expired, Revoked }

    private QrChallenge() { }

    private QrChallenge(
        QrChallengeId id,
        string secretCode,
        string requestingIp,
        DeviceMetadata? metadata,
        TimeSpan ttl)
    {
        Id = id;
        SecretCode = secretCode;
        RequestingIp = requestingIp;
        RequestingDeviceMetadata = metadata;
        Status = ChallengeStatus.Pending;
        ExpiresAt = DateTime.UtcNow.Add(ttl);

        AddDomainEvent(new QrChallengeCreatedEvent(Id, SecretCode));
    }

    /// <summary>
    /// Factory to initiate a new QR Handshake.
    /// Uses 32-byte entropy for the SecretCode to prevent brute-force guessing.
    /// </summary>
    public static QrChallenge Create(string ipAddress, DeviceMetadata? metadata)
    {
        // Gold Standard: Use high-entropy cryptographically secure string
        var secureCode = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        return new QrChallenge(
            QrChallengeId.New(),
            secureCode,
            ipAddress,
            metadata,
            TimeSpan.FromSeconds(120)); // QR codes should be short-lived
    }

    // --- Domain Behaviors ---

    /// <summary>
    /// Called by the Authenticated Mobile App to authorize this session.
    /// </summary>
    public void Authorize(UserId authorizerId)
    {
        EnsureActive();

        if (Status != ChallengeStatus.Pending)
            throw new DomainException($"Cannot authorize challenge in {Status} state.");

        AuthorizedBy = authorizerId;
        AuthorizedAt = DateTime.UtcNow;
        Status = ChallengeStatus.Authorized;

        AddDomainEvent(new QrChallengeAuthorizedEvent(Id, AuthorizedBy));
    }

    /// <summary>
    /// Called by the Requester (Browser) to exchange the authorized challenge for a real token.
    /// </summary>
    public void Consume()
    {
        if (Status != ChallengeStatus.Authorized)
            throw new DomainException("Challenge must be authorized before consumption.");

        Status = ChallengeStatus.Consumed;
        AddDomainEvent(new QrChallengeConsumedEvent(Id, AuthorizedBy!));
    }

    public void Revoke(string reason)
    {
        Status = ChallengeStatus.Revoked;
        AddDomainEvent(new QrChallengeRevokedEvent(Id, reason));
    }

    private void EnsureActive()
    {
        if (DateTime.UtcNow > ExpiresAt)
        {
            Status = ChallengeStatus.Expired;
            throw new DomainException("QR Challenge has expired.");
        }
    }
}
