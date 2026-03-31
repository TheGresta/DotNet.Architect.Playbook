namespace Playbook.Security.IdP.Domain.ValueObjects;

// Prevents primitive obsession and ensures SHA-256 formatting
public record DeviceIdentity
{
    public string Hash { get; }
    public string DeviceName { get; }

    public DeviceIdentity(string hash, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(hash) || hash.Length < 64)
            throw new DomainException("Invalid identity: Must be a SHA-256 hash.");

        Hash = hash.ToLowerInvariant();
        DeviceName = deviceName;
    }
}
