using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Playbook.Persistence.EntityFramework.Application;
using Playbook.Persistence.EntityFramework.Persistence.Context;
using Playbook.Persistence.EntityFramework.Persistence.Encryption;
using Playbook.Persistence.EntityFramework.Persistence.Interseptors;
using Playbook.Persistence.EntityFramework.Persistence.Options;

namespace Playbook.Persistence.EntityFramework.Persistence;

public static class PersistenceServiceRegistiration
{
    /// <summary>
    /// Registers persistence services with the dependency injection container.
    /// </summary>
    /// <param name="services">The collection of services to add the persistence services to.</param>
    /// <returns>The modified collection of services.</returns>
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services)
    {
        services.AddSingleton<IAesEncryptionService, AesEncryptionService>();

        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DbOptions>>().Value;
            var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();

            options.UseNpgsql(dbOptions.ConnectionString)
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
