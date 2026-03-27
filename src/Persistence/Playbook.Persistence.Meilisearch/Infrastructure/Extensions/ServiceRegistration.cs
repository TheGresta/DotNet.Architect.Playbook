using Meilisearch;

using Playbook.Persistence.Meilisearch.Infrastructure.Client;

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

        services.AddScoped<MeiliContext>();
        return services;
    }
}
