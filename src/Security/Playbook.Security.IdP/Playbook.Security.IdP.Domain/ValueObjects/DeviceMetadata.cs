using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Domain.ValueObjects;

public sealed class DeviceMetadata : ValueObject
{
    public string OperatingSystem { get; }
    public string Browser { get; }
    public string IpAddress { get; }

    public DeviceMetadata(string os, string browser, string ip)
    {
        OperatingSystem = os ?? "Unknown";
        Browser = browser ?? "Unknown";
        IpAddress = ip ?? "0.0.0.0";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return OperatingSystem;
        yield return Browser;
        yield return IpAddress;
    }
}
