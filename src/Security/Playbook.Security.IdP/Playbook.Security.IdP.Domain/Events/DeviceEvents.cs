using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Events;

public record DeviceRegisteredEvent(UserId UserId, DeviceId DeviceId, DeviceIdentity idendity) : DomainEvent;

public record DeviceTrustElevatedEvent(DeviceId DeviceId, UserId UserId, string Reason) : DomainEvent;

public record DeviceLocationChangedEvent(DeviceId Id, string LastIpAddress, string IpAddress) : DomainEvent;

public record DeviceSuspendedEvent(DeviceId Id, UserId UserId, string Reason) : DomainEvent;
