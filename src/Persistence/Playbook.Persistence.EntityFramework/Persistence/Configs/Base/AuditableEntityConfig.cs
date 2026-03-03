using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Playbook.Persistence.EntityFramework.Domain.Base;

namespace Playbook.Persistence.EntityFramework.Persistence.Configs.Base;

internal abstract class AuditableEntityConfig<TEntity> : EntityConfig<TEntity> where TEntity : AuditableEntity
{
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

    protected abstract void ConfigureAuditableEntity(EntityTypeBuilder<TEntity> builder);
}
