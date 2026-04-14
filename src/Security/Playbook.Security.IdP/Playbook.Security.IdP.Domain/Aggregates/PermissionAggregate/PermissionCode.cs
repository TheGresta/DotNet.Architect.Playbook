namespace Playbook.Security.IdP.Domain.Aggregates.PermissionAggregate;

public record PermissionCode
{
    public string Value { get; }
    private PermissionCode(string value) => Value = value.ToLowerInvariant();

    public static PermissionCode Create(string @namespace, string resource, string action)
        => new($"{@namespace}:{resource}:{action}");

    public static implicit operator string(PermissionCode code) => code.Value;
}
