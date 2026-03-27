using System.Reflection;

using Meilisearch;

using Playbook.Persistence.Meilisearch.Infrastructure.Configuration;
using Playbook.Persistence.Meilisearch.Infrastructure.Repositories;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Extensions;

public static class ServiceRegistration
{
    public static IServiceCollection AddMeiliPersistence(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(sp =>
        {
            var url = config["Meili:Url"] ?? "http://localhost:7700";
            var apiKey = config["Meili:ApiKey"] ?? "masterKey";

            // Use our Source-Generated Context for maximum performance
            return new MeilisearchClient(url, apiKey);
        });

        services.AddSingleton<MeiliContext>();

        // 1. Register all IMeiliIndexConfiguration implementations automatically
        var assembly = Assembly.GetExecutingAssembly();
        var configurations = assembly.GetTypes()
            .Where(t => typeof(IMeiliIndexConfiguration).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var cnf in configurations)
        {
            services.AddSingleton(typeof(IMeiliIndexConfiguration), cnf);
        }

        // 2. Register the generic repository
        services.AddSingleton<ICarDocumentRepository, CarDocumentRepository>();

        return services;
    }
}
