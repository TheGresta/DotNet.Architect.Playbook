using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Events;

// ── Device Events ─────────────────────────────────────────────────────────────

public record DeviceRegisteredEvent(UserId UserId, DeviceId DeviceId, DeviceIdentity Identity) : DomainEvent;
public record DeviceTrustElevatedEvent(DeviceId DeviceId, UserId UserId, string Reason) : DomainEvent;
public record DeviceTrustRevokedEvent(DeviceId DeviceId, UserId UserId, string Reason) : DomainEvent;
public record DeviceLocationChangedEvent(DeviceId DeviceId, IpAddress PreviousIp, IpAddress CurrentIp) : DomainEvent;
public record DeviceSuspendedEvent(DeviceId DeviceId, UserId UserId, string Reason) : DomainEvent;
public record DeviceUnsuspendedEvent(DeviceId DeviceId, UserId UserId, string Reason) : DomainEvent;
