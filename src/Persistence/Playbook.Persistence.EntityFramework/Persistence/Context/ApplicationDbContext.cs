using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Playbook.Persistence.EntityFramework.Persistence.Encryption;
using Playbook.Persistence.EntityFramework.Persistence.Extensions;
using Playbook.Persistence.EntityFramework.Persistence.Options;

namespace Playbook.Persistence.EntityFramework.Persistence.Context;

internal class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IAesEncryptionService aesEncryptionService,
    IOptions<DbOptions> dbOptions) : DbContext(options)
{
    private readonly DbOptions _dbOptions = dbOptions.Value;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        SetEncryption(modelBuilder);
    }

    private void SetEncryption(ModelBuilder modelBuilder)
    {
        if (!_dbOptions.EncryptionEnabled) return;

        var converter = new AesEncryptedConverter(aesEncryptionService);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(string) &&
                            p.FindAnnotation(PropertyBuilderExtensions.EncryptionAnnotation) != null);

            foreach (var property in properties)
            {
                property.SetValueConverter(converter);
            }
        }
    }
}
