namespace Playbook.Persistence.EntityFramework.Domain.Base;

/// <summary>
/// Represents an abstract class serving as the base for entities in the system, 
/// providing a unique identifier and an indicator of whether the entity is currently active.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity is active.
    /// <see langword="true"/> if the entity is active, otherwise <see langword="false"/>.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
