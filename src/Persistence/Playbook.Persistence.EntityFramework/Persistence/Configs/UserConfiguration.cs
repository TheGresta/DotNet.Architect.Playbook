using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Playbook.Persistence.EntityFramework.Domain;
using Playbook.Persistence.EntityFramework.Persistence.Configs.Base;
using Playbook.Persistence.EntityFramework.Persistence.Extensions;

namespace Playbook.Persistence.EntityFramework.Persistence.Configs;

/// <summary>
/// Specialized configuration for the <see cref="UserEntity"/>, defining table names, constraints, and encryption.
/// </summary>
internal class UserConfiguration : AuditableEntityConfig<UserEntity>
{
    /// <summary>
    /// Configures the specific schema mapping and data constraints for the Users table.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the <see cref="UserEntity"/>.</param>
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
               .Encrypt() // Custom extension to mark the property for AES encryption.
               .IsRequired();
    }
}
