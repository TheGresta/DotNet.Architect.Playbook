namespace Playbook.Security.IdP.Domain.ValueObjects;

public record DeviceMetadata(
    string OperatingSystem,
    string Browser,
    string UserAgent,
    string HardwareModel
);
