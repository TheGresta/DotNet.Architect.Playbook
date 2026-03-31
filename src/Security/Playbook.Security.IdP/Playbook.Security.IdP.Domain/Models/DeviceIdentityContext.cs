namespace Playbook.Security.IdP.Domain.Models;

public record DeviceIdentityContext(
    string? RawFingerprint,     // Legacy/Passive
    string? PublicKeyHash,      // Cryptographic/Active (The "Gold" way)
    string? HardwareId,         // e.g., IMEI or MAC (Mobile specific)
    string UserAgent
);
