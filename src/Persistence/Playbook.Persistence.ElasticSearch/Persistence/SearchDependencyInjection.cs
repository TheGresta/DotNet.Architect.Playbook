using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Options;
using Playbook.Persistence.ElasticSearch.Application;

namespace Playbook.Persistence.ElasticSearch.Persistence;

public static class SearchDependencyInjection
{
    public static IServiceCollection AddElasticsearch(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ElasticsearchOptions>()
            .Bind(configuration.GetSection(ElasticsearchOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;

            var settings = new ElasticsearchClientSettings(new Uri(options.Url))
                .DefaultIndex(options.DefaultIndex)
                .MaximumRetries(options.MaxRetries)
                .SniffOnStartup(options.SniffOnStartup)
                .ThrowExceptions(false);

            ConfigureLogging(settings, options);
            ConfigureAuthentication(settings, options);

            return new ElasticsearchClient(settings);
        });

        services.AddScoped(typeof(ISearchService<>), typeof(ElasticsearchService<>));

        return services;
    }

    private static void ConfigureLogging(ElasticsearchClientSettings settings, ElasticsearchOptions options)
    {
        if (!options.EnableDebugMode) return;

        settings.EnableDebugMode().PrettyJson();
    }

    private static void ConfigureAuthentication(ElasticsearchClientSettings settings, ElasticsearchOptions options)
    {
        _ = options switch
        {
            { ApiKey: { Length: > 0 } key } => settings.Authentication(new ApiKey(key)),
            { Username: { Length: > 0 } user } => settings.Authentication(new BasicAuthentication(user, options.Password ?? string.Empty)),
            _ => settings // No authentication provided (e.g., local development)
        };
    }
}