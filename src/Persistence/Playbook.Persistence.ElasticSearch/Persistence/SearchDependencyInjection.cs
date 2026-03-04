using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Options;
using Playbook.Persistence.ElasticSearch.Application;

namespace Playbook.Persistence.ElasticSearch.Persistence;

/// <summary>
/// Provides extension methods for registering Elasticsearch infrastructure with the <see cref="IServiceCollection"/>.
/// </summary>
public static class SearchDependencyInjection
{
    /// <summary>
    /// Registers the <see cref="ElasticsearchClient"/> and the generic <see cref="ISearchService{TEntity}"/> with the service container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">The configuration instance used to bind <see cref="ElasticsearchOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method performs the following registrations:
    /// <list type="bullet">
    /// <item>Binds and validates <see cref="ElasticsearchOptions"/> using Data Annotations.</item>
    /// <item>Registers <see cref="ElasticsearchClient"/> as a <see cref="ServiceLifetime.Singleton"/>.</item>
    /// <item>Registers the open generic <see cref="ElasticsearchService{T}"/> as <see cref="ServiceLifetime.Scoped"/>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Validation is performed on startup via <see cref="OptionsBuilderExtensions.ValidateOnStart"/>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddElasticsearch(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ElasticsearchOptions>()
            .Bind(configuration.GetSection(ElasticsearchOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.ApiKey)
                     || string.IsNullOrWhiteSpace(o.Username) == string.IsNullOrWhiteSpace(o.Password),
                "When ApiKey is not configured, Username and Password must both be set or both be empty.")
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;

            var settings = new ElasticsearchClientSettings(new Uri(options.Url))
                .DefaultIndex(options.DefaultIndex)
                .MaximumRetries(options.MaxRetries)
                .SniffOnStartup(options.SniffOnStartup)
                // Explicitly disabling exception throwing to handle failures via the Result pattern.
                .ThrowExceptions(false);

            ConfigureLogging(settings, options);
            ConfigureAuthentication(settings, options);

            return new ElasticsearchClient(settings);
        });

        // Register the open generic implementation
        services.AddScoped(typeof(ISearchService<>), typeof(ElasticsearchService<>));

        return services;
    }

    /// <summary>
    /// Configures diagnostic and logging settings for the Elasticsearch client.
    /// </summary>
    /// <param name="settings">The settings object to configure.</param>
    /// <param name="options">The bound options containing debug preferences.</param>
    /// <remarks>
    /// When <see cref="ElasticsearchOptions.EnableDebugMode"/> is true, this enables detailed request/response tracking
    /// and formats JSON output for easier inspection during development.
    /// </remarks>
    private static void ConfigureLogging(ElasticsearchClientSettings settings, ElasticsearchOptions options)
    {
        if (!options.EnableDebugMode) return;

        settings.EnableDebugMode().PrettyJson();
    }

    /// <summary>
    /// Configures the authentication mechanism for the client based on the provided credentials.
    /// </summary>
    /// <param name="settings">The settings object to configure.</param>
    /// <param name="options">The bound options containing credentials.</param>
    /// <remarks>
    /// The method uses a priority-based switch:
    /// <list type="number">
    /// <item>Checks for <c>ApiKey</c> first.</item>
    /// <item>Falls back to <c>BasicAuthentication</c> (Username/Password) if ApiKey is missing.</item>
    /// <item>Defaults to no authentication for local or development environments.</item>
    /// </list>
    /// </remarks>
    private static void ConfigureAuthentication(ElasticsearchClientSettings settings, ElasticsearchOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            settings.Authentication(new ApiKey(options.ApiKey));
            return;
        }
        
        var hasUsername = !string.IsNullOrWhiteSpace(options.Username);
        var hasPassword = !string.IsNullOrWhiteSpace(options.Password);
        
        if (hasUsername ^ hasPassword)
        {
            throw new InvalidOperationException("Elasticsearch basic authentication requires both Username and Password.");
        }
        
        if (hasUsername)
        {
            settings.Authentication(new BasicAuthentication(options.Username!, options.Password!));
        }
    }
}