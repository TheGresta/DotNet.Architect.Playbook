using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Playbook.Persistence.EntityFramework.Application;
using Playbook.Persistence.EntityFramework.Persistence.Context;
using Playbook.Persistence.EntityFramework.Persistence.Encryption;
using Playbook.Persistence.EntityFramework.Persistence.Interceptors;
using Playbook.Persistence.EntityFramework.Persistence.Options;

namespace Playbook.Persistence.EntityFramework.Persistence;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to centralize the registration 
/// of persistence-related services and database configurations.
/// </summary>
public static class PersistenceServiceRegistration
{
    /// <summary>
    /// Registers the database context, unit of work, encryption services, and interceptors 
    /// with the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining additional configuration calls.</returns>
    /// <remarks>
    /// <para>
    /// <b>Service Lifetimes:</b>
    /// <list type="bullet">
    /// <item>
    /// <description><b>Encryption:</b> Registered as <c>Singleton</c> as it is stateless and shares a common key.</description>
    /// </item>
    /// <item>
    /// <description><b>Interceptor:</b> Registered as <c>Scoped</c> to access the current execution context if needed.</description>
    /// </item>
    /// <item>
    /// <description><b>DbContext &amp; UnitOfWork:</b> Registered as <c>Scoped</c> to ensure all repositories within a single request share the same database connection and change tracker.</description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Database Provider:</b> This implementation configures PostgreSQL using Npgsql.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services)
    {
        // Stateless service for PII data protection.
        services.AddSingleton<IAesEncryptionService, AesEncryptionService>();

        // Logic for automatic CreatedAt/UpdatedAt timestamps.
        services.AddScoped<AuditableEntityInterceptor>();

        // Context configuration with Interceptor injection.
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DbOptions>>().Value;
            var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();

            options.UseNpgsql(dbOptions.ConnectionString, npgsqlOptions =>
                   {
                       npgsqlOptions.EnableRetryOnFailure(
                           maxRetryCount: 3,
                           maxRetryDelay: TimeSpan.FromSeconds(30),
                           errorCodesToAdd: null);
                   })
                   .AddInterceptors(interceptor);
        });

        // The central coordinator for repository access and transaction management.
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
