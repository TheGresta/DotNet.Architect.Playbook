using Playbook.Security.IdP.Domain.Entities;
using Playbook.Security.IdP.Domain.ValueObjects;

namespace Playbook.Security.IdP.Domain.Models;

public record AuthenticationContext(
    DeviceIdentity Identity,
    string IpAddress,
    UserDevice? DetectedDevice,
    RequestMetadata Metadata
);
