namespace Playbook.Security.IdP.Domain.ServiceModels;

public record DeviceIdentityContext(
    string? FingerprintHash,
    string? PublicKeyHash,
    string? HardwareIdHash,
    string UserAgent
);
