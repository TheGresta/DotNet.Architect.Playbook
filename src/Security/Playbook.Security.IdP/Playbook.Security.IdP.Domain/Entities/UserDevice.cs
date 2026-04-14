using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Events;
using Playbook.Security.IdP.Domain.Exceptions;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Entities;

public sealed class UserDevice : AuditableEntity<DeviceId>
{
    // --- Identity & Hardware ---
    public UserId UserId { get; private set; }
    public DeviceIdentity Identity { get; private set; }
    public DeviceMetadata Metadata { get; private set; }

    // --- State ---
    public bool IsTrusted { get; private set; }
    public bool IsSuspended { get; private set; }
    public DateTime LastUsedAt { get; private set; }
    public IpAddress LastIpAddress { get; private set; } // Now a Value Object

    // Risk Metrics
    public int FailureCount { get; private set; }

    // Required for ORM
    private UserDevice() { }

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

    /// <summary>
    /// Gold Standard Factory.
    /// </summary>
    public static UserDevice Create(
        UserId userId,
        DeviceIdentity identity,
        string ipAddress,
        DeviceMetadata metadata)
    {
        // Validation handled by the IP Value Object
        var validatedIp = IpAddress.Create(ipAddress);

        return new UserDevice(
            DeviceId.New(),
            userId,
            identity,
            validatedIp,
            metadata);
    }

    // --- Domain Behaviors ---

    public void MarkAsTrusted(string reason)
    {
        EnsureActive();

        IsTrusted = true;
        FailureCount = 0;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new DeviceTrustElevatedEvent(Id, UserId, reason));
    }

    public void RecordUsage(string newIp)
    {
        EnsureActive();

        var validatedIp = IpAddress.Create(newIp);

        // Security check: If IP changes, notify the system for risk analysis
        if (LastIpAddress != validatedIp)
        {
            AddDomainEvent(new DeviceLocationChangedEvent(Id, LastIpAddress.Value, validatedIp.Value));
        }

        LastIpAddress = validatedIp;
        LastUsedAt = DateTime.UtcNow;
    }

    public void RecordFailure()
    {
        FailureCount++;

        if (FailureCount >= 3)
        {
            Suspend("Too many consecutive verification failures.");
        }
    }

    public void Suspend(string reason)
    {
        if (IsSuspended) return;

        IsSuspended = true;
        IsTrusted = false;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new DeviceSuspendedEvent(Id, UserId, reason));
    }

    /// <summary>
    /// Guard method to prevent actions on compromised/suspended hardware.
    /// </summary>
    private void EnsureActive()
    {
        if (IsSuspended)
            throw new DomainException("This device is suspended and cannot be used.", "DEVICE_SUSPENDED");

        if (!IsActive)
            throw new DomainException("This device is no longer active.", "DEVICE_INACTIVE");
    }
}
