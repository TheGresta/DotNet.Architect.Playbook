using Playbook.Security.IdP.Domain.Entities.Base;
using Playbook.Security.IdP.Domain.Events;
using Playbook.Security.IdP.Domain.ValueObjects;
using Playbook.Security.IdP.Domain.ValueObjects.Ids;

namespace Playbook.Security.IdP.Domain.Entities;

public sealed class UserDevice : AuditableEntity<DeviceId>
{
    // --- Properties with Private Setters (Encapsulation) ---
    public UserId UserId { get; private set; }
    public DeviceIdentity Identity { get; private set; }
    public bool IsTrusted { get; private set; }
    public DateTime LastUsedAt { get; private set; }
    public string LastIpAddress { get; private set; }
    public DeviceMetadata Metadata { get; private set; }

    // Risk Scoring for Adaptive Auth
    public int FailureCount { get; private set; }
    public bool IsSuspended { get; private set; }

    // --- Constructor & Factory ---
    private UserDevice() { } // Required for ORM

    private UserDevice(
        DeviceId id,
        UserId userId,
        DeviceIdentity identity,
        string ipAddress,
        DeviceMetadata metadata)
    {
        Id = id;
        UserId = userId;
        Identity = identity;
        LastIpAddress = ipAddress;
        Metadata = metadata;
        LastUsedAt = DateTime.UtcNow;
        IsTrusted = false;
        IsActive = true;

        // Notify system a new hardware profile is being registered
        AddDomainEvent(new DeviceRegisteredEvent(UserId, Id, Identity));
    }

    /// <summary>
    /// Factory method ensuring all required invariants for a new device are met.
    /// </summary>
    public static UserDevice Create(
        UserId userId,
        DeviceIdentity identity,
        string ipAddress,
        DeviceMetadata metadata)
    {
        // Validation logic can live here or in the ValueObjects
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(identity);

        return new UserDevice(DeviceId.New(), userId, identity, ipAddress, metadata);
    }

    // --- Domain Behaviors (The "Gold" Logic) ---

    /// <summary>
    /// Transitions the device to a trusted state after successful MFA.
    /// </summary>
    public void MarkAsTrusted(string reason)
    {
        if (IsSuspended)
            throw new DomainException("Cannot trust a suspended device.");

        IsTrusted = true;
        FailureCount = 0; // Reset risk metrics
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new DeviceTrustElevatedEvent(Id, UserId, reason));
    }

    /// <summary>
    /// Records device activity and performs basic anomaly detection.
    /// </summary>
    public void RecordUsage(string ipAddress)
    {
        // If IP changes drastically, we could trigger a specific event
        if (LastIpAddress != ipAddress)
        {
            AddDomainEvent(new DeviceLocationChangedEvent(Id, LastIpAddress, ipAddress));
        }

        LastIpAddress = ipAddress;
        LastUsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Suspends device access due to suspicious activity (e.g., too many MFA failures).
    /// </summary>
    public void Suspend(string reason)
    {
        IsSuspended = true;
        IsTrusted = false;
        AddDomainEvent(new DeviceSuspendedEvent(Id, UserId, reason));
    }
}
