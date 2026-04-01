using Playbook.Security.IdP.Domain.Entities.Base;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Events;
using Playbook.Security.IdP.Domain.Exceptions;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Entities;

public sealed class QrChallenge : Entity<QrChallengeId>
{
    // --- Handshake Metadata ---
    public ChallengeSecret SecretCode { get; private set; }
    public string? ConnectionId { get; private set; } // Real-time push identifier
    public DateTime ExpiresAt { get; private set; }

    // --- State Machine ---
    public ChallengeStatus Status { get; private set; }
    public UserId? AuthorizedBy { get; private set; }
    public DateTime? AuthorizedAt { get; private set; }

    // --- Contextual Security ---
    public IpAddress RequestingIp { get; private set; }
    public DeviceMetadata? RequestingDeviceMetadata { get; private set; }

    public enum ChallengeStatus { Pending, Authorized, Consumed, Expired, Revoked }

    private QrChallenge() { }

    private QrChallenge(
        QrChallengeId id,
        ChallengeSecret secretCode,
        IpAddress requestingIp,
        DeviceMetadata? metadata,
        TimeSpan ttl)
    {
        Id = id;
        SecretCode = secretCode;
        RequestingIp = requestingIp;
        RequestingDeviceMetadata = metadata;
        Status = ChallengeStatus.Pending;
        ExpiresAt = DateTime.UtcNow.Add(ttl);

        AddDomainEvent(new QrChallengeCreatedEvent(Id, SecretCode.Value));
    }

    /// <summary>
    /// Factory to initiate a new QR Handshake with strict 120s TTL.
    /// </summary>
    public static QrChallenge Create(string ipAddress, DeviceMetadata? metadata)
    {
        return new QrChallenge(
            QrChallengeId.New(),
            ChallengeSecret.Generate(),
            IpAddress.Create(ipAddress),
            metadata,
            TimeSpan.FromSeconds(120));
    }

    // --- Domain Behaviors ---

    public void Authorize(UserId authorizerId)
    {
        EnsureNotExpired();

        if (Status != ChallengeStatus.Pending)
            throw new DomainException($"Challenge cannot be authorized from {Status} state.", "INVALID_STATE");

        AuthorizedBy = authorizerId;
        AuthorizedAt = DateTime.UtcNow;
        Status = ChallengeStatus.Authorized;

        AddDomainEvent(new QrChallengeAuthorizedEvent(Id, AuthorizedBy));
    }

    public void Consume()
    {
        EnsureNotExpired();

        if (Status != ChallengeStatus.Authorized)
            throw new DomainException("Challenge must be authorized before it can be consumed.", "NOT_AUTHORIZED");

        Status = ChallengeStatus.Consumed;

        AddDomainEvent(new QrChallengeConsumedEvent(Id, AuthorizedBy!));
    }

    public void Revoke(string reason)
    {
        if (Status is ChallengeStatus.Consumed or ChallengeStatus.Expired)
            return; // Terminal states cannot be revoked

        Status = ChallengeStatus.Revoked;

        AddDomainEvent(new QrChallengeRevokedEvent(Id, reason));
    }

    public void UpdateConnectionId(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId)) return;
        ConnectionId = connectionId;
    }

    private void EnsureNotExpired()
    {
        if (DateTime.UtcNow > ExpiresAt)
        {
            Status = ChallengeStatus.Expired;
            throw new DomainException("The QR code has expired. Please refresh and try again.", "QR_EXPIRED");
        }
    }
}
