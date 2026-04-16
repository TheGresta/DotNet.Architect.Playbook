using Playbook.Security.IdP.Domain.Entities.Ids;

namespace Playbook.Security.IdP.Domain.Common;

/// <summary>
/// Extends the <see cref="Entity"/> class to provide audit-trail capabilities, 
/// including creation and modification metadata.
/// </summary>
public abstract class AuditableEntity<TId> : Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets or sets the date and time, in UTC, when the entity was first persisted.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets or sets the identifier (e.g., username or system ID) of the user who created the entity.
    /// </summary>
    public UserId CreatedBy { get; private set; } = null!;

    /// <summary>
    /// Gets or sets the date and time, in UTC, when the entity was last modified.
    /// </summary>
    /// <value>
    /// A <see cref="Nullable{DateTime}"/> representing the last update timestamp, or <see langword="null"/> if the entity has never been updated.
    /// </value>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last modified the entity.
    /// </summary>
    public UserId? UpdatedBy { get; private set; }

    public void SetCreationMetadata(DateTimeOffset createdAt, UserId createdBy)
    {
        if (CreatedBy is not null && CreatedAt != default)
            throw new InvalidOperationException("Creation metadata has already been set.");

        CreatedAt = createdAt.UtcDateTime;
        CreatedBy = createdBy;
    }

    public void SetUpdateMetadata(DateTimeOffset updatedAt, UserId updatedBy)
    {
        UpdatedAt = updatedAt.UtcDateTime;
        UpdatedBy = updatedBy;
    }
}
