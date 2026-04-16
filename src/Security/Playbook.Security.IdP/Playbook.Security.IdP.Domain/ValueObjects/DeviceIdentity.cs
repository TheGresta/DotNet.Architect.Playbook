using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.ValueObjects;

public sealed class DeviceIdentity : ValueObject
{
    public string Hash { get; }
    public string DeviceName { get; }

    public DeviceIdentity(string hash, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(hash) || hash.Length != 64)
            throw new DomainException("Invalid identity: Must be a 64-character SHA-256 hash.", "INVALID_DEVICE_HASH");

        Hash = hash.ToLowerInvariant();
        DeviceName = deviceName?.Trim() ?? "Unknown Device";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hash;
        yield return DeviceName;
    }
}
