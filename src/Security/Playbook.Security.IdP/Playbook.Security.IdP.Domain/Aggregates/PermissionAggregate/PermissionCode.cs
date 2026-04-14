namespace Playbook.Security.IdP.Domain.Aggregates.PermissionAggregate;

public record PermissionCode
{
    public string Value { get; }
    private PermissionCode(string value) => Value = value.ToLowerInvariant();

    public static PermissionCode Create(string @namespace, string resource, string action)
    {
        if (string.IsNullOrWhiteSpace(@namespace)) throw new ArgumentException("Namespace is required.", nameof(`@namespace`));
        if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentException("Resource is required.", nameof(resource));
        if (string.IsNullOrWhiteSpace(action)) throw new ArgumentException("Action is required.", nameof(action));
        if (@namespace.Contains(':') || resource.Contains(':') || action.Contains(':'))
            throw new ArgumentException("Permission segments must not contain ':'");

        return new($"{@namespace.Trim()}:{resource.Trim()}:{action.Trim()}");
    }

    public static implicit operator string(PermissionCode code) =>
        code is null ? throw new ArgumentNullException(nameof(code)) : code.Value;
}
