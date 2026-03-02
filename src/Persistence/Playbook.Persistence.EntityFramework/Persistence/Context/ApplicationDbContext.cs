using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Playbook.Persistence.EntityFramework.Domain.Base;
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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_dbOptions.ConnectionString);

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        SetEncryption(modelBuilder);
    }

    public override int SaveChanges()
    {
        SetAuditableProperties();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SetAuditableProperties();
        return await base.SaveChangesAsync(cancellationToken);
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

    private void SetAuditableProperties()
    {
        var entries = ChangeTracker.Entries<AuditableEntity>();
        var transactionDate = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = transactionDate;
                    entry.Entity.CreatedBy = _dbOptions.ApplicationName;
                    entry.Entity.IsActive = true;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = transactionDate;
                    entry.Entity.UpdatedBy = _dbOptions.ApplicationName;

                    // Prevent accidental modification of Created fields
                    entry.Property(x => x.CreatedAt).IsModified = false;
                    entry.Property(x => x.CreatedBy).IsModified = false;
                    break;
            }
        }
    }
}
