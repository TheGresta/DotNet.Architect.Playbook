using Playbook.Persistence.EntityFramework.Domain.Base;

namespace Playbook.Persistence.EntityFramework.Domain;

/// <summary>
/// Represents a user within the system, inheriting identity and audit functionality from <see cref="AuditableEntity"/>.
/// </summary>
public class UserEntity : AuditableEntity
{
    /// <summary>
    /// Gets or sets the given name of the user.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the family name of the user.
    /// </summary>
    public string Surname { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary email address for the user, typically used for authentication.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
