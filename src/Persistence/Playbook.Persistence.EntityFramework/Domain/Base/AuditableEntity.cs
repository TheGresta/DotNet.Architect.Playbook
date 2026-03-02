namespace Playbook.Persistence.EntityFramework.Domain.Base;

/// <summary>
///  Represents an abstract class serving as the base for entities that track audit information 
///  such as creation and modification timestamps along with the user responsible for the changes.
/// </summary>
public abstract class AuditableEntity : Entity
{
    /// <summary>
    /// Gets or sets the timestamp when the entity was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created the entity.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the entity was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated the entity, if applicable.
    /// </summary>
    public string? UpdatedBy { get; set; }
}
