using Playbook.Security.IdP.Domain.Common;
using Playbook.Security.IdP.Domain.Entities.Ids;
using Playbook.Security.IdP.Domain.Exceptions;

namespace Playbook.Security.IdP.Domain.Entities;

/// <summary>
/// A granular permission in the namespace:resource:action format.
///
/// Design decisions:
/// - ResourcePattern: Supports wildcard patterns (e.g., "documents/*") for
///   resource-based access control. Evaluated by the policy engine.
/// - Conditions: A JSON policy expression (e.g., ABAC attribute conditions)
///   evaluated at runtime. Null = unconditional grant.
/// - Effect: Explicit allow/deny enables role exclusions without role removal.
///   A deny permission in a role overrides any allow from another role.
/// </summary>
public sealed class Permission : Entity<PermissionId>
{
    /// <summary>Fully-qualified permission code: namespace:resource:action</summary>
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    /// <summary>Optional resource pattern with wildcard support. Null = applies to all resources.</summary>
    public string? ResourcePattern { get; private set; }

    /// <summary>JSON policy conditions for ABAC evaluation. Null = unconditional.</summary>
    public string? Conditions { get; private set; }

    public PermissionEffect Effect { get; private set; } = PermissionEffect.Allow;

    public enum PermissionEffect { Allow, Deny }

    // ORM constructor
    private Permission() { }

    public Permission(
        PermissionId id,
        string name,
        string description,
        PermissionEffect effect = PermissionEffect.Allow,
        string? resourcePattern = null,
        string? conditions = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Permission name cannot be empty.", "INVALID_PERMISSION_NAME");

        // Enforce namespace:resource:action format
        if (name.Split(':').Length != 3)
            throw new DomainException(
                "Permission name must follow namespace:resource:action format.",
                "INVALID_PERMISSION_FORMAT");

        Id = id;

        var trimmedName = name.Trim();
        var segments = trimmedName.Split(':');
        if (segments.Length != 3 || segments.Any(string.IsNullOrWhiteSpace))
            throw new DomainException(
            "Permission name must follow namespace:resource:action format with non-empty segments.",
            "INVALID_PERMISSION_FORMAT");

        Name = trimmedName.ToLowerInvariant();
        Description = description?.Trim() ?? string.Empty;
        Effect = effect;
        ResourcePattern = resourcePattern?.Trim();
        Conditions = conditions;
    }

    public bool IsDeny => Effect == PermissionEffect.Deny;
    public bool IsAllow => Effect == PermissionEffect.Allow;
}
