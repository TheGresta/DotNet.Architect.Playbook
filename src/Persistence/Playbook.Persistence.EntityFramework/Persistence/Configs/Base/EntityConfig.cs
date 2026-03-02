using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Playbook.Persistence.EntityFramework.Domain.Base;

namespace Playbook.Persistence.EntityFramework.Persistence.Configs.Base;

internal abstract class EntityConfig<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : Entity
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(x => x.Id);

        builder.Property(x => x.IsActive)
               .HasDefaultValue(true)
               .IsRequired();

        builder.HasQueryFilter(x => x.IsActive);

        ConfigureEntity(builder);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}