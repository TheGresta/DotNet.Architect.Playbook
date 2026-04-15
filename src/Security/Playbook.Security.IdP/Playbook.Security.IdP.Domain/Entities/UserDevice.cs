using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Events;
using Playbook.Security.IdP.Domain.Exceptions;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Entities;

/// <summary>
/// Represents a physical or virtual device that a user has authenticated from.
///
/// Design decisions:
/// - DeviceMetadata no longer stores IpAddress — that was a redundancy bug.
///   Ip is a first-class property on the entity.
/// - RiskProfile is modelled as a value object so it can be updated atomically
///   and compared for change detection.
/// - AttestationStatement captures the FIDO2/WebAuthn attestation data
///   for enterprise device health verification.
/// - TrustGrantedAt / TrustGrantedBy provide an audit trail for the trust elevation.
/// - FailureCount auto-suspend threshold is a constant, not a magic number.
/// - LastVerifiedAt tracks when the device's cryptographic identity was last confirmed,
///   enabling freshness-based revocation policies.
/// </summary>
public sealed class UserDevice : AuditableEntity<DeviceId>
{
    private const int AutoSuspendFailureThreshold = 3;

    // ── Identity & Hardware ───────────────────────────────────────────────────
    public UserId UserId { get; private set; } = null!;
    public DeviceIdentity Identity { get; private set; } = null!;

    /// <summary>
    /// Metadata about the device's operating environment.
    /// Does NOT contain IP — that's a session property, not a device property.
    /// </summary>
    public DeviceMetadata Metadata { get; private set; } = null!;

    // ── State ─────────────────────────────────────────────────────────────────
    public bool IsTrusted { get; private set; }
    public bool IsSuspended { get; private set; }
    public DateTime LastUsedAt { get; private set; }

    /// <summary>
    /// The last IP seen for this device. Tracked for location-change anomaly detection.
    /// Updated on every successful authentication, NOT on every login attempt.
    /// </summary>
    public IpAddress LastIpAddress { get; private set; } = null!;

    // ── Trust Audit ───────────────────────────────────────────────────────────
    public DateTime? TrustGrantedAt { get; private set; }
    public UserId? TrustGrantedBy { get; private set; }  // Admin or user who trusted it
    public string? TrustGrantReason { get; private set; }

    // ── Risk Metrics ──────────────────────────────────────────────────────────
    public int FailureCount { get; private set; }
    public DateTime? SuspendedAt { get; private set; }
    public string? SuspendReason { get; private set; }

    // ── FIDO2 / Device Attestation ────────────────────────────────────────────
    /// <summary>
    /// The FIDO2 attestation statement for hardware-backed devices.
    /// Null for browser-fingerprinted (passive) devices.
    /// </summary>
    public string? AttestationStatement { get; private set; }
    public string? AttestationFormat { get; private set; }  // "packed", "tpm", "android-key"

    /// <summary>When the device's cryptographic identity was last verified.</summary>
    public DateTime? LastVerifiedAt { get; private set; }

    // ── ORM constructor ───────────────────────────────────────────────────────
    private UserDevice() { }

    // ── Private constructor ───────────────────────────────────────────────────
    private UserDevice(
        DeviceId id,
        UserId userId,
        DeviceIdentity identity,
        IpAddress ipAddress,
        DeviceMetadata metadata)
    {
        Id = id;
        UserId = userId;
        Identity = identity;
        LastIpAddress = ipAddress;
        Metadata = metadata;
        LastUsedAt = DateTime.UtcNow;
        IsTrusted = false;
        IsSuspended = false;
        IsActive = true;

        AddDomainEvent(new DeviceRegisteredEvent(UserId, Id, Identity));
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static UserDevice Create(
        UserId userId,
        DeviceIdentity identity,
        string ipAddress,
        DeviceMetadata metadata)
    {
        var validatedIp = IpAddress.Create(ipAddress);
        return new UserDevice(DeviceId.New(), userId, identity, validatedIp, metadata);
    }

    // ── Domain Behaviours ─────────────────────────────────────────────────────

    public void MarkAsTrusted(string reason, UserId grantedBy)
    {
        EnsureActive();

        IsTrusted = true;
        FailureCount = 0;
        TrustGrantedAt = DateTime.UtcNow;
        TrustGrantedBy = grantedBy;
        TrustGrantReason = reason;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new DeviceTrustElevatedEvent(Id, UserId, reason));
    }

    public void RevokeTrust(string reason)
    {
        if (!IsTrusted) return;

        IsTrusted = false;
        TrustGrantedAt = null;
        TrustGrantedBy = null;
        TrustGrantReason = null;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new DeviceTrustRevokedEvent(Id, UserId, reason));
    }

    /// <summary>
    /// Records a successful authentication from this device.
    /// Raises an event if the IP address has changed (anomaly signal).
    /// </summary>
    public void RecordUsage(string currentIp)
    {
        EnsureActive();

        var validatedIp = IpAddress.Create(currentIp);

        if (LastIpAddress != validatedIp)
            AddDomainEvent(new DeviceLocationChangedEvent(Id, LastIpAddress, validatedIp));

        FailureCount = 0;
        LastIpAddress = validatedIp;
        LastUsedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordVerification()
    {
        EnsureActive();
        LastVerifiedAt = DateTime.UtcNow;
    }

    public void RecordFailure()
    {
        FailureCount++;

        if (FailureCount >= AutoSuspendFailureThreshold)
            Suspend($"Automatically suspended after {AutoSuspendFailureThreshold} consecutive failures.");
    }

    public void ResetFailureCount()
    {
        FailureCount = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend(string reason)
    {
        if (IsSuspended) return;

        IsSuspended = true;
        IsTrusted = false;  // Trust is revoked on suspension.
        SuspendedAt = DateTime.UtcNow;
        SuspendReason = reason;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new DeviceSuspendedEvent(Id, UserId, reason));
    }

    public void Unsuspend(string reason)
    {
        if (!IsSuspended) return;

        IsSuspended = false;
        FailureCount = 0;
        SuspendedAt = null;
        SuspendReason = null;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new DeviceUnsuspendedEvent(Id, UserId, reason));
    }

    public void SetAttestation(string statement, string format)
    {
        if (string.IsNullOrWhiteSpace(statement))
            throw new DomainException("Attestation statement cannot be empty.", "INVALID_ATTESTATION");

        AttestationStatement = statement;
        AttestationFormat = format;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void EnsureActive()
    {
        if (IsSuspended)
            throw new DomainException("This device is suspended and cannot be used.", "DEVICE_SUSPENDED");

        if (!IsActive)
            throw new DomainException("This device is no longer active.", "DEVICE_INACTIVE");
    }
}
