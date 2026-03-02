using Playbook.Persistence.EntityFramework.Domain.Base;

namespace Playbook.Persistence.EntityFramework.Domain;

/// <summary>
/// Represents an entity for a user, extending the base entity 
/// with additional properties such as name, surname, and email.
/// </summary>
public class UserEntity : AuditableEntity
{
    /// <summary>
    /// Gets or sets the name of the user.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the surname of the user.
    /// </summary>
    public string Surname { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
