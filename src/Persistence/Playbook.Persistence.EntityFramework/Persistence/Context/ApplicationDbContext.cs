using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Playbook.Persistence.EntityFramework.Persistence.Encryption;
using Playbook.Persistence.EntityFramework.Persistence.Extensions;
using Playbook.Persistence.EntityFramework.Persistence.Options;

namespace Playbook.Persistence.EntityFramework.Persistence.Context;

/// <summary>
/// The primary database context for the application, responsible for configuring entity mappings,
/// managing transactions, and applying global data behaviors such as encryption.
/// </summary>
/// <param name="options">The options to be used by this <see cref="DbContext"/>.</param>
/// <param name="aesEncryptionService">The service used to perform AES encryption and decryption on sensitive data.</param>
/// <param name="dbOptions">The application-specific database settings, including encryption toggles.</param>
internal class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IAesEncryptionService aesEncryptionService,
    IOptions<DbOptions> dbOptions) : DbContext(options)
{
    /// <summary>
    /// Holds the resolved database configuration options.
    /// </summary>
    private readonly DbOptions _dbOptions = dbOptions.Value;

    /// <summary>
    /// Configures the model that was discovered by convention from the entity types exposed 
    /// in <see cref="DbSet{TEntity}"/> properties on this context.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    /// <remarks>
    /// This override applies all configurations from the current assembly and initializes 
    /// the encryption pipeline if enabled.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Dynamically applies all IEntityTypeConfiguration implementations in the assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        SetEncryption(modelBuilder);
    }

    /// <summary>
    /// Scans the entire model for string properties marked for encryption and applies 
    /// an <see cref="AesEncryptedConverter"/> to them.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to configure the model.</param>
    /// <remarks>
    /// The encryption is only applied if <see cref="DbOptions.EncryptionEnabled"/> is <see langword="true"/>.
    /// It looks for the custom annotation defined in <see cref="PropertyBuilderExtensions.EncryptionAnnotation"/>.
    /// </remarks>
    private void SetEncryption(ModelBuilder modelBuilder)
    {
        if (!_dbOptions.EncryptionEnabled) return;

        var converter = new AesEncryptedConverter(aesEncryptionService);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Retrieve all string properties that carry the specific encryption metadata annotation.
            var properties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(string) &&
                            p.FindAnnotation(PropertyBuilderExtensions.EncryptionAnnotation) != null);

            foreach (var property in properties)
            {
                // Assigns the value converter to handle transparent encryption/decryption during DB I/O.
                property.SetValueConverter(converter);
            }
        }
    }
}
