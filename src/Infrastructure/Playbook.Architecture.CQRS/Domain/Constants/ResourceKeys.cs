namespace Playbook.Architecture.CQRS.Domain.Constants;

public static class ResourceKeys
{
    private const string _prefix = LocalizationPrefixes.Resource;

    public const string User = _prefix + "USER";
    public const string Order = _prefix + "ORDER";
    public const string Product = _prefix + "PRODUCT";
    public const string Account = _prefix + "PRODUCT";
}
