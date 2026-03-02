using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Playbook.Persistence.EntityFramework.Domain;
using Playbook.Persistence.EntityFramework.Persistence.Configs.Base;
using Playbook.Persistence.EntityFramework.Persistence.Extensions;

namespace Playbook.Persistence.EntityFramework.Persistence.Configs;

internal class UserConfiguration : AuditableEntityConfig<UserEntity>
{
    protected override void ConfigureAuditableEntity(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("Users");

        builder.Property(x => x.Name)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(x => x.Surname)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(x => x.Email)
               .HasMaxLength(255)
               //.Encrypt()
               .IsRequired();
    }
}
