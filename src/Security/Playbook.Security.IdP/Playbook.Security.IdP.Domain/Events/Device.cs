using Playbook.Security.IdP.Domain.ValueObjects;
using Playbook.Security.IdP.Domain.ValueObjects.Ids;

namespace Playbook.Security.IdP.Domain.Events;

public record DeviceRegisteredEvent(UserId UserId, DeviceId DeviceId, DeviceIdentity idendity) : IDomainEvent;

public record DeviceTrustElevatedEvent(DeviceId DeviceId, UserId UserId, string Reason) : IDomainEvent;
