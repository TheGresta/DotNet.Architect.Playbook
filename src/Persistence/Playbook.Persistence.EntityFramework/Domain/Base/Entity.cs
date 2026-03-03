namespace Playbook.Persistence.EntityFramework.Domain.Base;

/// <summary>
/// Provides a base abstraction for all domain entities, ensuring a consistent identity through a unique identifier.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets the unique identifier for the entity.
    /// </summary>
    /// <value>
    /// A <see cref="Guid"/> representing the unique identity of the entity.
    /// </value>
    /// <remarks>
    /// This property uses the <see langword="init"/> setter to ensure the identity is immutable after object creation.
    /// </remarks>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity is logically active within the system.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the entity is active; otherwise, <see langword="false"/>. The default is <see langword="true"/>.
    /// </value>
    public bool IsActive { get; set; } = true;
}
