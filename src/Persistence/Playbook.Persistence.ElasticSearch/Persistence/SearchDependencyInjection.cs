using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Playbook.Persistence.ElasticSearch.Application;

namespace Playbook.Persistence.ElasticSearch.Persistence;

public static class SearchDependencyInjection
{
    public static IServiceCollection AddElasticsearch(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind and Validate Options
        services.AddOptions<ElasticsearchOptions>()
            .Bind(configuration.GetSection(ElasticsearchOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration.GetSection(ElasticsearchOptions.SectionName).Get<ElasticsearchOptions>()
                      ?? throw new InvalidOperationException("Elasticsearch configuration section is missing.");

        var settings = new ElasticsearchClientSettings(new Uri(options.Url))
            .DefaultIndex(options.DefaultIndex)
            .MaximumRetries(options.MaxRetries)
            .SniffOnStartup(options.SniffOnStartup)
            .ThrowExceptions(false);

        if (options.EnableDebugMode)
        {
            settings.EnableDebugMode().PrettyJson();
        }

        // 2. Handle Authentication Strategy
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            settings.Authentication(new ApiKey(options.ApiKey));
        }
        else if (!string.IsNullOrWhiteSpace(options.Username))
        {
            settings.Authentication(new BasicAuthentication(options.Username, options.Password ?? string.Empty));
        }

        // 3. Register Singleton Client (The "Engine" of the connection pool)
        var client = new ElasticsearchClient(settings);
        services.AddSingleton(client);

        // 4. Register Infrastructure Implementations
        services.AddScoped(typeof(ISearchService<>), typeof(ElasticsearchService<>));

        return services;
    }
}