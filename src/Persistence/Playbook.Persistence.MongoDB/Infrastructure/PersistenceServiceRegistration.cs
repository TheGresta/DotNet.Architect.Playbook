using Playbook.Persistence.MongoDB.Application;
using Playbook.Persistence.MongoDB.Infrastructure.Contexts;

namespace Playbook.Persistence.MongoDB.Infrastructure;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MongoDbOptions>()
            .Bind(configuration.GetSection("MongoDbSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // One client for the whole app
        services.AddSingleton<MongoDbContext>();

        // Repositories
        services.AddScoped<IDocumentCollection, DocumentCollection>();

        services.AddHostedService<MongoDbResilientSeederService>();

        return services;
    }
}
