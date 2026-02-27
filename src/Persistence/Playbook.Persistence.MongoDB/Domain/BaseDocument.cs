namespace Playbook.Persistence.MongoDB.Domain;

/// <summary>
/// Provides a base abstract class for all MongoDB documents, ensuring a consistent schema for identification and auditing.
/// </summary>
public abstract class BaseDocument
{
    /// <summary>
    /// Gets or sets the unique identifier for the document.
    /// </summary>
    /// <value>A <see cref="Guid"/> that uniquely identifies the document in the collection. Defaults to <see cref="Guid.NewGuid"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the date and time, in UTC, when the document was created.
    /// </summary>
    /// <value>The <see cref="DateTime"/> representing the creation timestamp. Defaults to <see cref="DateTime.UtcNow"/>.</value>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the version number used for optimistic concurrency control.
    /// </summary>
    /// <remarks>
    /// This value should be incremented on every update to prevent lost updates when multiple processes 
    /// attempt to modify the same document simultaneously.
    /// </remarks>
    public int Version { get; set; } = 1;
}
