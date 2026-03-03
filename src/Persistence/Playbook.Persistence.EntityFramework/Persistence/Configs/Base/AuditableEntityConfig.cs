using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Playbook.Persistence.EntityFramework.Domain.Base;

namespace Playbook.Persistence.EntityFramework.Persistence.Configs.Base;

/// <summary>
/// Provides a base configuration for entities that require audit-trail metadata.
/// </summary>
/// <typeparam name="TEntity">The type of the auditable entity.</typeparam>
internal abstract class AuditableEntityConfig<TEntity> : EntityConfig<TEntity> where TEntity : AuditableEntity
{
    /// <summary>
    /// Configures audit-specific properties such as creation and update metadata.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    protected override void ConfigureEntity(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(x => x.CreatedBy)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(x => x.CreatedAt)
               .IsRequired();

        builder.Property(x => x.UpdatedBy)
               .HasMaxLength(100)
               .IsRequired(false);

        builder.Property(x => x.UpdatedAt)
               .IsRequired(false);

        ConfigureAuditableEntity(builder);
    }

    /// <summary>
    /// When overridden in a derived class, provides configuration for the specific auditable entity type.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    protected abstract void ConfigureAuditableEntity(EntityTypeBuilder<TEntity> builder);
}