using System.Reflection;

using Meilisearch;

using Playbook.Persistence.Meilisearch.Infrastructure.Configuration;
using Playbook.Persistence.Meilisearch.Infrastructure.Repositories;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Extensions;

/// <summary>
/// Provides centralized dependency injection (DI) registration logic for the Meilisearch persistence layer.
/// This class automates the discovery of index configurations and sets up the search client using 
/// high-performance defaults.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Configures and adds Meilisearch services to the <see cref="IServiceCollection"/>.
    /// This includes the <see cref="MeilisearchClient"/>, the <see cref="MeiliContext"/>, 
    /// and automatic discovery of all <see cref="IMeiliIndexConfiguration"/> implementations.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="config">The application configuration for retrieving connection strings and API keys.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> for method chaining.</returns>
    public static IServiceCollection AddMeiliPersistence(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(sp =>
        {
            // Retrieves connection parameters with sensible defaults for local development environments.
            var url = config["Meili:Url"] ?? "http://localhost:7700";
            var apiKey = config["Meili:ApiKey"] ?? "masterKey";

            // The MeilisearchClient is registered as a singleton to maintain persistent 
            // HTTP connections and leverage internal connection pooling.
            return new MeilisearchClient(url, apiKey);
        });

        // The MeiliContext acts as the primary orchestrator for index lifecycle management.
        services.AddSingleton<MeiliContext>();

        // 1. Automatic Discovery: Register all IMeiliIndexConfiguration implementations found in the current assembly.
        // This eliminates the need for manual registration whenever a new index is added to the system.
        var assembly = Assembly.GetExecutingAssembly();
        var configurations = assembly.GetTypes()
            .Where(t => typeof(IMeiliIndexConfiguration).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var cnf in configurations)
        {
            // Registers each configuration under the common interface to be resolved as an IEnumerable 
            // by the MeiliContext constructor.
            services.AddSingleton(typeof(IMeiliIndexConfiguration), cnf);
        }

        // 2. Register the domain-specific repository implementation.
        // This maps the abstraction to the concrete search implementation.
        services.AddSingleton<ICarDocumentRepository, CarDocumentRepository>();

        return services;
    }
}
