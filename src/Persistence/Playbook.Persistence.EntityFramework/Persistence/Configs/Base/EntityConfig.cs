using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Playbook.Persistence.EntityFramework.Domain.Base;

namespace Playbook.Persistence.EntityFramework.Persistence.Configs.Base;

/// <summary>
/// Provides a base configuration for all entities deriving from <see cref="Entity"/>.
/// </summary>
/// <typeparam name="TEntity">The type of the entity to be configured.</typeparam>
/// <remarks>
/// This class handles global configurations such as the default identity property 
/// and a global query filter for the soft-delete (<see cref="Entity.IsActive"/>) mechanism.
/// </remarks>
internal abstract class EntityConfig<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : Entity
{
    /// <summary>
    /// Configures the base properties and global filters for the entity.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(x => x.Id);

        builder.Property(x => x.IsActive)
               .HasDefaultValue(true)
               .IsRequired();

        // Automatically filters out "deleted" (inactive) records from all queries using this context.
        builder.HasQueryFilter(x => x.IsActive);

        ConfigureEntity(builder);
    }

    /// <summary>
    /// When overridden in a derived class, provides additional configuration for the specific entity type.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}